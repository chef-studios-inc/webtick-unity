using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using WebTick.Core.Transport.NetworkInterfaces.LiveKit.Client;
using WebTick.Core.Server;

namespace WebTick.Core.Transport.NetworkInterfaces.LiveKit.Common
{
    [System.Serializable]
    public struct ServerSettings
    {
        public string ws_url;
        public string token;
        public uint health_port;
    }

    public class ConnectionDetails : MonoBehaviour
    {
        public enum Mode
        {
            UseEditorDetails,
            UseProductionDetails,
            UseIPC
        }

        public Mode mode;
        public string wsUrl;
        public string serverToken;
        public List<string> clientTokens;
        public static ConnectionDetails instance;
        private int index = 0;

        private void Awake()
        {
            if(instance != null)
            {
                throw new Exception("Only one connection details allowed");
            }
            instance = this;

        }

        private void OnDestroy()
        {
            instance = null;
        }

        private string GetClientToken()
        {
            if(index >= clientTokens.Count)
            {
                throw new System.Exception("No more editor client tokens");
            }
            return clientTokens[index++];
        }

        protected virtual Task<ClientConnectionDetails> GetProductionClientDetails()
        {

            throw new System.Exception("Must implement GetProductionClientDetails in subclass");
        }

        public async Task<ClientConnectionDetails> GetClientConnectionDetails()
        {
            if (!Application.isEditor)
            {
                return await GetProductionClientDetails();
            }

            if(mode == Mode.UseEditorDetails)
            {
                var token = GetClientToken();
                return new ClientConnectionDetails { token = token, wsUrl = wsUrl };
            } else if(mode == Mode.UseProductionDetails)
            {
                return await GetProductionClientDetails();
            }
            return new ClientConnectionDetails();
        }

        public ServerConnectionDetails GetServerConnectionDetails()
        {
            if(!Application.isEditor)
            {
                return GetProductionServerConnectionDetails();
            }

            if(mode == Mode.UseEditorDetails)
            {
                return new ServerConnectionDetails { token = serverToken, wsUrl = wsUrl, healthPort=7882 };
            } else if(mode == Mode.UseProductionDetails)
            {
                return GetProductionServerConnectionDetails();
            }

            return new ServerConnectionDetails();
        }

        private ServerConnectionDetails GetProductionServerConnectionDetails()
        {
            var serverSettingsString = Environment.GetEnvironmentVariable("WEBTICK_SERVER_SETTINGS");
            var serverSettings = JsonUtility.FromJson<ServerSettings>(serverSettingsString);
            return new ServerConnectionDetails { token = serverSettings.token, wsUrl = serverSettings.ws_url, healthPort = serverSettings.health_port };
        }

    }
}

