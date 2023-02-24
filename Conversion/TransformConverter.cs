using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace WebTick
{
    namespace Conversion
    {
        public class TransformReference : IComponentData
        {
            public Transform transform;
        }

        public class TransformCleanupReference : ICleanupComponentData
        {
            public Transform transform;
        }

        public class TransformConverter : MonoBehaviour, IConverter
        {
            public Component[] Convert(IConverter.ConvertParams p)
            {
                if (p.hasParent)
                {
                    p.entityManager.AddComponentData(p.entity, new Parent { Value = p.parentEntity });
                }
                p.entityManager.AddComponentData(p.entity, LocalTransform.FromPositionRotationScale(transform.position, transform.rotation, 1.0f));
                p.entityManager.AddComponentData(p.entity, new PostTransformScale { Value = new float3x3(new float3(transform.localScale.x, 1, 1), new float3(1, transform.localScale.y, 1), new float3(1, 1, transform.localScale.z)) });
                p.entityManager.AddComponentData(p.entity, new LocalToWorld { Value = (p.go.transform.localToWorldMatrix) });
                //TODO does this need fixing up?

                if(p.client)
                {
                    p.entityManager.AddComponentObject(p.entity, new TransformReference { transform = p.go.transform });
                    p.entityManager.AddComponentObject(p.entity, new TransformCleanupReference { transform = p.go.transform });
                }

                return new Component[] { };
            }
        }
    }
}

