using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using WebTick.Transport;
using System.Threading.Tasks;
using WebTick.Core.Server;
using System;
using System.Runtime.CompilerServices;

namespace WebTick
{
    [System.Serializable]
    public struct ServerSettings
    {
        public string ws_url;
        public string token;
        public string health_port;
    }

    public interface IProductionClientConnectionDetailsProvider
    {
        public Task<ClientConnectionDetails> GetProductionClientConnectionDetails();
    }

    public class ConnectionDetailsReference: IComponentData
    {
        public ConnectionDetailsAuthoring value;
    }

    public class ConnectionDetailsAuthoring : MonoBehaviour
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
        private IProductionClientConnectionDetailsProvider productionClientDetailsProvider = null;
        private int index = 0;

        public string GetServerToken()
        {
            return serverToken;
        }

        private string GetClientToken()
        {
            if(index >= clientTokens.Count)
            {
                throw new System.Exception("No more editor client tokens");
            }
            return clientTokens[index++];
        }

        public class Baker : Baker<ConnectionDetailsAuthoring>
        {
            public override void Bake(ConnectionDetailsAuthoring authoring)
            {
                Debug.LogFormat("NEIL bake");
                var e = CreateAdditionalEntity(TransformUsageFlags.None, false, "EditorServerConnectionDetails");
                AddComponentObject(e, new ConnectionDetailsReference { value = authoring });
                authoring.productionClientDetailsProvider = authoring.GetComponentInChildren<IProductionClientConnectionDetailsProvider>();
            }
        }

        protected virtual Task<ClientConnectionDetails> GetProductionClientDetails()
        {
            if(productionClientDetailsProvider == null)
            {
                throw new System.Exception("Must add an IProductionClientConnectionDetails child monobehavior to this prefab");
            }
            return productionClientDetailsProvider.GetProductionClientConnectionDetails();
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
                return new ServerConnectionDetails { token = serverToken, wsUrl = wsUrl };
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
            return new ServerConnectionDetails { token = serverSettings.token, wsUrl = serverSettings.ws_url };
        }

    }
}

