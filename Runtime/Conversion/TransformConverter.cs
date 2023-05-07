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
        public class TransformConverter : MonoBehaviour, IConverter
        {
            public void Convert(IConverter.ConvertParams p)
            {
                if (p.hasParent)
                {
                    p.entityManager.AddComponentData(p.entity, new Parent { Value = p.parentEntity });
                }
                p.entityManager.AddComponentData(p.entity, LocalTransform.FromPositionRotationScale(transform.position, transform.rotation, 1.0f));
                //TODO
                //p.entityManager.AddComponentData(p.entity, new  { Value = new float3x3(new float3(transform.localScale.x, 1, 1), new float3(1, transform.localScale.y, 1), new float3(1, 1, transform.localScale.z)) });
                p.entityManager.AddComponentData(p.entity, new LocalToWorld { Value = (p.go.transform.localToWorldMatrix) });
            }
        }
    }
}

