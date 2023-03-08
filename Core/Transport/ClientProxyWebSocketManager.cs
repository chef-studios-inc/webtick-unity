#if !UNITY_WEBGL
using NativeWebSocket;
#endif
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
    public class ClientProxyWebSocketManager: MonoBehaviour 
    {
        private NativeWebSocket.WebSocket ws = null;

        public ConcurrentQueue<byte[]> receiveQueue = new ConcurrentQueue<byte[]>();
        public bool isReady = false;
        public bool isError = false;
        public bool isClosed = false;
        public string error = null; 

        public async Task Connect(string url)
        {
            ws = new NativeWebSocket.WebSocket(url);
            ws.OnMessage += Ws_OnMessage;
            ws.OnClose += Ws_OnClose;
            ws.OnError += Ws_OnError;
            Debug.Log("[CLIENT_PROXY] Connecting to websocket...");
            var tr = new TaskCompletionSource<bool>();
            _Connect();
            StartCoroutine(WaitForReadyMessage(tr));
            Debug.Log("[CLIENT_PROXY] Connected to websocket");
            await tr.Task;
        }

        private async void _Connect()
        {
            await ws.Connect();
        }

        private void Ws_OnError(string errorMsg)
        {
            Debug.LogErrorFormat("[CLIENT_PROXY] Ws Error: {0}", errorMsg);
            isError = true;
            error = errorMsg;
        }

        private void Ws_OnClose(NativeWebSocket.WebSocketCloseCode closeCode)
        {
            Debug.LogFormat("[CLIENT_PROXY] Ws On close: {0}", closeCode);
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
            Debug.LogFormat("[CLIENT] on message: {0} {1}", data[0], data[1]);

            receiveQueue.Enqueue(data);
        }

        private IEnumerator WaitForReadyMessage(TaskCompletionSource<bool> tr)
        {
            while(!isReady)
            {
                yield return null;
            }
            tr.SetResult(true);
        }

        public async void SendMessage(byte[] payload)
        {
            if(ws.State != NativeWebSocket.WebSocketState.Open)
            {
                Debug.LogError("[CLIENT_PROXY] Websocket closed when trying to send message");
                return;
            }

            var array = new byte[payload.Length]; ;
            Buffer.BlockCopy(payload, 0, array, 0, payload.Length);
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
}
