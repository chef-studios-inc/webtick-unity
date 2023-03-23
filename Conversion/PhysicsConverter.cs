using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;


namespace WebTick.Conversion
{
    public class PhysicsConverter : MonoBehaviour, IConverter
    {
        public Component[] Convert(IConverter.ConvertParams p)
        {
            return Convert(p.go, p.entity, p.entityManager);
        }

        private static BlobAssetReference<Unity.Physics.Collider> CreateColliderFromShape(Entity e, EntityManager em, GameObject go, PhysicsShapeAuthoring physicsShapeAuthoring)
        {
            var filter = new CollisionFilter { BelongsTo = physicsShapeAuthoring.BelongsTo.Value, CollidesWith = physicsShapeAuthoring.CollidesWith.Value };
            var material = new Unity.Physics.Material {
                CollisionResponse = physicsShapeAuthoring.CollisionResponse,
                Friction = physicsShapeAuthoring.Friction.Value,
                Restitution = physicsShapeAuthoring.Restitution.Value,
                FrictionCombinePolicy = physicsShapeAuthoring.Friction.CombineMode,
                RestitutionCombinePolicy = physicsShapeAuthoring.Restitution.CombineMode,
                CustomTags = physicsShapeAuthoring.CustomTags.Value
            };

            if (physicsShapeAuthoring.ShapeType == ShapeType.Box)
            {
                var geom = physicsShapeAuthoring.GetBoxProperties();
                geom.Size = geom.Size * go.transform.lossyScale;
                return Unity.Physics.BoxCollider.Create(geom, filter, material);
            }
            else if (physicsShapeAuthoring.ShapeType == ShapeType.Capsule)
            {
                if(go.transform.lossyScale.sqrMagnitude != 1)
                {
                    Debug.LogError("Capsules don't support scaling yet - you may see some weirdness. Especially for non-uniform scales");
                }
                return Unity.Physics.CapsuleCollider.Create(physicsShapeAuthoring.GetCapsuleProperties().ToRuntime(), filter, material);
            }
            else if (physicsShapeAuthoring.ShapeType == ShapeType.Sphere)
            {
                if(go.transform.lossyScale.sqrMagnitude != 1)
                {
                    Debug.LogError("Spheres don't support scaling yet - you may see some weirdness. Especially for non-uniform scales");
                }
                quaternion outQuat;
                return Unity.Physics.SphereCollider.Create(physicsShapeAuthoring.GetSphereProperties(out outQuat), filter, material);
            }
            else if (physicsShapeAuthoring.ShapeType == ShapeType.Cylinder)
            {
                if(go.transform.lossyScale.sqrMagnitude != 1)
                {
                    Debug.LogError("Cylinders don't support scaling yet - you may see some weirdness. Especially for non-uniform scales");
                }
                var geom = physicsShapeAuthoring.GetCylinderProperties();
                return Unity.Physics.CylinderCollider.Create(physicsShapeAuthoring.GetCylinderProperties(), filter, material);
            }
            else if (physicsShapeAuthoring.ShapeType == ShapeType.Mesh)
            {
                if(go.transform.lossyScale.sqrMagnitude != 1) {
                    Debug.LogError("Mesh Colliders require uniform scale");
                }
                // NOTE is there a faster way to copy these arrays other than using burst/jobs?
                var mesh = go.GetComponent<MeshFilter>().mesh;
                var verts = new NativeArray<float3>(mesh.vertices.Length, Allocator.TempJob);
                verts.Reinterpret<Vector3>().CopyFrom(mesh.vertices);
                var tris = new NativeArray<int3>(mesh.triangles.Length, Allocator.TempJob);
                var t = new NativeArray<int>(mesh.triangles, Allocator.TempJob);

                var job = new MeshColliderJob {
                    meshTriangles = t,
                    triangles = tris,
                };

                job.Run();
                var collider = Unity.Physics.MeshCollider.Create(verts, tris, filter, material);
                return collider;
            }
            else if (physicsShapeAuthoring.ShapeType == ShapeType.ConvexHull)
            {
                throw new System.Exception("Unhandled collider 'Hull'");
            }
            else if (physicsShapeAuthoring.ShapeType == ShapeType.Plane)
            {
                throw new System.Exception("Unhandled collider 'Plane'");
            }

            throw new System.Exception("Unhandled Collider");
        }

        [Unity.Burst.BurstCompile]
        public struct MeshColliderJob : IJob {
            public NativeArray<int> meshTriangles;
            public NativeArray<int3> triangles;
            public void Execute() {
                var trisLength = meshTriangles.Length;
                for(int i = 0; i < trisLength; i += 3) {
                    triangles[i] = new int3 {
                        x = meshTriangles[i],
                        y = meshTriangles[i + 1],
                        z = meshTriangles[i + 2]
                    };
                }
            }
        }


        public static Component[] Convert(GameObject go, Entity e, EntityManager entityManager)
        {
            var physicsShapeAuthoring = go.GetComponent<PhysicsShapeAuthoring>();
            var physicsBodyAuthoring = go.GetComponent<PhysicsBodyAuthoring>();
            Debug.LogFormat("NEIL {0} - {1} - {2}", physicsShapeAuthoring, physicsBodyAuthoring, go);
            var isDynamic = physicsBodyAuthoring.MotionType == BodyMotionType.Dynamic;
            var transform = go.GetComponent<Transform>();

            var collider = CreateColliderFromShape(e, entityManager, go, physicsShapeAuthoring);

            entityManager.AddComponentData(e, new LocalToWorld());
            entityManager.AddComponentData(e, LocalTransform.FromPositionRotationScale(transform.position, transform.rotation, transform.lossyScale.x));
            entityManager.AddComponentData(e, new PhysicsCollider { Value = collider });
            entityManager.AddSharedComponentManaged(e, new PhysicsWorldIndex { Value = physicsBodyAuthoring.WorldIndex });

            if (isDynamic)
            {
                entityManager.AddComponent<PhysicsVelocity>(e);
                unsafe
                {
                    Unity.Physics.Collider* colliderPtr = (Unity.Physics.Collider*)collider.GetUnsafePtr();
                    entityManager.AddComponentData(e, PhysicsMass.CreateDynamic(colliderPtr->MassProperties, physicsBodyAuthoring.Mass));
                }
                entityManager.AddComponentData(e, new PhysicsDamping
                {
                    Linear = physicsBodyAuthoring.LinearDamping,
                    Angular = physicsBodyAuthoring.AngularDamping
                });
                entityManager.AddComponentData(e, new PhysicsGravityFactor { Value = physicsBodyAuthoring.GravityFactor });
            }
            else
            {
                unsafe
                {
                    Unity.Physics.Collider* colliderPtr = (Unity.Physics.Collider*)collider.GetUnsafePtr();
                    entityManager.AddComponentData(e, PhysicsMass.CreateKinematic(colliderPtr->MassProperties));
                }
            }

            return new Component[] { physicsBodyAuthoring, physicsShapeAuthoring };
        }
    }



}
