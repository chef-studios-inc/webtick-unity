using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace WebTick.Core.Server.HealthReporter
{
    public struct HealthInfo : IComponentData
    {
        public enum Status
        {
            Initializing,
            Running,
            Complete,
            Failed
        }

        public Status Value;
    }
}
