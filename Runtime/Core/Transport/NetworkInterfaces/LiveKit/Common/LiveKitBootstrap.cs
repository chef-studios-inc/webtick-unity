using System.Collections;
using System.Collections.Generic;
using Unity.NetCode;
using UnityEngine;
using WebTick.Transport;

class LiveKitBoostrap : ClientServerBootstrap
{
    override public bool Initialize(string defaultWorldName)
    {
        NetworkStreamReceiveSystem.DriverConstructor = new LiveKitDriverConstructor();
        return base.Initialize(defaultWorldName);
    }
}
