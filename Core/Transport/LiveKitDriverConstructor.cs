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
        private ClientProxyWebSocketManager clientProxyWebSocketManager;

        private LiveKitDriverConstructor() { }

        public LiveKitDriverConstructor(WebSocketManager wsManager, LiveKitManager liveKitManager, ClientProxyWebSocketManager clientProxyWebSocketManager) {
            this.webSocketManager = wsManager;
            this.liveKitManager = liveKitManager;
            this.clientProxyWebSocketManager = clientProxyWebSocketManager;
        }

        public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            if(clientProxyWebSocketManager != null)
            {
                var di = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitClientProxyNetworkInterface(clientProxyWebSocketManager));
                driver.RegisterDriver(TransportType.Socket, di);
                return;
            }

            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitClientNetworkInterface(liveKitManager));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            if (webSocketManager != null)
            {
                var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitServerNetworkInterface(webSocketManager));
                driver.RegisterDriver(TransportType.Socket, driverInstance);
                return;
            }

            throw new System.Exception("No websocket manager");
        }
    }
}



