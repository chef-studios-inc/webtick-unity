using LiveKit;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WebTick.Transport
{
    public class LiveKitManager : MonoBehaviour
    {
        public ConcurrentQueue<byte[]> receiveQueue = new ConcurrentQueue<byte[]>();
        public bool isReady = false;
        private Room room;
        private RemoteParticipant serverParticipant;

        public async Task Connect(string url, string token)
        {
            var tr = new TaskCompletionSource<bool>();
            StartCoroutine(ConnectCoro(url, token, tr));
            await tr.Task;
        }

        public void SendMessageToServer(byte[] msg)
        {
            if(room.State != ConnectionState.Connected)
            {
                Debug.LogError("Trying to send when room is closed");
                return;
            }

            if(serverParticipant == null)
            {
                Debug.LogError("Trying to send when room no server participant");
                return;
            }

            room.LocalParticipant.PublishData(msg, DataPacketKind.LOSSY, serverParticipant);
        }

        public IEnumerator ConnectCoro(string url, string token, TaskCompletionSource<bool> tr)
        {
            var room = new Room();
            var c = room.Connect(url, token);
            yield return c;

            if (c.IsError)
            {
                tr.SetException(new System.Exception(c.Error.Message));
                yield break;
            }

            room.ParticipantConnected += Room_ParticipantConnected;
            room.ParticipantDisconnected += Room_ParticipantDisconnected;
            room.DataReceived += Room_DataReceived;

            foreach(var p in room.Participants)
            {
                if(p.Value.Identity == "server")
                {
                    serverParticipant = p.Value;
                }
            }

            while(serverParticipant == null)
            {
                Debug.Log("Waiting for server participant");
                yield return null;
            }
            tr.SetResult(true);
        }

        private void Room_ParticipantDisconnected(RemoteParticipant participant)
        {
            if(participant.Identity == "server")
            {
                serverParticipant = null;
            }
        }

        private void Room_ParticipantConnected(RemoteParticipant participant)
        {
            if(participant.Identity == "server")
            {
                serverParticipant = participant;
            }
        }

        private void Room_DataReceived(byte[] data, RemoteParticipant participant, DataPacketKind? kind)
        {
            if(participant.Identity != "server")
            {
                return;
            }

            receiveQueue.Enqueue(data);
        }
    }
}
