using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace WebTick
{
    namespace Conversion
    {
        public class MeshRendererReference : IComponentData
        {
            public MeshRenderer meshRenderer;
        }

        public class MeshFilterReference: IComponentData
        {
            public MeshFilter meshFilter;
        }

        [RequireComponent(typeof(TransformConverter))]
        public class MeshRendererConverter : MonoBehaviour, IConverter
        {
            public Component[] Convert(IConverter.ConvertParams p)
            {
                if (p.client && !p.thinClient)
                {
                    p.entityManager.AddComponentObject(p.entity, new MeshRendererReference { meshRenderer = p.go.GetComponent<MeshRenderer>() });
                    p.entityManager.AddComponentObject(p.entity, new MeshFilterReference { meshFilter = p.go.GetComponent<MeshFilter>() });
                    return new Component[] {};
                }


                return new Component[] {p.go.GetComponent<MeshRenderer>(), p.go.GetComponent<MeshFilter>()};
            }
        }
    }
}

