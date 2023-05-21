using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using WebTick.Core.Server;
using WebTick.Core.Transport.NetworkInterfaces.LiveKit.Client.Standalone;

namespace WebTick.Transport
{
    public class LiveKitDriverConstructor : INetworkStreamDriverConstructor
    {
        public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitClientNetworkInterface(world.EntityManager));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
            return;
#else
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitStandaloneClientNetworkInterface(world.EntityManager));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
#endif
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var driverInstance = DefaultDriverBuilder.CreateServerNetworkDriver(new WebTick.Transport.LiveKitStandaloneServerNetworkInterface(world.EntityManager));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
        }
    }
}



