using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WebTick.Core.Server
{
    internal interface IServerSettingsProvider
    {
        Task<ServerSettings> GetServerSettings();
    }
}