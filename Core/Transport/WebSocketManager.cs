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
    public class WebSocketManager: MonoBehaviour 
    {
        private NativeWebSocket.WebSocket ws = null;

        public ConcurrentQueue<Message> receiveQueue = new ConcurrentQueue<Message>();
        public bool isReady = false;
        public bool isError = false;
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
            Debug.Log("Connecting to websocket...");
            var tr = new TaskCompletionSource<bool>();
            _Connect();
            StartCoroutine(WaitForReadyMessage(tr));
            Debug.Log("Connected to websocket");
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
            var msg = new Message
            {
                sender = sender,
                payload = new ArraySegment<byte>(data, 4, data.Length - 4)
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

            var totalPayload = new ArraySegment<byte>();
            totalPayload.Concat(BitConverter.GetBytes(recipient));
            totalPayload.Concat(payload);
            await ws.Send(totalPayload.Array);
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            ws.DispatchMessageQueue();
#endif
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
}
