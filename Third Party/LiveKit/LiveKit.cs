using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace LiveKit
{
    public class LiveKit : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void LK_InitializeBridge(string gameObjectName);

        [DllImport("__Internal")]
        private static extern void LK_ConnectToRoom(string url, string token);

        [DllImport("__Internal")]
        private static extern void LK_SendMessageToServer(byte[] payload, int payloadSize);


        public UnityEvent onConnected = new UnityEvent();
        public UnityEvent<byte[]> onServerData = new UnityEvent<byte[]>();

        private void Start()
        {
            LK_InitializeBridge(gameObject.name);
        }

        public void ConnectToRoom(string url, string token)
        {
            Debug.Log("ConnectToRoom");
            LK_ConnectToRoom(url, token);
        }
        public void SendMessageToServer(byte[] msg)
        {
            LK_SendMessageToServer(msg, msg.Length);
        }

        [Preserve]
        private void Web_ConnectedCallback()
        {
            Debug.Log("Connected");
            onConnected.Invoke();
        }

        [Preserve]
        private void Web_DisconnectedCallback()
        {
            Debug.Log("Disconnected");
        }

        [Preserve]
        private void Web_DataReceived(string payload)
        {
            byte[] bytes = System.Convert.FromBase64String(payload);
            onServerData.Invoke(bytes);
        }
    }
}

