using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace WebTick.Client
{
    public class ClientSettingsProvider : MonoBehaviour, IClientSettingsProvider
    {
        public Task<ClientSettings> GetClientSettings()
        {
            if(Application.isEditor) {
                var url = "127.0.0.1:7880";
                return Task.FromResult(new ClientSettings { token = "", url = url });
            }

            var provider = FindObjectsOfType<MonoBehaviour>().OfType<ICustomClientSettingsProvider>();
            if(provider.Count() == 0)
            {
                var msg = "[WebTick Client] You must have a MonoBehaviour that implements ICustomClientSettingsProvider in the scene";
                Debug.LogError(msg);
                return Task.FromException(new Exception(msg)) as Task<ClientSettings>;
            }

            if (provider.Count() > 1)
            {
                var msg = "[WebTick Client] You must have a MonoBehaviour that implements ICustomClientSettingsProvider in the scene";
                Debug.LogError(msg);
                return Task.FromException(new Exception(msg)) as Task<ClientSettings>;
            }

            return provider.First().GetClientSettings();
        }
    }
}
