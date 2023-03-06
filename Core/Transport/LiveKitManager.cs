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
        //private Room room;
        //private RemoteParticipant serverParticipant;
        private LiveKit.LiveKit liveKit;
        private TaskCompletionSource<bool> connectedTask;

        private void Start()
        {
            liveKit = gameObject.AddComponent<LiveKit.LiveKit>();
            liveKit.onConnected.AddListener(OnConnected);
        }

        public async Task Connect(string url, string token)
        {
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

        //private void Room_DataReceived(byte[] data, RemoteParticipant participant, DataPacketKind? kind)
        //{
        //    //if(participant.Identity != "server")
        //    //{
        //    //    return;
        //    //}

        //    //receiveQueue.Enqueue(data);
        //}
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
