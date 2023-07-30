using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Networking.Transport;
using System.Threading.Tasks;
using WebTick.Transport;
using WebTick.Core.Transport.NetworkInterfaces.LiveKit.Common;
#if UNITY_WEBGL && !UNITY_EDITOR
#else
using WebTick.Livekit.Standalone;
#endif

namespace WebTick.Core.Transport.NetworkInterfaces.LiveKit.Client
{

    public struct ClientConnectRequest: IComponentData { };
    public struct ClientConnectionDetails : IComponentData
    {
        public FixedString512Bytes wsUrl;
        public FixedString512Bytes token;
    }

    class GetClientConnectionDetailsTask: IComponentData
    {
        public Task<ClientConnectionDetails> value;
    }

    struct GetClientConnectionDetailsTaskTag : IComponentData { };

#if UNITY_WEBGL && !UNITY_EDITOR
#else
    class ClientGameObject : IComponentData
    {
        public uint engineHandle;
        public RTCEngine rtcEngine;
    }
#endif

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ClientConnectSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // Fetch client connecton details
            foreach (var (_, entity) in SystemAPI.Query<RefRO<ClientConnectRequest>>().WithEntityAccess())
            {
                if (ConnectionDetails.instance == null)
                {
                    Debug.LogWarning("No ConnectionDetails when connecting");
                    return;
                }

                var connectionDetailsTask = ConnectionDetails.instance.GetClientConnectionDetails();
                var taskEntity = ecb.CreateEntity();
                ecb.AddComponent(taskEntity, new GetClientConnectionDetailsTask { value = connectionDetailsTask });
                ecb.AddComponent<GetClientConnectionDetailsTaskTag>(taskEntity);

                ecb.DestroyEntity(entity);
                break;
            }

            // Wait for client details and connect everything when we get them
            if(SystemAPI.HasSingleton<GetClientConnectionDetailsTaskTag>() && SystemAPI.HasSingleton<NetworkStreamDriver>())
            {
                Debug.LogFormat("NEIL Waiting for client connection details");
                var e = SystemAPI.GetSingletonEntity<GetClientConnectionDetailsTaskTag>();
                ecb.DestroyEntity(e);
                var task = EntityManager.GetComponentData<GetClientConnectionDetailsTask>(e);
                if(!task.value.IsCompleted)
                {
                    return;
                }
                if(task.value.IsCompletedSuccessfully)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    Debug.LogFormat("NEIL Creating client connection details: {0}", task.value.Result.token);
                    var clientConnectionDetailsEntity = ecb.CreateEntity();
                    ecb.SetName(clientConnectionDetailsEntity, "ClientConnectionDetails");
                    var newToken = new FixedString512Bytes(task.value.Result.token);
                    var newWs = new FixedString512Bytes(task.value.Result.wsUrl);
                    ecb.AddComponent(clientConnectionDetailsEntity, new ClientConnectionDetails { token = newToken, wsUrl = newWs });
#else
                    CreateRTCEngineGameObject(ecb, task.value.Result);
#endif
                }

                var nsde = SystemAPI.GetSingletonEntity<NetworkStreamDriver>();
                var nsd = SystemAPI.GetComponent<NetworkStreamDriver>(nsde);
                var endpoint = NetworkEndpoint.LoopbackIpv4.WithPort(1234);
                nsd.Connect(EntityManager, endpoint);

            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
#else
        private static void CreateRTCEngineGameObject(EntityCommandBuffer em, ClientConnectionDetails clientConnectionDetails)
        {
            var e = em.CreateEntity();
            em.SetName(e, "ClientConnection");
            em.AddComponent(e, clientConnectionDetails);
            var go = new GameObject("ClientConnection");
            var rtcEngine = go.AddComponent<RTCEngine>();
            rtcEngine.Connect(new RTCEngine.ConnectParams { token = clientConnectionDetails.token.ToString(), url = clientConnectionDetails.wsUrl.ToString() });
            var engineHandle = RTCEngineManager.RegisterEngine(rtcEngine);
            var clientGameObject = new ClientGameObject 
            {
                engineHandle = engineHandle,
                rtcEngine = rtcEngine,
            };
            em.AddComponent(e, clientGameObject);

        }
#endif
    }
}

