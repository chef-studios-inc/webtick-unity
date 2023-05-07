using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace WebTick
{
    public interface IWebTickSpawner
    {
        public GameObject GetViewPrefab();
        public GhostPrefabCreation.Config GetGhostConfig(); 
        public Entity CreateGhostEntity(EntityManager em);
    }
}

