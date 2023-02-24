using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WebTick.Client
{
    public interface ICustomClientSettingsProvider
    {
        public Task<ClientSettings> GetClientSettings();
    }
}
