using System;
using Cmune.Realtime.Common;
using UnityEngine;
using Cmune.Realtime.Photon.Client.Network;

public class ServerLoadRequest : ServerRequest
{
    private Action _callback;

    public RequestStateType RequestState { get; private set; }
    public GameServerView Server { get; private set; }

    public enum RequestStateType
    {
        None,
        Waiting,
        Running,
        Down,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="server"></param>
    private ServerLoadRequest(MonoBehaviour mono, GameServerView server, Action callback)
        : base(mono)
    {
        _callback = callback;
        Server = server;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="server"></param>
    public static ServerLoadRequest Run(MonoBehaviour mono, GameServerView server, Action callback)
    {
        ServerLoadRequest request = new ServerLoadRequest(mono, server, callback);
        request.RunAgain();
        return request;
    }

    public void RunAgain()
    {
        if (Execute(Server.ConnectionString, null, GameApplicationRPC.QueryServerLoad))
        {
            RequestState = RequestStateType.Waiting;
        }
    }

    protected override void OnRequestCallback(int result, object[] table)
    {
        RequestState = RequestStateType.Down;

        base.OnRequestCallback(result, table);

        if (result == 0)
        {
            if (table.Length > 0 && table[0] is ServerLoadData)
            {
                Server.Data = (ServerLoadData)table[0];

                Server.Data.Latency = _client.Latency;
                Server.Data.State = ServerLoadData.Status.Alive;

                RequestState = RequestStateType.Running;
            }
            else
            {
                Server.Data.State = ServerLoadData.Status.NotReachable;
            }
        }
        else
        {
            Server.Data.State = ServerLoadData.Status.NotReachable;
        }

        _callback();
    }
}