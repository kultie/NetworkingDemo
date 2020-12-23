using Sfs2X.Entities.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField]
    float speed = 2;
    public List<PositionBufferElement> positionBuffers = new List<PositionBufferElement>();
    Vector2 position;
    public void ApplyInput(NetInput input)
    {
        position.x += input.direction * speed;
    }

    internal void AddPositionBuffer(long ts, Vector2 servPos)
    {
        positionBuffers.Add(new PositionBufferElement()
        {
            timeStamp = ts,
            position = servPos
        });
    }

    internal void ApplyPosition(Vector2 servPos)
    {
        position = servPos;
    }
    public void Render()
    {
        transform.position = position;
    }

    internal void Setup(SFSObject entityData)
    {
        float[] pos = entityData.GetFloatArray("position");
        ApplyPosition(new Vector2(pos[0], pos[1]));
        Render();
    }
}

public class PositionBufferElement
{
    public long timeStamp;
    public Vector2 position;
}

public class NetInput
{
    public int entityID;
    public int seqNumber;
    public float direction;
    public NetInput(int entityID, int seqNumber, float direction)
    {
        this.entityID = entityID;
        this.seqNumber = seqNumber;
        this.direction = direction;
    }
}
