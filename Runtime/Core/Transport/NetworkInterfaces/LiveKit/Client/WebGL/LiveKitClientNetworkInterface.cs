using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using System;
using System.Runtime.InteropServices;
using System.Xml;
using Unity.Entities;
using WebTick.Core.Transport.NetworkInterfaces.LiveKit.Client;

namespace WebTick.Transport
{
#if UNITY_WEBGL && !UNITY_EDITOR
    public struct LiveKitClientNetworkInterface : INetworkInterface
    {
        struct LiveKit
        {
            [DllImport("__Internal")]
            public static extern void LK_ConnectToRoom(string url, string token);

            [DllImport("__Internal")]
            public static extern void LK_SendMessageToServer(byte[] payload, int payloadSize);

            [DllImport("__Internal")]
            public static extern int LK_ReceiveData(IntPtr data, int size);

            [DllImport("__Internal")]
            public static extern bool LK_IsConnected();

            [DllImport("__Internal")]
            public static extern bool LK_Cleanup();

        }

        private EntityQuery clientConnectionDetailsQuery;

        public LiveKitClientNetworkInterface(EntityManager em)
        {
            clientConnectionDetailsQuery = em.CreateEntityQuery(typeof(ClientConnectionDetails));
        }

        public NetworkEndpoint LocalEndpoint
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        public int Bind(NetworkEndpoint endpoint)
        {
            return 0;
        }

        public void Dispose()
        {
        }

        public int Initialize(ref NetworkSettings settings, ref int packetPadding)
        {
            return 0;
        }

        public int Listen()
        {
            return 0;
        }

        public JobHandle ScheduleReceive(ref ReceiveJobArguments arguments, JobHandle dep)
        {
            return new ReceiveJob { receiveQueue = arguments.ReceiveQueue }.Schedule(dep);
        }

        public unsafe JobHandle ScheduleSend(ref SendJobArguments arguments, JobHandle dep)
        {
            dep.Complete();
            if(!LiveKit.LK_IsConnected())
            {
                if(clientConnectionDetailsQuery.TryGetSingleton<ClientConnectionDetails>(out var ccd))
                {
                    LiveKit.LK_ConnectToRoom(ccd.wsUrl.ToString(), ccd.token.ToString());
                }
                return dep;
            }

            for (int i = 0; i < arguments.SendQueue.Count; i++)
            {
                var msg = arguments.SendQueue[i];

                if (msg.Length == 0)
                {
                    continue;
                }

                var nativeByteArray = new NativeArray<byte>(msg.Length, Allocator.Temp);
                msg.CopyPayload(nativeByteArray.GetUnsafePtr(), msg.Length);
                LiveKit.LK_SendMessageToServer(nativeByteArray.ToArray(), msg.Length);
                nativeByteArray.Dispose();
            }
            arguments.SendQueue.Clear();
            return dep;
        }

        struct ReceiveJob : IJob
        {
            public NativeArray<byte> message;
            public PacketsQueue receiveQueue;

            public unsafe void Execute()
            {
                if (!LiveKit.LK_IsConnected())
                {
                    return;
                }

                while (true)
                {
                    if (!receiveQueue.EnqueuePacket(out var packetProcessor))
                    {
                        break;
                    }

                    var nbytes = LiveKit.LK_ReceiveData((IntPtr)(byte*)packetProcessor.GetUnsafePayloadPtr() + packetProcessor.Offset, packetProcessor.BytesAvailableAtEnd);

                    if (nbytes > 0)
                    {
                        packetProcessor.AppendToPayload((void*)((IntPtr)(byte*)packetProcessor.GetUnsafePayloadPtr() + packetProcessor.Offset), nbytes); // TODO this is an extra copy
                    }
                    else
                    {
                        packetProcessor.Drop();
                        break;
                    }
                }
            }
        }
    }
#else
    public struct LiveKitClientNetworkInterface : INetworkInterface
    {
        public NetworkEndpoint LocalEndpoint => throw new NotImplementedException();

        public int Bind(NetworkEndpoint endpoint)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int Initialize(ref NetworkSettings settings, ref int packetPadding)
        {
            throw new NotImplementedException();
        }

        public int Listen()
        {
            throw new NotImplementedException();
        }

        public JobHandle ScheduleReceive(ref ReceiveJobArguments arguments, JobHandle dep)
        {
            throw new NotImplementedException();
        }

        public JobHandle ScheduleSend(ref SendJobArguments arguments, JobHandle dep)
        {
            throw new NotImplementedException();
        }
    }
#endif
}

