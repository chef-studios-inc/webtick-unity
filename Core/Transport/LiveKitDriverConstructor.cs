using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace WebTick.Transport
{
    public class LiveKitDriverConstructor : INetworkStreamDriverConstructor
    {
        public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new WebSocketNetworkInterface());
            // TODO when we have livekit working
            // var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitNetworkInterface(LiveKitNetworkInterface.Mode.Client, world));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new WebSocketNetworkInterface());
            // TODO when we have livekit working
            // var driverInstance = DefaultDriverBuilder.CreateServerNetworkDriver(new LiveKitNetworkInterface(LiveKitNetworkInterface.Mode.Server, world));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
        }
    }
}



