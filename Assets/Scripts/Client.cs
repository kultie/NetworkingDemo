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

public class Client : MonoBehaviour
{
    public static Client Instance;
    [SerializeField]
    int lag;
    public Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
    public int entityID;
    [SerializeField]
    bool prediction;
    [SerializeField]
    bool reconciliation;
    [SerializeField]
    bool interpolation;

    [SerializeField]
    Entity entityPrefab;

    SmartFox sfs;

    public User headlessUser;
    public Room currentRoom;

    List<NetInput> pendingInputs = new List<NetInput>();
    int inputSeqNumber = 0;

    private void Awake()
    {
        Instance = this;
        ConnectToServer();
    }

    public void ConnectToServer()
    {
        sfs = Network.sfs;
        sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnJoinRoom);
        sfs.AddEventListener(SFSEvent.CONNECTION, OnConnect);
        Network.ConnectToServer();
    }

    private void OnConnect(BaseEvent evt)
    {
        sfs.InitUDP();
    }

    private void OnReceiveServerMessage(BaseEvent evt)
    {
        SFSObject message = (SFSObject)evt.Params["message"];
        Network.OnReceiveServerMessage(message);
    }

    private void OnJoinRoom(BaseEvent evt)
    {
        Room r = (Room)evt.Params["room"];
        headlessUser = r.GetUserByName("Headless");
        currentRoom = r;
        sfs.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnReceiveServerMessage);
    }

    public void JoinRoom()
    {
        sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
        sfs.Send(new LoginRequest(Guid.NewGuid().ToString()));
    }

    private void OnLogin(BaseEvent evt)
    {
        sfs.Send(new JoinRoomRequest("Headless"));
        entityID = ((User)evt.Params["user"]).Id;
    }

    private void Update()
    {
        sfs.ProcessEvents();
        ProcessMessage();
        ProcessInput();
        if (interpolation)
        {
            InterpolationEntities();
        }

        foreach (var i in entities.Values)
        {
            i.RenderClient();
        }
    }

    private void ProcessMessage()
    {
        while (true)
        {
            var message = Network.GetServerMessage();
            if (message == null)
            {
                break;
            }

            for (int i = 0; i < message.payload.Size(); i++)
            {
                SFSObject state = (SFSObject)message.payload.GetSFSObject(i);
                if (!entities.ContainsKey(state.GetInt("entity_id")))
                {
                    entities[state.GetInt("entity_id")] = CreateNewEntity(state);
                }
                var entity = entities[state.GetInt("entity_id")];
                var pos = state.GetFloatArray("position");
                var velocity = state.GetFloatArray("velocity");
                Vector2 servPos = new Vector2(pos[0], pos[1]);
                Vector2 servVel = new Vector2(velocity[0], velocity[1]);
                if (state.GetInt("entity_id") == entityID)
                {
                    entity.ApplyPosition(servPos);
                    entity.ApplyVelocity(servVel);
                    if (reconciliation)
                    {
                        var j = 0;
                        while (j < pendingInputs.Count)
                        {
                            var input = pendingInputs[j];
                            if (input.seqNumber <= state.GetInt("last_input"))
                            {
                                pendingInputs.Remove(input);
                            }
                            else
                            {
                                entity.ApplyInputClient(input);
                                j++;
                            }
                        }
                    }
                    else
                    {
                        pendingInputs.Clear();
                    }
                }
                else
                {
                    if (interpolation)
                    {
                        var ts = Extensions.GetCurrentTime();
                        entity.AddPositionBuffer(ts, servPos);
                    }
                    else
                    {
                        entity.ApplyPosition(servPos);
                    }
                }
            }
        }
    }

    private Entity CreateNewEntity(SFSObject entityData)
    {
        Entity e = Instantiate(entityPrefab);
        e.Setup(entityData);
        return e;
    }

    private void ProcessInput()
    {
        float dt = Time.deltaTime;
        float dir = 0;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            dir = dt * -1;

        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            dir = dt;
        }

        if (dir != 0)
        {
            SFSObject input = new SFSObject();
            int seq = inputSeqNumber++;
            input.PutFloat("direction", dir);
            input.PutInt("seq_number", seq);
            input.PutInt("entity_id", entityID);
            NetInput i = new NetInput(entityID, seq, dir);
            Network.SendClientMessage(lag, input);
            pendingInputs.Add(i);
            if (prediction)
            {
                entities[entityID].ApplyInputClient(i);
            }
        }
    }



    private void InterpolationEntities()
    {
        var now = Extensions.GetCurrentTime();
        var renderTimeStamp = now - (1000 / 30);
        foreach (KeyValuePair<int, Entity> e in entities)
        {
            Entity entity = e.Value;
            if (e.Key == entityID)
            {
                continue;
            }

            var buffer = entity.positionBuffers;
            while (buffer.Count >= 2 && buffer[1].timeStamp <= renderTimeStamp)
            {
                buffer.Remove(buffer[0]);
            }

            if (buffer.Count >= 2 && buffer[0].timeStamp <= renderTimeStamp && renderTimeStamp <= buffer[1].timeStamp)
            {
                var t0 = buffer[0].timeStamp;
                var t1 = buffer[1].timeStamp;
                entity.ApplyPosition(Vector2.Lerp(buffer[0].position, buffer[1].position, (renderTimeStamp - t0) * 1f / (t1 - t0)));
            }
        }
    }
}
