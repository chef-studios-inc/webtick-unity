using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;

namespace WebTick.Livekit.Standalone
{
    public class RTCEngine : MonoBehaviour 
    {
        private bool signalClientJoined = false;

        private delegate void OnParticipantUpdate(Participant[] participants);

        public struct ConnectParams
        {
            public string url;
            public string token;
        }

        public struct Message
        {
            public byte[] data;
            public uint sender;
        }

        const string lossyDataChannel = "_lossy";
        const string reliableDataChannel = "_reliable";

        public string participantSid = null;
        private RTCPeerConnection localPeerConnection; // publisher
        private RTCDataChannel lossyDC = null;
        private RTCDataChannel reliableDC = null;

        private RTCPeerConnection remotePeerConnection; // subscriber
        private RTCDataChannel remoteLossyDC = null;
        private RTCDataChannel remoteReliableDC = null;

        private SignalClient signalClient;

        private RTCConfiguration configuration;

        public ConcurrentQueue<Message> receiveQueue = new ConcurrentQueue<Message>();
        public uint serverSid = 0;

        private Dictionary<uint, string> sidLookup = new Dictionary<uint, string>();

        [System.Serializable]
        private struct CandidateInit
        {
            public string candidate;
            public int sdpMLineIndex;
            public string sdpMid;
        }

        private void Update()
        {
            if (signalClient != null)
            {
                signalClient.UpdateTick();
            }
        }

        private void OnDestroy()
        {
            if (localPeerConnection != null)
            {
                localPeerConnection.Close();
            }
            if (signalClient != null)
            {
                signalClient.Close();
            }
        }

        public async void Connect(ConnectParams connectParams)
        {
            configuration = default;
            configuration.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302", "stun:stun1.l.google.com:19302" } } };

            signalClient = gameObject.AddComponent<SignalClient>();
            signalClient.onJoin += SignalClient_OnJoin;
            signalClient.onTrickle += SignalClient_OnTrickle;
            signalClient.onRemoteTrickle += SignalClient_OnRemoteTrickle;
            signalClient.onAnswer += SignalClient_OnAnswer;
            signalClient.onOffer += SignalClient_OnOffer;
            signalClient.onUpdate += SignalClient_OnUpdate;
            signalClient.Join(connectParams.url, connectParams.token);

            while (!signalClientJoined)
            {
                await Task.Delay(100);
            }

            CreateLocalPeerConnection();
        }

        private void CreateLocalPeerConnection()
        {
            Debug.Log("NEIL creating local pc");
            localPeerConnection = new RTCPeerConnection(ref configuration);
            Debug.Log("NEIL created local pc");

            lossyDC = localPeerConnection.CreateDataChannel(lossyDataChannel, new RTCDataChannelInit { maxRetransmits = 0, ordered = true });
            reliableDC = localPeerConnection.CreateDataChannel(reliableDataChannel, new RTCDataChannelInit { ordered = true });

            localPeerConnection.OnNegotiationNeeded = () =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] - on negotiation needed");
                StartCoroutine(PublisherCreateAndSendOffer());
            };

