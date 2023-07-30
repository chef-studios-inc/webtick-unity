using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using WebTick.Core.Server;
using static WebTick.Core.Server.ServerConnectSystem;

namespace WebTick.Transport
{
    public struct LiveKitStandaloneServerNetworkInterface : INetworkInterface
    {
#if UNITY_WEBGL && !UNITY_EDITOR
       public LiveKitStandaloneServerNetworkInterface(EntityManager em)
        {
        }

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

#else
        private uint engineHandle; 
        private EntityQuery serverGameObjectQuery;

        public LiveKitStandaloneServerNetworkInterface(EntityManager em)
        {
            serverGameObjectQuery = em.CreateEntityQuery(typeof(ServerGameObject));
            this.engineHandle = 0;
        }

        public NetworkEndpoint LocalEndpoint => throw new NotImplementedException();

        public int Bind(NetworkEndpoint endpoint)
        {
            return 0;
        }

        public void Dispose()
        {
            if(this.engineHandle != 0)
            {
                RTCEngineManager.CleanupEngine(engineHandle);
            }
            this.engineHandle = 0;
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
            if(engineHandle == 0 && serverGameObjectQuery.HasSingleton<ServerGameObject>())
            {
                var clientGO = serverGameObjectQuery.GetSingleton<ServerGameObject>();
                this.engineHandle = clientGO.engineHandle; 
            }
            return new ReceiveJob { engineHandle=engineHandle, receiveQueue = arguments.ReceiveQueue }.Schedule(dep);
        }

        public unsafe JobHandle ScheduleSend(ref SendJobArguments arguments, JobHandle dep)
        {
            if(engineHandle == 0 && serverGameObjectQuery.HasSingleton<ServerGameObject>())
            {
                var clientGO = serverGameObjectQuery.GetSingleton<ServerGameObject>();
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
                var recipient = BitConverter.ToUInt32(msg.EndpointRef.GetRawAddressBytes());
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
 #endif
    }
}
