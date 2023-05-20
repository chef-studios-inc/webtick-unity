using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace WebTick.Core.Server.HealthReporter
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class HealtherReporterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!SystemAPI.HasSingleton<HealthInfo>())
            {
                var cb = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
                var e = cb.CreateEntity();
                cb.SetName(e, "HealthInfo");
                cb.AddComponent(e, new HealthInfo { Value = HealthInfo.Status.Running }); // Initializing
            }
        }
    }
}
