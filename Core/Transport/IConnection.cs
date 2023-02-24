using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace WebTick.Transport
{
    public struct ReceivedMessage 
    {
        public FixedString128Bytes sender;
        public NativeArray<byte> payload;

        public void Dispose(JobHandle dep)
        {
            payload.Dispose(dep);
        }

        public static ReceivedMessage From(NativeArray<byte> bytes, FixedString128Bytes sender)
        {
            return new ReceivedMessage { payload = bytes, sender = sender };
        }
    }

    public struct OutgoingMessage 
    {
        public NativeArray<FixedString128Bytes> recipients;
        public NativeArray<byte> payload;

        public void Dispose()
        {
            payload.Dispose();
            recipients.Dispose();
        }

        public static OutgoingMessage From(NativeArray<byte> bytes, NativeArray<FixedString128Bytes> recipients)
        {
            return new OutgoingMessage { payload = bytes, recipients = recipients };
        }
    }

    public interface IConnection
    {
        public NativeQueue<ReceivedMessage> receiveQueue { get; }
        public void Send(byte[] bytes, string recipient);
    };
}

