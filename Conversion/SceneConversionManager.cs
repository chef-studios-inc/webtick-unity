using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using System;

namespace WebTick
{
    namespace Conversion
    {
        public class SceneConversionManager : MonoBehaviour
        {
            public static SceneConversionManager instance;

            private Dictionary<Tuple<World, GameObject>, Entity> entityLookup = new Dictionary<Tuple<World, GameObject>, Entity>();
            private HashSet<Transform> convertedServerSet = new HashSet<Transform>();
            private HashSet<Transform> convertedClientSet = new HashSet<Transform>();

            //private HashSet<Component> componentsToRemove = new HashSet<Component>();

            private void Awake()
            {
                instance = this;
            }

            public void Convert(World world)
            {
                var rootGameObjects = gameObject.scene.GetRootGameObjects();
                foreach(var rootGo in rootGameObjects)
                {
                    var transforms = rootGo.GetComponentsInChildren<Transform>();
                    foreach(var t in transforms)
                    {
                        ConvertTransform(world, t, convertedClientSet, world.EntityManager, false, true, false);
                    }
                }

                //foreach(var c in componentsToRemove)
                //{
                //    Destroy(c);
                //}

                //componentsToRemove.Clear();
            }

            private void ConvertTransform(World w, Transform t, HashSet<Transform> convertedSet, EntityManager em, bool server, bool client, bool thinClient)
            {
                if(convertedSet.Contains(t))
                {
                    return;
                }

                // TODO converters can be cached
                var converters = t.GetComponents<IConverter>();

                if(t.parent != null)
                {
                    ConvertTransform(w, t.parent, convertedSet, em, server, client, thinClient);
                }
                Entity pe = Entity.Null;
                var hasParent = false;
                if(t.parent != null && entityLookup.ContainsKey(Tuple.Create(w, t.parent.gameObject)))
                {
                    hasParent = true;
                    pe = entityLookup[Tuple.Create(w, t.parent.gameObject)];
                }

                Entity e;
                if (entityLookup.ContainsKey(Tuple.Create(w, t.gameObject)))
                {
                    e = entityLookup[Tuple.Create(w, t.gameObject)];
                }
                else
                {
                    e = em.CreateEntity();
                    entityLookup[Tuple.Create(w, t.gameObject)] = e;
                    em.SetName(e, t.name);
                }

                foreach (var c in converters)
                {
                    var components = c.Convert(new IConverter.ConvertParams
                    {
                        client = client,
                        server = server,
                        thinClient = false,
                        go = t.gameObject,
                        entityManager = em,
                        entity = e,
                        parentEntity =pe,
                        hasParent = hasParent,
                    });

                    //foreach(var comp in components)
                    //{
                    //    componentsToRemove.Add(comp);
                    //}
                }
            }
        }
    }
}

