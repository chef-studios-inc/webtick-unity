using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WebTick.Core.Server
{
    public class ServerSettingsProvider : MonoBehaviour, IServerSettingsProvider
    {
        public Task<ServerSettings> GetServerSettings()
        {
            if (Application.isEditor)
            {
                return Task.FromResult(new ServerSettings { });
            }
            var serverSettingsString = Environment.GetEnvironmentVariable("WEBTICK_SERVER_SETTINGS");
            if(string.IsNullOrEmpty(serverSettingsString))
            {
                Debug.LogError("Need to supply WEBTICK_SERVER_SETTINGS environment variable");
            }
            var serverSettings = JsonUtility.FromJson<ServerSettings>(serverSettingsString);
            return Task.FromResult(serverSettings);
        }
    }
}
