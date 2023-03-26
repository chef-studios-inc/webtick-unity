using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace WebTick.Objects
{
    public class StatsDisplay : MonoBehaviour
    {
        public Text rttText;
        public Text fps;
        // Start is called before the first frame update
        private World clientWorld = null;
        private WaitForSeconds wait = new WaitForSeconds(0.2f);
        private Entity entity;

        void Start()
        {
#if UNITY_CLIENT || UNITY_EDITOR
            StartCoroutine(Initialize());
#endif
        }

        IEnumerator Initialize()
        {
            while (clientWorld == null)
            {
                yield return wait;
                var worlds = World.All;
                foreach (var w in worlds)
                {
                    if (w.IsClient())
                    {
                        clientWorld = w;
                        break;
                    }
                }
            }

            var em = clientWorld.EntityManager;
            entity = em.CreateEntity();
            em.SetName(entity, "Stats Display");
            em.AddComponentObject(entity, new StatsReference { stats = this });

        }

        private void OnDestroy()
        {
            if(clientWorld != null)
            {
                clientWorld.EntityManager.DestroyEntity(entity);
            }
        }
    }

    class StatsReference: IComponentData
    {
        public StatsDisplay stats;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    partial class StatsDisplaySystem : SystemBase
    {
        private float fps;
        protected override void OnUpdate()
        {
            var fps = 1.0f / SystemAPI.Time.DeltaTime;
            var rtt = -1.0f;
            Entities.ForEach((NetworkSnapshotAckComponent snapshot) => {
                rtt = snapshot.EstimatedRTT;
            }).WithoutBurst().Run();

            Entities.ForEach((StatsReference sr) => {
                sr.stats.rttText.text = string.Format("RTT: {0}", rtt.ToString());
                sr.stats.fps.text = string.Format("FPS: {0}", fps.ToString());
            }).WithoutBurst().Run();
        }
    }
}

