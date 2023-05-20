using Codice.CM.Client.Differences;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using WebTick.Core.Server.HealthReporter;
using WebTick;
using WebTick.Livekit.Standalone;
using Unity.Networking.Transport;
using System.Threading.Tasks;
using WebTick.Transport;

namespace WebTick.Transport
{
    public struct ClientConnectionDetails : IComponentData
    {
        public FixedString512Bytes wsUrl;
        public FixedString512Bytes token;
    }

    public struct ClientConnectRequest: IComponentData { };

    class GetClientConnectionDetailsTask: IComponentData
    {
        public Task<ClientConnectionDetails> value;
    }

    struct GetClientConnectionDetailsTaskTag : IComponentData { };

    class ClientGameObject : IComponentData
    {
        public uint engineHandle;
        public RTCEngine rtcEngine;
    }

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
                ConnectionDetailsReference connectionDetailsReference = null;
                Entities.ForEach((ConnectionDetailsReference reference) =>
                {
                    connectionDetailsReference = reference;
                }).WithoutBurst().Run();

                if (connectionDetailsReference == null)
                {
                    Debug.LogWarning("No ConnectionDetailsReference when connecting");
                    return;
                }

                var connectionDetailsTask = connectionDetailsReference.value.GetClientConnectionDetails();
                var taskEntity = ecb.CreateEntity();
                ecb.AddComponent(taskEntity, new GetClientConnectionDetailsTask { value = connectionDetailsTask });
                ecb.AddComponent<GetClientConnectionDetailsTaskTag>(taskEntity);

                ecb.DestroyEntity(entity);
                break;
            }

            // Wait for client details and connect everything when we get them
            if(SystemAPI.HasSingleton<GetClientConnectionDetailsTaskTag>() && SystemAPI.HasSingleton<NetworkStreamDriver>())
            {
                var e = SystemAPI.GetSingletonEntity<GetClientConnectionDetailsTaskTag>();
                var task = EntityManager.GetComponentData<GetClientConnectionDetailsTask>(e);
                if(!task.value.IsCompleted)
                {
                    return;
                }
                if(task.value.IsCompletedSuccessfully)
                {
                    CreateRTCEngineGameObject(ecb, task.value.Result);
                }

                var nsde = SystemAPI.GetSingletonEntity<NetworkStreamDriver>();
                var nsd = SystemAPI.GetComponent<NetworkStreamDriver>(nsde);
                var endpoint = NetworkEndpoint.LoopbackIpv4.WithPort(1234);
                nsd.Connect(EntityManager, endpoint);

                ecb.DestroyEntity(e);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

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
    }
}

