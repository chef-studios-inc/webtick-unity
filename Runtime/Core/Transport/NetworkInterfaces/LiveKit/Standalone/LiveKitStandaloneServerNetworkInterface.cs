using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace WebTick.Transport
{
    public struct LiveKitStandaloneServerNetworkInterface : INetworkInterface
    {
        private uint handle; 
        public LiveKitStandaloneServerNetworkInterface(uint handle)
        {
            this.handle = handle;
        }

        public NetworkEndpoint LocalEndpoint => throw new NotImplementedException();

        public int Bind(NetworkEndpoint endpoint)
        {
            throw new NotImplementedException();
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
            return new ReceiveJob { engineHandle=handle, receiveQueue = arguments.ReceiveQueue }.Schedule(dep);
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
                var recipient = BitConverter.ToUInt32(msg.EndpointRef.GetRawAddressBytes());
                RTCEngineManager.SendData(handle, nativeByteArray.ToArray(), recipient);
                nativeByteArray.Dispose();
            }
            arguments.SendQueue.Clear();
            return dep;
        }

        struct ReceiveJob : IJob
        {
            public uint engineHandle;
            public PacketsQueue receiveQueue;

            public unsafe void Execute()
            {
                if (!RTCEngineManager.IsConnected(engineHandle))
                {
                    return;
                }

                while (true)
                {
                    if (!receiveQueue.EnqueuePacket(out var packetProcessor))
                    {
                        break;
                    }

                    if(!RTCEngineManager.ReceiveData(engineHandle, out var msg))
                    {
                        break;
                    }

                    if(msg.data.Length == 0)
                    {
                        packetProcessor.Drop();
                        break;
                    }
                    var recipientBytes = BitConverter.GetBytes(msg.sender);
                    packetProcessor.EndpointRef.SetRawAddressBytes(new NativeArray<byte>(recipientBytes, Allocator.Temp));
                    packetProcessor.AppendToPayload(new NativeArray<byte>(msg.data, Allocator.Temp).GetUnsafePtr(), msg.data.Length);
                }
            }
        }
    }
}
