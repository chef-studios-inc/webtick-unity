using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

public struct UDPNetworkInterface : INetworkInterface
{
    public NetworkEndpoint LocalEndpoint => throw new System.NotImplementedException();

    public int Bind(NetworkEndpoint endpoint)
    {
        throw new System.NotImplementedException();
    }

    public void Dispose()
    {
        throw new System.NotImplementedException();
    }

    public int Initialize(ref NetworkSettings settings, ref int packetPadding)
    {
        throw new System.NotImplementedException();
    }

    public int Listen()
    {
        throw new System.NotImplementedException();
    }

    public JobHandle ScheduleReceive(ref ReceiveJobArguments arguments, JobHandle dep)
    {
        throw new System.NotImplementedException();
    }

    public JobHandle ScheduleSend(ref SendJobArguments arguments, JobHandle dep)
    {
        throw new System.NotImplementedException();
    }
}
