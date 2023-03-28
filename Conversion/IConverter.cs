using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace WebTick.Conversion
{
    public interface IConverter
    {
        public struct ConvertParams
        {
            public GameObject go;
            public Entity entity;
            public Entity parentEntity;
            public bool hasParent;
            public EntityManager entityManager;
            public bool client;
            public bool server;
            public bool thinClient;
        }
        /// <summary>
        /// Method <c>Convert</c> runs once when the scene first starts. 
        /// Return an array of components to remove from the gameobject.
        /// If there is a conflict, it will favor keeping the GameObject.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="e"></param>
        /// <param name="em"></param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <param name="thinClient"></param>
        /// <returns></returns>
        public void Convert(ConvertParams p);
    }
}
