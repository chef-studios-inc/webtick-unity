using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS;
using UnityEngine;

namespace WebTick.Transport
{
    public class LiveKitDriverConstructor : INetworkStreamDriverConstructor
    {
        public struct ClientSettings
        {
            public string host;
            public uint port;
        }

        public struct ServerSettings
        {
            public uint port; 
        }

        private ClientSettings clientSettings;
        private ServerSettings serverSettings;

        private LiveKitDriverConstructor() { }

        public LiveKitDriverConstructor(ClientSettings clientSettings, ServerSettings serverSettings) {
            this.clientSettings = clientSettings;
            this.serverSettings = serverSettings;
        }

        public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var networkSettings = new NetworkSettings();
            if (clientSettings.host != null && (clientSettings.host.StartsWith("wss://") || clientSettings.host.StartsWith("https://")))
            {
                networkSettings = networkSettings.WithSecureClientParameters(clientSettings.host.Replace("wss://", "").Replace("https://", ""));
            }
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new WebSocketNetworkInterface(), networkSettings);
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



