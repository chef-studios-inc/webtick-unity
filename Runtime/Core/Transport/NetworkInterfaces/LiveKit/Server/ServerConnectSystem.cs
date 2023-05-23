using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using WebTick.Core.Server.HealthReporter;
using WebTick;
using Unity.Collections;
using Unity.NetCode;
using Unity.Networking.Transport;
using WebTick.Core.Transport.NetworkInterfaces.LiveKit.Common;
#if UNITY_WEBGL && !UNITY_EDITOR
#else
using WebTick.Livekit.Standalone;
#endif

namespace WebTick.Core.Server
{
    public struct ServerConnectionDetails : IComponentData
    {
        public FixedString512Bytes wsUrl;
        public FixedString512Bytes token;
        public uint healthPort;
    }

    public struct ServerConnectRequest: IComponentData { };

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ServerConnectSystem : SystemBase
    {

#if UNITY_WEBGL && !UNITY_EDITOR
        protected override void OnUpdate()
        {
            throw new System.NotImplementedException("Not implemented");
        }
#else
        public class ServerGameObject: IComponentData {
            public uint engineHandle;
            public RTCEngine rtcEngine;
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // Fetch server connecton details
            foreach (var (_, entity) in SystemAPI.Query<RefRO<ServerConnectRequest>>().WithEntityAccess())
            {
                if(!SystemAPI.HasSingleton<NetworkStreamDriver>())
                {
                    return;
                }
                if(ConnectionDetails.instance == null)
                {
                    Debug.LogWarning("No ConnectionDetails when connecting");
                    return;
                }

                var serverConnectionDetails = ConnectionDetails.instance.GetServerConnectionDetails();
                CreateRTCEngineGameObject(ecb, serverConnectionDetails);
                var nsde = SystemAPI.GetSingletonEntity<NetworkStreamDriver>();
                var nsd = SystemAPI.GetComponent<NetworkStreamDriver>(nsde);
                var endpoint = NetworkEndpoint.AnyIpv4.WithPort(1234);
                nsd.Listen(endpoint);
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);

            // TODO health check
            //healthServer.status = HealthInfo.Status.Running;
            //StartCoroutine(HealthCheckLoop());
        }

        private static void CreateRTCEngineGameObject(EntityCommandBuffer em, ServerConnectionDetails serverConnectionDetails)
        {
            var e = em.CreateEntity();
            em.SetName(e, "ServerConnection");
            em.AddComponent(e, serverConnectionDetails);
            var go = new GameObject("ServerConnection");
            var rtcEngine = go.AddComponent<RTCEngine>();
            rtcEngine.Connect(new RTCEngine.ConnectParams { token = serverConnectionDetails.token.ToString(), url = serverConnectionDetails.wsUrl.ToString() });
            var engineHandle = RTCEngineManager.RegisterEngine(rtcEngine);
            var serverGameObject = new ServerGameObject {
                engineHandle = engineHandle,
                rtcEngine = rtcEngine,
            };
            em.AddComponent(e, serverGameObject);

        }
#endif
    }
}


