using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace WebTick.Client
{
    internal interface IClientSettingsProvider
    {
        public Task<ClientSettings> GetClientSettings(World world);
    }
}
