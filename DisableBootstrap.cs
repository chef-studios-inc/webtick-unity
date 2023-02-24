using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace WebTick
{
    [UnityEngine.Scripting.Preserve]
    public class DisableBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            Debug.Log("DISABLE BOOTSTRAP");
            var defaultWorld = new World("Default");
            World.DefaultGameObjectInjectionWorld = defaultWorld;
            return true;
        }
    }
}

