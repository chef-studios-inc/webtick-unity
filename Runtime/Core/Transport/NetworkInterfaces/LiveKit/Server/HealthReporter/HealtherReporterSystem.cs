using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using WebTick.Core.Transport.NetworkInterfaces.LiveKit.Common;

namespace WebTick.Core.Server.HealthReporter
{
    class HealthServerReference : IComponentData
    {
        public HealthServer healthServer;
    }

    struct HealthServerTag: IComponentData { }

    struct HealthServerListeningTag: IComponentData { }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class HealtherReporterSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var healthServerReferenceEntity = EntityManager.CreateEntity();
            EntityManager.SetName(healthServerReferenceEntity, "HealthServerReference");
            var go = new GameObject("HealthServer");
            var hs = go.AddComponent<HealthServer>();
            EntityManager.AddComponentData(healthServerReferenceEntity, new HealthServerReference { healthServer = hs });
            EntityManager.AddComponent<HealthServerTag>(healthServerReferenceEntity);
        }

        protected override void OnDestroy()
        {
            var e = SystemAPI.GetSingletonEntity<HealthServerTag>();
            var healthServerReference = EntityManager.GetComponentData<HealthServerReference>(e);
            if(healthServerReference.healthServer != null)
            {
                GameObject.Destroy(healthServerReference.healthServer.gameObject);
                healthServerReference.healthServer = null;
            }
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            // Start the health server when we get the port
            foreach (var (_, entity) in SystemAPI.Query<RefRO<HealthServerTag>>().WithNone<HealthServerListeningTag>().WithEntityAccess())
            {
                uint healthPort = 0;
                Entities.ForEach((Entity e, ConnectionDetailsReference connDetails) => {
                    healthPort = connDetails.value.GetServerConnectionDetails().healthPort;
                }).WithoutBurst().Run();

                var healthServerReference = EntityManager.GetComponentData<HealthServerReference>(entity);
                if(healthPort > 0)
                {
                    healthServerReference.healthServer.StartWithServerSettings(healthPort);
                    ecb.AddComponent<HealthServerListeningTag>(entity);
                }
            }

            // Create health info singleton
            if (!SystemAPI.HasSingleton<HealthInfo>())
            {
                var e = ecb.CreateEntity();
                ecb.SetName(e, "HealthInfo");
                ecb.AddComponent(e, new HealthInfo { Value = HealthInfo.Status.Running }); // Initializing
            }

            // Sync health info with health server
            foreach (var (_, entity) in SystemAPI.Query<RefRO<HealthServerTag>>().WithAll<HealthServerListeningTag>().WithEntityAccess())
            {
                if(SystemAPI.TryGetSingleton<HealthInfo>(out var hi))
                {
                    EntityManager.GetComponentData<HealthServerReference>(entity).healthServer.status = hi.Value;
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
