using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace WebTick
{
    namespace Conversion
    {
        [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
        public partial class TransformSyncSystem : SystemBase
        {
            protected override void OnUpdate()
            {
                var cb = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

                // Mark gamobject for destruction 
                Entities.WithNone<TransformReference>().ForEach((Entity e, TransformCleanupReference tcr) =>
                {
                    Object.Destroy(tcr.transform);
                    cb.DestroyEntity(e);
                }).WithoutBurst().Run();

                // Sync the transform
                Entities.ForEach((TransformReference tr, in Unity.Transforms.LocalToWorld ltw) =>
                {
                    if (tr.transform == null)
                    {
                        return;
                    }
                    tr.transform.position = ltw.Position;
                    tr.transform.rotation = ltw.Rotation;
                }).WithoutBurst().Run();

                Entities.ForEach((TransformReference tr, in Unity.Transforms.PostTransformScale cs) =>
                {
                    if (tr.transform == null)
                    {
                        return;
                    }
                    tr.transform.localScale = new Vector3(cs.Value.c0.x, cs.Value.c1.y, cs.Value.c2.z);
                }).WithoutBurst().Run();
            }
        }
    }
}


