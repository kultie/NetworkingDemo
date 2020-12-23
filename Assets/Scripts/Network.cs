using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Network
{
    private static SmartFox _sfs;
    public static SmartFox sfs
    {
        get
        {
            if (_sfs == null)
            {
                _sfs = new SmartFox();
            }
            return _sfs;
        }
    }

    public static void ConnectToServer()
    {
        ConfigData cfg = new ConfigData();
        cfg.Host = "127.0.0.1";
        cfg.Zone = "Networking";
        cfg.Port = 9933;
        sfs.Connect(cfg);
    }

    static List<ServerToClientMessage> stcMessages = new List<ServerToClientMessage>();
    static List<ClientToServerMessage> ctsMessages = new List<ClientToServerMessage>();


    public static void SendSeverMessage(SFSArray data)
    {
        var ts = Extensions.GetCurrentTime();
        SFSObject sendingData = new SFSObject();
        sendingData.PutLong("ts", ts);
        sendingData.PutSFSArray("payload", data);
        sfs.Send(new ObjectMessageRequest(sendingData));
    }

    public static void SendClientMessage(long lag, SFSObject data)
    {
        if (Client.Instance.headlessUser == null) return;
        var ts = Extensions.GetCurrentTime() + lag;
        SFSObject sendingData = new SFSObject();
        sendingData.PutLong("ts", ts);
        sendingData.PutSFSObject("payload", data);
        sfs.Send(new ObjectMessageRequest(sendingData, Client.Instance.currentRoom, new User[] { Client.Instance.headlessUser }));
    }

    public static void OnReceiveServerMessage(SFSObject data)
    {
        ServerToClientMessage stc = new ServerToClientMessage();
        stc.ts = data.GetLong("ts");
        stc.payload = (SFSArray)data.GetSFSArray("payload");
        stcMessages.Add(stc);
    }

    public static void OnReceiveClientMessage(SFSObject data)
    {
        ClientToServerMessage cts = new ClientToServerMessage();
        cts.ts = data.GetLong("ts");
        cts.payload = (SFSObject)data.GetSFSObject("payload");
        ctsMessages.Add(cts);
    }

    public static ServerToClientMessage GetServerMessage()
    {
        var now = Extensions.GetCurrentTime();
        for (int i = 0; i < stcMessages.Count; i++)
        {
            var ms = stcMessages[i];
            if (ms.ts <= now)
            {
                stcMessages.Remove(ms);
                return ms;
            }
        }
        return null;
    }

    public static ClientToServerMessage GetClientMessage()
    {
        var now = Extensions.GetCurrentTime();
        for (int i = 0; i < ctsMessages.Count; i++)
        {
            var ms = ctsMessages[i];
            if (ms.ts <= now)
            {
                ctsMessages.Remove(ms);
                return ms;
            }
        }
        return null;
    }
}

public class ServerToClientMessage
{
    public long ts;
    public SFSArray payload;
}

public class ClientToServerMessage
{
    public long ts;
    public SFSObject payload;
}
