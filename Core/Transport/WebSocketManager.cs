using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace WebTick.Transport
{
#if !UNITY_WEBGL
    public class WebSocketManager: MonoBehaviour 
    {
        private NativeWebSocket.WebSocket ws = null;

        public ConcurrentQueue<Message> receiveQueue = new ConcurrentQueue<Message>();
        public bool isReady = false;
        public bool isError = false;
        public bool isClosed = false;
        public string error = null; 

        public struct Message
        {
            public uint sender;
            public ArraySegment<byte> payload;
        }

        public async Task Connect(string url)
        {
            ws = new NativeWebSocket.WebSocket(url);
            ws.OnMessage += Ws_OnMessage;
            ws.OnClose += Ws_OnClose;
            ws.OnError += Ws_OnError;
            Debug.LogFormat("Connecting to websocket: {0}", url);
            var tr = new TaskCompletionSource<bool>();
            _Connect();
            StartCoroutine(WaitForReadyMessage(tr));
            Debug.LogFormat("Connected to websocket: {0}", url);
            await tr.Task;
        }

        private async void _Connect()
        {
            await ws.Connect();
        }

        private void Ws_OnError(string errorMsg)
        {
            Debug.LogErrorFormat("Ws Error: {0}", errorMsg);
            isError = true;
            error = errorMsg;
        }

        private void Ws_OnClose(NativeWebSocket.WebSocketCloseCode closeCode)
        {
            Debug.LogFormat("Ws On close: {0}", closeCode);
            isClosed = true;
            throw new NotImplementedException();
        }

        private void Ws_OnMessage(byte[] data)
        {
            if (!isReady)
            {
                var ready = Encoding.UTF8.GetString(data, 0, data.Length);
                if (ready == "READY")
                {
                    isReady = true;
                }
                return;
            }
            var sender = BitConverter.ToUInt32(data);
            var payload = new ArraySegment<byte>(data, 4, data.Length - 4);
            var msg = new Message
            {
                sender = sender,
                payload = payload
            };
            receiveQueue.Enqueue(msg);
        }

        private IEnumerator WaitForReadyMessage(TaskCompletionSource<bool> tr)
        {
            while(!isReady)
            {
                yield return null;
            }
            tr.SetResult(true);
        }

        public async void SendMessage(byte[] payload, uint recipient)
        {
            if(ws.State != NativeWebSocket.WebSocketState.Open)
            {
                Debug.LogError("Websocket closed when trying to send message");
                return;
            }

            var array = new byte[4 + payload.Length]; ;
            Buffer.BlockCopy(BitConverter.GetBytes(recipient), 0, array, 0, 4);
            Buffer.BlockCopy(payload, 0, array, 4, payload.Length);
            await ws.Send(array);
        }

        void Update()
        {
            ws.DispatchMessageQueue();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private async void Cleanup()
        {
            await this.ws.Close();
        }

    }
#else
    public class WebSocketManager : MonoBehaviour
    {
        private NativeWebSocket.WebSocket ws = null;

        public ConcurrentQueue<Message> receiveQueue = new ConcurrentQueue<Message>();
        public bool isReady = false;
        public bool isError = false;
        public bool isClosed = false;
        public string error = null;

        public struct Message
        {
            public uint sender;
            public ArraySegment<byte> payload;
        }

        public Task Connect(string url)
        {
            throw new System.NotImplementedException("not available on webgl");
        }

        public void SendMessage(byte[] payload, uint recipient)
        {
            throw new System.NotImplementedException("not available on webgl");
        }

    }
#endif
}
