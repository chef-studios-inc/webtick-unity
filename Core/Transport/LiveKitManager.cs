using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WebTick.Transport
{
#if UNITY_WEBGL
    public class LiveKitManager : MonoBehaviour
    {
        public ConcurrentQueue<byte[]> receiveQueue = new ConcurrentQueue<byte[]>();
        public bool isReady = false;
        private LiveKit.LiveKit liveKit;
        private TaskCompletionSource<bool> connectedTask;
        private TaskCompletionSource<bool> livekitCreatedTask =new TaskCompletionSource<bool>();

        private void Start()
        {
            liveKit = gameObject.AddComponent<LiveKit.LiveKit>();
            liveKit.onConnected.AddListener(OnConnected);
            liveKit.onServerData.AddListener(ServerDataReceived);
            livekitCreatedTask.SetResult(true);
        }

        private void OnDestroy()
        {
            liveKit.onConnected.RemoveListener(OnConnected);
            liveKit.onServerData.RemoveListener(ServerDataReceived);
        }

        public async Task Connect(string url, string token)
        {
            await livekitCreatedTask.Task;
            Debug.Log("Connecting to livekit room");
            liveKit.ConnectToRoom(url, token);
            connectedTask = new TaskCompletionSource<bool>();
            await connectedTask.Task;
            Debug.Log("Connected to livekit room adsf");
        }

        public void SendMessageToServer(byte[] msg)
        {
            liveKit.SendMessageToServer(msg);
        }

        private void OnConnected()
        {
            Debug.Log("Connected to livekit room");
            connectedTask.SetResult(true);
        }

        private void ServerDataReceived(byte[] data)
        {
            receiveQueue.Enqueue(data);
        }
    }

#else
    public class LiveKitManager : MonoBehaviour
    {
        public ConcurrentQueue<byte[]> receiveQueue = new ConcurrentQueue<byte[]>();
        public bool isReady = false;

        public async Task Connect(string url, string token)
        {
            throw new System.NotImplementedException("only available in webgl build");
        }

        public void SendMessageToServer(byte[] msg)
        {
            throw new System.NotImplementedException("only available in webgl build");
        }
    }
#endif
}
