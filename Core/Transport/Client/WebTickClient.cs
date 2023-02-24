using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using WebTick.Transport;
using WebTick.Util;

namespace WebTick.Client
{
    public class WebTickClient : MonoBehaviour 
    {
        // TODO when we get livekit
//        public static Dictionary<FixedString128Bytes, WebTickClient> instanceLookup = new Dictionary<FixedString128Bytes, WebTickClient>();
//        public static WebTickClient GetInstance(FixedString128Bytes worldName)
//        {
//            if (instanceLookup.ContainsKey(worldName))
//            {
//                return instanceLookup[worldName];
//            }
//            return null;
//        }

//        private IRTCEngine livekit;
//        private World world;
//        private string serverSid = null;

//        private string url;
//        private string token;
//        private ushort port;

//        private NativeQueue<ReceivedMessage> messageQueue = new NativeQueue<ReceivedMessage>(Allocator.Persistent);
//        public NativeQueue<ReceivedMessage> receiveQueue => messageQueue;

//        public TaskCompletionSource<bool> connectTask = new TaskCompletionSource<bool>();

//        // Start is called before the first frame update
//        public Task Connect(string url, ushort port, string token, World world)
//        {
//            this.url = url;
//            this.token = token;
//            this.port = port;

//            instanceLookup[world.Name] = this;
//            this.world = world;
//#if UNITY_WEBGL && !UNITY_EDITOR
//// TODO when we have livekit
//                // livekit = gameObject.AddComponent<WebTick.Livekit.Web.RTCEngine>();
//#else
//            // TODO when we have livekit
//            // livekit = gameObject.AddComponent<Livekit.Standalone.RTCEngine>();
//#endif

//            StartCoroutine(JoinLivekit());
//            return connectTask.Task;
//        }

//        private void OnDestroy()
//        {
//            instanceLookup.Remove(world.Name);
//        }

//        private IEnumerator JoinLivekit()
//        {
//            var wsUrl = string.Format("{0}:{1}", url, port);

//            var joinTask = livekit.Connect(new IRTCEngine.ConnectParams { token = token, url = wsUrl }, OnData, OnParticipantUpdate);

//            while (!joinTask.IsCompleted)
//            {
//                yield return null;
//            }
//            if (joinTask.IsFaulted)
//            {
//                Debug.LogErrorFormat("[WEBTICK CLIENT] Error joining livekit, trying again in 2 seconds: {0}", joinTask.Exception.Message);
//                yield return new WaitForSeconds(2.0f);
//                StartCoroutine(JoinLivekit());
//                yield break;
//            }

//            while (serverSid == null)
//            {
//                yield return new WaitForSeconds(2.0f);
//                Debug.Log("NEIL Waiting for server sid");
//            }

//            Debug.Log("[WEBTICK CLIENT] Client joined LiveKit");
//            var nsdQuery = world.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
//            var nsd = nsdQuery.GetSingleton<NetworkStreamDriver>();
//            var ne = new NetworkEndpoint().WithPort(1234);
//            var bytes = new NativeArray<byte>(BitConverter.GetBytes(MurmurHash2.Hash(serverSid)), Allocator.Temp);
//            ne.SetRawAddressBytes(bytes, NetworkFamily.Ipv4);
//            bytes.Dispose();
//            nsd.Connect(world.EntityManager, ne);
//            connectTask.SetResult(true);
//        }

//        private void OnData(byte[] bytes, string sender, bool reliable)
//        {
//            var nativeBytes = new NativeArray<byte>(bytes, Allocator.TempJob);
//            messageQueue.Enqueue(ReceivedMessage.From(nativeBytes, sender));
//        }

//        private void OnParticipantUpdate(Participant[] participants)
//        {
//            if (serverSid != null)
//            {
//                return;
//            }
//            foreach (var p in participants)
//            {
//                if (p.identity == "server")
//                {
//                    serverSid = p.sid;
//                    break;
//                }
//            }
//        }

//        private void SendData(byte[] bytes, bool reliable)
//        {
//            if (reliable)
//            {
//                livekit.SendReliableMessage(bytes, new string[] { serverSid });
//            }
//            else
//            {
//                livekit.SendLossyMessage(bytes, new string[] { serverSid });
//            }
//        }

//        public void Send(byte[] bytes, string recipient)
//        {
//            SendData(bytes, false);
//        }
    }
}

