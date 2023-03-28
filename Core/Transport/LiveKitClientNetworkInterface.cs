using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using System;
using Unity.Entities;
using UnityEngine;

namespace WebTick.Transport
{
    public struct LiveKitClientNetworkInterface : INetworkInterface
    {
        public static class Dependencies
        {
            public static LiveKitManager livekitManager;
        }

        public LiveKitClientNetworkInterface(LiveKitManager livekitManager)
        {
            Dependencies.livekitManager = livekitManager;
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
            while (!Dependencies.livekitManager.receiveQueue.IsEmpty)
            {
                Dependencies.livekitManager.receiveQueue.TryDequeue(out var bytes);
                var msg = new NativeArray<byte>(bytes.Length, Allocator.TempJob);
                msg.CopyFrom(bytes);
                previousJob = new ReceiveJob { receiveQueue = arguments.ReceiveQueue, message = msg }.Schedule(previousJob);
                msg.Dispose(previousJob);
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
                Dependencies.livekitManager.SendMessageToServer(nativeByteArray.ToArray());
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
                if (receiveQueue.EnqueuePacket(out var pp))
                {
                    var address = new NativeArray<byte>(4, Allocator.Temp);
                    address[0] = 1;
                    address[1] = 2;
                    address[2] = 3;
                    address[3] = 4;
                    pp.EndpointRef.SetRawAddressBytes(address, NetworkFamily.Ipv4);
                    pp.EndpointRef = pp.EndpointRef.WithPort(1234);
                    pp.AppendToPayload(message.GetUnsafeReadOnlyPtr(), message.Length);
                    address.Dispose();
                };
            }
        }
    }
}

