using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour
{
    List<User> users = new List<User>();
    public static Server Instance;
    public Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
    public Dictionary<int, int> lastProcessInput = new Dictionary<int, int>();

    [SerializeField]
    Entity entityPrefab;

    SmartFox sfs;

    private void Awake()
    {
        Application.targetFrameRate = 32;
        Instance = this;
        ConnectToServer();
    }

    public void ConnectToServer()
    {
        sfs = Network.sfs;
        sfs.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnReceiveClientMessage);
        sfs.AddEventListener(SFSEvent.CONNECTION, OnConnect);
        sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
        sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
        Network.ConnectToServer();
    }

    private void OnUserEnterRoom(BaseEvent evt)
    {
        User u = (User)evt.Params["user"];
        Debug.Log(u.Id + ": " + "Has joined");
        users.Add(u);
        CreateNewEntity(u);
    }

    void CreateNewEntity(User user)
    {
        Entity entity = Instantiate(entityPrefab);
        entities[user.Id] = entity;
    }

    private void OnConnect(BaseEvent evt)
    {
        sfs.InitUDP();
        Login();
    }

    private void Login()
    {
        sfs.Send(new LoginRequest("Headless"));
    }

    private void OnLogin(BaseEvent evt)
    {
        CreateRoom();
    }

    private void CreateRoom()
    {
        RoomSettings rs = new RoomSettings("Headless");
        sfs.Send(new CreateRoomRequest(rs, true));
    }

    private void OnReceiveClientMessage(BaseEvent evt)
    {
        SFSObject message = (SFSObject)evt.Params["message"];
        Network.OnReceiveClientMessage(message);
    }

    public void JoinRoom()
    {
        sfs.Send(new JoinRoomRequest("Headless"));
    }

    private void Update()
    {
        sfs.ProcessEvents();
        ProcessInput();

        SendGameState();

        foreach (var i in entities.Values)
        {
            i.Render();
        }
    }

    private void SendGameState()
    {
        SFSArray dataArr = new SFSArray();
        foreach (KeyValuePair<int, Entity> e in entities)
        {
            SFSObject _d = new SFSObject();
            _d.PutInt("entity_id", e.Key);
            _d.PutFloatArray("position", new float[] { e.Value.transform.position.x, e.Value.transform.position.y });
            if (lastProcessInput.ContainsKey(e.Key))
            {
                _d.PutInt("last_input", lastProcessInput[e.Key]);
            }
            dataArr.AddSFSObject(_d);
        }
        Network.SendSeverMessage(dataArr);
    }

    private void ProcessInput()
    {

        while (true)
        {
            var message = Network.GetClientMessage();
            if (message == null)
            {
                break;
            }

            var id = message.payload.GetInt("entity_id");
            var seq = message.payload.GetInt("seq_number");
            var direction = message.payload.GetFloat("direction");
            NetInput input = new NetInput(id, seq, direction);
            entities[id].ApplyInput(input);
            lastProcessInput[id] = seq;
        }
    }
}
