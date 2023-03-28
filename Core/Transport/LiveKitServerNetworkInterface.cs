using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using System;
using Unity.Entities;
using UnityEngine;
using System.Linq;

namespace WebTick.Transport
{


    public struct LiveKitServerNetworkInterface : INetworkInterface
    {
        public static class Dependencies
        {
            public static WebSocketManager websocketManager;
        }


        public LiveKitServerNetworkInterface(WebSocketManager websocketManager)
        {
            Dependencies.websocketManager = websocketManager;
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
            JobHandle previousJob = dep;
            while (!Dependencies.websocketManager.receiveQueue.IsEmpty)
            {
                Dependencies.websocketManager.receiveQueue.TryDequeue(out var msg);
                var array = msg.payload.ToArray();
                var na = new NativeArray<byte>(array, Allocator.TempJob);
                previousJob = new ReceiveJob { receiveQueue = arguments.ReceiveQueue, message = na, intSid = msg.sender }.Schedule(previousJob);
                na.Dispose(previousJob);
            }

            return previousJob; 
        }

        public unsafe JobHandle ScheduleSend(ref SendJobArguments arguments, JobHandle dep)
        {

            dep.Complete();
            for (int i = 0; i < arguments.SendQueue.Count; i++)
            {
                var msg = arguments.SendQueue[i];

                if (msg.Length == 0)
                {
                    continue;
                }


                var nativeByteArray = new NativeArray<byte>(msg.Length, Allocator.Temp);
                msg.CopyPayload(nativeByteArray.GetUnsafePtr(), msg.Length);
                var addressBytes = msg.EndpointRef.GetRawAddressBytes();
                if (!BitConverter.IsLittleEndian)
                {
                    Debug.LogWarning("Reversing endianess");
                    addressBytes.Reverse();
                }
                var recipientSid = BitConverter.ToUInt32(addressBytes);
                Dependencies.websocketManager.SendMessage(nativeByteArray.ToArray(), recipientSid);
                nativeByteArray.Dispose();
            }
            arguments.SendQueue.Clear();
            return dep;
        }

        struct ReceiveJob : IJob
        {
            public NativeArray<byte> message;
            public uint intSid;
            public PacketsQueue receiveQueue;

            public unsafe void Execute()
            {
                if (receiveQueue.EnqueuePacket(out var pp))
                {
                    var addressBytes = new NativeArray<byte>(BitConverter.GetBytes(intSid), Allocator.Temp);
                    if(!BitConverter.IsLittleEndian)
                    {
                        Debug.LogWarning("Reversing endianess");
                        addressBytes.Reverse();
                    }
                    pp.EndpointRef.SetRawAddressBytes(addressBytes, NetworkFamily.Ipv4);
                    pp.EndpointRef = pp.EndpointRef.WithPort(1234);
                    addressBytes.Dispose();
                    pp.AppendToPayload(message.GetUnsafeReadOnlyPtr(), message.Length);
                };
            }
        }
    }
}

