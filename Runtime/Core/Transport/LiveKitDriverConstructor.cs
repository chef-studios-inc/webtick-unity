using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using WebTick.Core.Server;

namespace WebTick.Transport
{
    public class LiveKitDriverConstructor : INetworkStreamDriverConstructor
    {
        public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
#if UNITY_WEBGL
            var token = settings.token;
            var url = settings.url;
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitClientNetworkInterface(url, token));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
            return;
#else
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new WebTick.Transport.LiveKitStandaloneClientNetworkInterface(world.EntityManager));
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



