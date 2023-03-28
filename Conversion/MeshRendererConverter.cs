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
            public void Convert(IConverter.ConvertParams p)
            {

                p.entityManager.AddComponentObject(p.entity, new MeshRendererReference { meshRenderer = p.go.GetComponent<MeshRenderer>() });
                p.entityManager.AddComponentObject(p.entity, new MeshFilterReference { meshFilter = p.go.GetComponent<MeshFilter>() });
            }
        }
    }
}

