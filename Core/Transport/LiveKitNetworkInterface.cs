using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using WebTick.Server;
using WebTick.Client;
using System;
using Unity.Entities;

namespace WebTick.Transport
{
    public struct LiveKitNetworkInterface : INetworkInterface
    {
        public enum Mode
        {
            Client,
            Server
        }

        private Mode mode;
        private FixedString128Bytes worldName;

        public LiveKitNetworkInterface(Mode mode, World world)
        {
            this.mode = mode;
            worldName = world.Name;
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
            return dep;
            // TODO when we get livekit
            //if (mode == Mode.Server)
            //{
            //    if (WebTickServer.instance == null)
            //    {
            //        return dep;
            //    }
            //    JobHandle previousJob = dep;
            //    while(WebTickServer.instance.receiveQueue.Count > 0)
            //    {
            //        var msg = WebTickServer.instance.receiveQueue.Dequeue();
            //        var intSid = WebTickServer.instance.sidToInt[msg.sender];
            //        var job = new ReceiveJob { receiveQueue = arguments.ReceiveQueue, message = msg, intSid = intSid }.Schedule(previousJob);
            //        msg.Dispose(job);
            //        previousJob = job;
            //    }

            //    return previousJob;

            //}
            //else
            //{
            //    var client = WebTickClient.GetInstance(worldName);
            //    if (client == null)
            //    {
            //        return dep;
            //    }
            //    JobHandle previousJob = dep;
            //    while(client.receiveQueue.Count > 0)
            //    {
            //        var msg = client.receiveQueue.Dequeue();
            //        var job = new ReceiveJob { receiveQueue = arguments.ReceiveQueue, message = msg, intSid = 0 }.Schedule(previousJob);
            //        msg.Dispose(job);
            //        previousJob = job;
            //    }

            //    return previousJob;
            //}
        }

        public unsafe JobHandle ScheduleSend(ref SendJobArguments arguments, JobHandle dep)
        {
            return dep;
            // TODO when we get livekit
            //if (mode == Mode.Server)
            //{
            //    if (WebTickServer.instance == null)
            //    {
            //        return dep;
            //    }
            //    dep.Complete();
            //    // TODO can this be jobified?
            //    for(int i = 0; i < arguments.SendQueue.Count; i++)
            //    {
            //        var msg = arguments.SendQueue[i];

            //        if(msg.Length == 0)
            //        {
            //            continue;
            //        }

            //        var nativeByteArray = new NativeArray<byte>(msg.Length, Allocator.Temp);
            //        msg.CopyPayload(nativeByteArray.GetUnsafePtr(), msg.Length);
            //        var recipientSid = WebTickServer.instance.intToSid[BitConverter.ToUInt32(msg.EndpointRef.GetRawAddressBytes())];
            //        WebTickServer.instance.Send(nativeByteArray.ToArray(), recipientSid.ToString());
            //        nativeByteArray.Dispose();
            //    }
            //    arguments.SendQueue.Clear();
            //    return dep;
            //}
            //else
            //{
            //    var client = WebTickClient.GetInstance(worldName);
            //    if (client == null)
            //    {
            //        return dep;
            //    }
            //    dep.Complete();
            //    // TODO can this be jobified?
            //    for(int i = 0; i < arguments.SendQueue.Count; i++)
            //    {
            //        var msg = arguments.SendQueue[i];

            //        if(msg.Length == 0)
            //        {
            //            continue;
            //        }

            //        var nativeByteArray = new NativeArray<byte>(msg.Length, Allocator.Temp);
            //        msg.CopyPayload(nativeByteArray.GetUnsafePtr(), msg.Length);
            //        var recipient = msg.EndpointRef.Address;
            //        client.Send(nativeByteArray.ToArray(), recipient);
            //        nativeByteArray.Dispose();
            //    }
            //    arguments.SendQueue.Clear();
            //    return dep;
            //}
        }

        struct ReceiveJob : IJob
        {
            public ReceivedMessage message;
            public uint intSid;
            public PacketsQueue receiveQueue;

            public unsafe void Execute()
            {
                if (receiveQueue.EnqueuePacket(out var pp))
                {
                    var addressBytes = new NativeArray<byte>(BitConverter.GetBytes(intSid), Allocator.Temp);
                    pp.EndpointRef.SetRawAddressBytes(addressBytes, NetworkFamily.Ipv4);
                    pp.EndpointRef = pp.EndpointRef.WithPort(1234);
                    addressBytes.Dispose();
                    pp.AppendToPayload(message.payload.GetUnsafeReadOnlyPtr(), message.payload.Length);
                };
            }
        }
    }
}

