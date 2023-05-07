using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace WebTick.Transport
{
    public class LiveKitDriverConstructor : INetworkStreamDriverConstructor
    {
        public struct RTCSettings {
            public uint engineHandle;
            public string token;
            public string url;
        }

        private Dictionary<string, RTCSettings> worldClientDriverMap = new Dictionary<string, RTCSettings>();

        private LiveKitDriverConstructor() { }

        public LiveKitDriverConstructor(Dictionary<string, RTCSettings> worldClientDriverMap)
        {
            this.worldClientDriverMap = worldClientDriverMap;
        }

        public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var settings = worldClientDriverMap[world.Name];
#if UNITY_WEBGL
            var token = settings.token;
            var url = settings.url;
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new LiveKitClientNetworkInterface(url, token));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
            return;
#else
            if(!worldClientDriverMap.ContainsKey(world.Name)) { 
                throw new System.Exception("No client livekit engine handle for world");
            }
            var driverInstance = DefaultDriverBuilder.CreateClientNetworkDriver(new WebTick.Transport.LiveKitStandaloneClientNetworkInterface(settings.engineHandle));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
#endif
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug netDebug)
        {
            var settings = worldClientDriverMap[world.Name];
            var driverInstance = DefaultDriverBuilder.CreateServerNetworkDriver(new WebTick.Transport.LiveKitStandaloneServerNetworkInterface(settings.engineHandle));
            driver.RegisterDriver(TransportType.Socket, driverInstance);
            return;
        }
    }
}