            localPeerConnection.OnIceCandidate = async e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] on ice candidate: {0}", e.Candidate);
                LiveKit.Proto.TrickleRequest trickleReq = new LiveKit.Proto.TrickleRequest();
                CandidateInit c;
                c.candidate = e.Candidate;
                c.sdpMid = null;
                c.sdpMLineIndex = 0;
                trickleReq.CandidateInit = JsonUtility.ToJson(c);
                trickleReq.Target = LiveKit.Proto.SignalTarget.Publisher;
                await signalClient.SendIceCandidate(trickleReq);
            };

            localPeerConnection.OnIceConnectionChange = e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] on ice connection change: {0}", e.ToString());
            };

            localPeerConnection.OnIceGatheringStateChange = e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] on ice gathering state change: {0}", e);
            };

        }

        private IEnumerator PublisherCreateAndSendOffer()
        {
            if (localPeerConnection == null)
            {
                Debug.LogError("TODO: Local peer connection null");
                yield break;
            }

            var op = localPeerConnection.CreateOffer();
            yield return op;

            if (op.IsError)
            {
                Debug.LogError("TODO: handle error here");
                yield break;
            }

            var offer = op.Desc;
            Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] setting local description: {0}", offer.sdp);
            var setLDOp = localPeerConnection.SetLocalDescription(ref offer);
            yield return setLDOp;
            LiveKit.Proto.SessionDescription protoSessionDescription = new LiveKit.Proto.SessionDescription();
            protoSessionDescription.Sdp = offer.sdp;
            protoSessionDescription.Type = "offer";
            var sendOfferTask = signalClient.SendOffer(protoSessionDescription);
            while (!sendOfferTask.IsCompleted)
            {
                yield return null;
            }
            Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] sent offer");
        }

        #region LiveKitSignalClient Handler

        private void SignalClient_OnJoin(object sender, LiveKit.Proto.JoinResponse joinResponse)
        {
            Debug.LogFormat("[LICEKIT ENGINE] on join");
            participantSid = joinResponse.Participant.Sid;
            var participants = joinResponse.OtherParticipants.Select(pi => new Participant { identity = pi.Identity, sid = pi.Sid }).ToArray();
            foreach ( var participant in participants )
            {
                if (participant.identity == "server")
                {
                    serverSid = WebTick.Util.MurmurHash2.Hash(participant.sid);
                }
                sidLookup[WebTick.Util.MurmurHash2.Hash(participant.sid)] = participant.sid;
            }
            signalClientJoined = true;
        }

        private void SignalClient_OnTrickle(object sender, LiveKit.Proto.TrickleRequest trickle)
        {
            var parsed = JsonUtility.FromJson<CandidateInit>(trickle.CandidateInit);
            var candidateInit = new RTCIceCandidateInit();
            candidateInit.candidate = parsed.candidate;
            candidateInit.sdpMid = parsed.sdpMid;
            candidateInit.sdpMLineIndex = parsed.sdpMLineIndex;
            var candidate = new RTCIceCandidate(candidateInit);
            Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] adding ice candidate: {0}", candidate.Candidate.ToString());
            localPeerConnection.AddIceCandidate(candidate);
        }
        private void SignalClient_OnRemoteTrickle(object sender, LiveKit.Proto.TrickleRequest trickle)
        {
            var parsed = JsonUtility.FromJson<CandidateInit>(trickle.CandidateInit);
            var candidateInit = new RTCIceCandidateInit();
            candidateInit.candidate = parsed.candidate;
            candidateInit.sdpMid = parsed.sdpMid;
            candidateInit.sdpMLineIndex = parsed.sdpMLineIndex;
            var candidate = new RTCIceCandidate(candidateInit);
            Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] adding ice candidate: {0}", candidate.Candidate.ToString());
            remotePeerConnection.AddIceCandidate(candidate);
        }

        private void SignalClient_OnAnswer(object sender, LiveKit.Proto.SessionDescription answer)
        {
            if (answer == null)
            {
                Debug.LogError("[LIVEKIT ENGINE] no answer");
                return;
            }
            if (localPeerConnection == null)
            {
                Debug.LogError("TODO: No local peer connection");
                return;
            }
            var desc = new RTCSessionDescription();
            desc.sdp = answer.Sdp;
            // TODO: use the actual type?
            desc.type = RTCSdpType.Answer;
            Debug.LogFormat("[LIVEKIT ENGINE PUBLISHER] setting remote description: {0}", desc.ToString());
            localPeerConnection.SetRemoteDescription(ref desc);
        }

        private void SignalClient_OnUpdate(object sender, LiveKit.Proto.ParticipantUpdate update)
        {
            var participants = update.Participants.Select(pi => new Participant { identity = pi.Identity, sid = pi.Sid }).ToArray();
            foreach (var participant in participants)
            {
                if(participant.identity == "server")
                {
                    serverSid = WebTick.Util.MurmurHash2.Hash(participant.sid);
                }
                sidLookup[WebTick.Util.MurmurHash2.Hash(participant.sid)] = participant.sid;
            }
        }

        private void SignalClient_OnOffer(object sender, LiveKit.Proto.SessionDescription offer)
        {
            remotePeerConnection = new RTCPeerConnection(ref configuration);

            remotePeerConnection.OnNegotiationNeeded = () =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] - on negotiation needed");
            };

            remotePeerConnection.OnIceCandidate = e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] - on ice candidate");
                LiveKit.Proto.TrickleRequest trickleReq = new LiveKit.Proto.TrickleRequest();
                CandidateInit c;
                c.candidate = e.Candidate;
                c.sdpMid = null;
                c.sdpMLineIndex = 0;
                trickleReq.CandidateInit = JsonUtility.ToJson(c);
                trickleReq.Target = LiveKit.Proto.SignalTarget.Subscriber;
                signalClient.SendIceCandidate(trickleReq);
            };

            remotePeerConnection.OnIceConnectionChange = e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] - on ice connection change: {0}", e.ToString());
            };

            remotePeerConnection.OnIceGatheringStateChange = e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] - on ice gathering state change: {0}", e);
            };

            remotePeerConnection.OnTrack = e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] - on track: {0}", e.Track.Id);
            };

            remotePeerConnection.OnDataChannel = e =>
            {
                Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] ondatachannel sub: {0}", e.Label);
                if (e.Label == reliableDataChannel)
                {
                    remoteReliableDC = e;
                    remoteReliableDC.OnMessage = OnReliableMessage;
                }
                else if (e.Label == lossyDataChannel)
                {
                    remoteLossyDC = e;
                    remoteLossyDC.OnMessage = OnLossyMessage;
                }
            };
            StartCoroutine(AsyncOnOffer(offer));
        }

        private IEnumerator AsyncOnOffer(LiveKit.Proto.SessionDescription offer)
        {
            Debug.Log("[LIVEKIT ENGINE SUBSCRIBER] got here");
            if (remotePeerConnection == null)
            {
                Debug.LogError("[LIVEKIT ENGINE SUBSCRIBER]: No remote peer connection");
                yield break;
            }

            var desc = new RTCSessionDescription();
            desc.sdp = offer.Sdp;
            // TODO: use the actual type?
            desc.type = RTCSdpType.Offer;
            Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] setting remote description: {0}", desc.sdp);
            yield return remotePeerConnection.SetRemoteDescription(ref desc);

            //create and send the answer
            var op = remotePeerConnection.CreateAnswer();
            yield return op;

            if (op.IsError)
            {
                Debug.LogError("subscriber - error generating answer");
                yield break;
            }

            var answer = op.Desc;
            remotePeerConnection.SetLocalDescription(ref answer);
            LiveKit.Proto.SessionDescription protoSessionDescription = new LiveKit.Proto.SessionDescription();
            protoSessionDescription.Sdp = answer.sdp;
            protoSessionDescription.Type = "answer";
            var sendAnswerTask = signalClient.SendAnswer(protoSessionDescription);
            while (!sendAnswerTask.IsCompleted)
            {
                yield return null;
            }
            Debug.LogFormat("[LIVEKIT ENGINE SUBSCRIBER] sent offer");
        }

        #endregion

        #region Data Channel

        private void OnLossyMessage(byte[] bytes)
        {
            DispatchData(bytes);
        }

        private void OnReliableMessage(byte[] bytes)
        {
            DispatchData(bytes);
        }

        private void DispatchData(byte[] bytes)
        {
            var dp = LiveKit.Proto.DataPacket.Parser.ParseFrom(bytes);
            if (dp.ValueCase == LiveKit.Proto.DataPacket.ValueOneofCase.User)
            {
                var sender = dp.User.ParticipantSid;
                var payload = dp.User.Payload.ToByteArray();
                receiveQueue.Enqueue(new Message { data = payload, sender = WebTick.Util.MurmurHash2.Hash(sender) });
            }
        }

        public void SendLossyMessage(byte[] bytes, uint recipient)
        {
            if (!sidLookup.TryGetValue(recipient, out var sid))
            {
                Debug.LogWarningFormat("No sid for hash: {0}", recipient);
                return;
            }

            _SendMessage(bytes, new string[] { sid }, false);
        }

        public void SendReliableMessage(byte[] bytes, string[] recipientSids)
        {
            _SendMessage(bytes, recipientSids, true);
        }

        private void _SendMessage(byte[] bytes, string[] recipientSids, bool reliable)
        {
            if (localPeerConnection.ConnectionState != RTCPeerConnectionState.Connected)
            {
                Debug.LogError("TODO - trying to send when peer connection is not connected");
                return;
            }
            var dp = new LiveKit.Proto.DataPacket();
            dp.User = new LiveKit.Proto.UserPacket();
            dp.User.ParticipantSid = this.participantSid;
            dp.User.Payload = ByteString.CopyFrom(bytes);

            if (reliable)
            {
                dp.Kind = LiveKit.Proto.DataPacket.Types.Kind.Reliable;
            }
            else
            {
                dp.Kind = LiveKit.Proto.DataPacket.Types.Kind.Lossy;
            }

            if (recipientSids != null)
            {
                dp.User.DestinationSids.Add(recipientSids);
            }

            if (reliable)
            {
                reliableDC.Send(dp.ToByteArray());
            }
            else
            {
                lossyDC.Send(dp.ToByteArray());
                remoteLossyDC.Send(dp.ToByteArray());
            }
        }



        #endregion
    }
}