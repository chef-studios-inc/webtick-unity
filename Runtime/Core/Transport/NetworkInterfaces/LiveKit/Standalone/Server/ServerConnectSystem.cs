using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using WebTick.Core.Server.HealthReporter;
using WebTick;
using Unity.Collections;
using WebTick.Livekit.Standalone;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace WebTick.Core.Server
{
    public struct ServerConnectionDetails : IComponentData
    {
        public FixedString512Bytes wsUrl;
        public FixedString512Bytes token;
    }

    struct ServerConnectionTag : IComponentData { }
    public struct ServerConnectRequest: IComponentData { };

    class ServerGameObject: IComponentData {
        public uint engineHandle;
        public RTCEngine rtcEngine;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ServerConnectSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
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

                var serverConnectionDetails = connectionDetailsReference.value.GetServerConnectionDetails();
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private static void CreateRTCEngineGameObject(EntityCommandBuffer em, ServerConnectionDetails serverConnectionDetails)
        {
            var e = em.CreateEntity();
            em.SetName(e, "ServerConnection");
            em.AddComponent<ServerConnectionTag>(e);
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
    }
}


