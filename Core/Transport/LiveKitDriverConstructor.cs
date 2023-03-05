using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace WebTick.Transport
{
    public class LiveKitDriverConstructor : INetworkStreamDriverConstructor
    {
        private WebSocketManager webSocketManager;
        private LiveKitManager liveKitManager;

        private LiveKitDriverConstructor() { }

        public LiveKitDriverConstructor(WebSocketManager wsManager, LiveKitManager liveKitManager) {
            this.webSocketManager = wsManager;
            this.liveKitManager = liveKitManager;
        }

        public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitClientNetworkInterface(liveKitManager));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitServerNetworkInterface(webSocketManager));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
        }
    }
}



