using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace WebTick.Transport
{
    public struct LiveKitStandaloneClientNetworkInterface : INetworkInterface
    {
        private EntityQuery clientGameObjectQuery;
        private uint engineHandle;

        public LiveKitStandaloneClientNetworkInterface(EntityManager em)
        {
            clientGameObjectQuery = em.CreateEntityQuery(typeof(ClientGameObject));
            this.engineHandle = 0;
        }

        public NetworkEndpoint LocalEndpoint => throw new NotImplementedException();

        public int Bind(NetworkEndpoint endpoint)
        {
            return 0;
        }

        public void Dispose()
        {
            //TODO?
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
            if(engineHandle == 0 && clientGameObjectQuery.HasSingleton<ClientGameObject>())
            {
                var clientGO = clientGameObjectQuery.GetSingleton<ClientGameObject>();
                this.engineHandle = clientGO.engineHandle; 
            }
            return new ReceiveJob { engineHandle=engineHandle, receiveQueue = arguments.ReceiveQueue }.Schedule(dep);
        }

        public unsafe JobHandle ScheduleSend(ref SendJobArguments arguments, JobHandle dep)
        {
            if(engineHandle == 0 && clientGameObjectQuery.HasSingleton<ClientGameObject>())
            {
                var clientGO = clientGameObjectQuery.GetSingleton<ClientGameObject>();
                this.engineHandle = clientGO.engineHandle; 
            }
            dep.Complete();
            for (int i = 0; i < arguments.SendQueue.Count; i++)
            {
                var msg = arguments.SendQueue[i];

                if (msg.Length == 0 || engineHandle == 0)
                {
                    continue;
                }

                var nativeByteArray = new NativeArray<byte>(msg.Length, Allocator.Temp);
                msg.CopyPayload(nativeByteArray.GetUnsafePtr(), msg.Length);
                var recipient = RTCEngineManager.GetServerId(engineHandle);
                RTCEngineManager.SendData(engineHandle, nativeByteArray.ToArray(), recipient);
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
                while (true)
                {
                    if (!receiveQueue.EnqueuePacket(out var packetProcessor))
                    {
                        break;
                    }

                    if(engineHandle == 0)
                    {
                        packetProcessor.Drop();
                        break;
                    }

                    if (!RTCEngineManager.IsConnected(engineHandle))
                    {
                        packetProcessor.Drop();
                        break;
                    }

                    if (!RTCEngineManager.ReceiveData(engineHandle, out var msg))
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
