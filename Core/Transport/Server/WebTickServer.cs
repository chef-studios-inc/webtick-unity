using System.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Collections;
using WebTick.Transport;
using System.Threading.Tasks;

namespace WebTick.Server
{
    // TODO when we get livekit
    public class WebTickServer : MonoBehaviour 
    {
//        private LivekitServer server = null;
//        private IRTCEngine livekit = null;
//        private World world;
//        public static WebTickServer instance;

//        private string url;
//        private ushort port;
//        private ushort udpPort;
//        private ushort tcpPort;
//        private string apiKey;
//        private string apiSecret;

//        public NativeParallelHashMap<FixedString128Bytes, uint> sidToInt = new NativeParallelHashMap<FixedString128Bytes, uint>(1000, Allocator.Persistent);
//        public NativeParallelHashMap<uint, FixedString128Bytes> intToSid = new NativeParallelHashMap<uint, FixedString128Bytes>(1000, Allocator.Persistent);

//        private NativeQueue<ReceivedMessage> messageQueue = new NativeQueue<ReceivedMessage>(Allocator.Persistent);
//        public NativeQueue<ReceivedMessage> receiveQueue => messageQueue;

//        private TaskCompletionSource<bool> listenTask = new TaskCompletionSource<bool>();

//        public Task Listen(string url, ushort port, ushort udpPort, ushort tcpPort, string apiKey, string apiSecret, World world)
//        {
//            this.url = url;
//            this.port = port;
//            this.udpPort = udpPort;
//            this.tcpPort = tcpPort;
//            this.apiKey = apiKey;
//            this.apiSecret = apiSecret;

//            instance = this;
//            this.world = world;

//            server = gameObject.AddComponent<LivekitServer>();
//            server.StartServer(port, udpPort, tcpPort, apiSecret);
//#if !UNITY_WEBGL || UNITY_EDITOR
//            // TODO when we have livekit
//            // livekit = gameObject.AddComponent<Livekit.Standalone.RTCEngine>();
//#else
//            throw new System.Exception("Server not supported on WebGL");
//#endif
//            StartCoroutine(JoinLivekit());
//            return listenTask.Task;
//        }

//        private IEnumerator JoinLivekit()
//        {
//            var token = LivekitTokenGenerator.GenerateToken("server", apiKey, apiSecret);
//            var wsUrl = string.Format("{0}:{1}", url, port);
//            var joinTask = livekit.Connect(new IRTCEngine.ConnectParams { token=token, url=wsUrl}, OnData, null);
//            while (!joinTask.IsCompleted)
//            {
//                yield return null;
//            }
//            if (joinTask.IsFaulted)
//            {
//                Debug.LogErrorFormat("[WEBTICK SERVER] Error joining livekit, trying again in 2 seconds: {0}", joinTask.Exception.Message);
//                yield return new WaitForSeconds(2.0f);
//                StartCoroutine(JoinLivekit());
//                yield break;
//            }
//            Debug.Log("[WEBTICK SERVER] Server joined LiveKit");
//            var nsdQuery = world.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
//            var nsd = nsdQuery.GetSingleton<NetworkStreamDriver>();
//            nsd.Listen(NetworkEndpoint.AnyIpv4);
//            listenTask.SetResult(true);
//        }

//        private unsafe void OnData(byte[] bytes, string sender, bool reliable)
//        {
//            var nativeBytes = new NativeArray<byte>(bytes.Length, Allocator.TempJob);
//            nativeBytes.CopyFrom(bytes);

//            // TODO clean these up occasionally?
//            if (!sidToInt.ContainsKey(sender))
//            {
//                sidToInt[sender] = WebTick.Util.MurmurHash2.Hash(sender);
//                intToSid[sidToInt[sender]] = sender;
//            }

//            FixedString128Bytes fixedSender = sender;
//            messageQueue.Enqueue(ReceivedMessage.From(nativeBytes, fixedSender));
//        }

//        private void SendData(byte[] bytes, bool reliable, string[] recipientSids)
//        {
//            if (reliable)
//            {
//                livekit.SendReliableMessage(bytes, recipientSids);
//            }
//            else
//            {
//                livekit.SendLossyMessage(bytes, recipientSids);
//            }
//        }

//        private void OnDestroy()
//        {
//            instance = null;
//            if (server != null)
//            {
//                Destroy(server);
//            }
//        }

//        public void Send(byte[] bytes, string recipient)
//        {
//            SendData(bytes, false, new string[] { recipient });
//        }
    }
}

