using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MidiPlayerTK;

[System.Serializable]
public struct Block : ISharedComponentData
{
    public AudioSource audio;
    public bool isTouched;
    public string noteName;
    public int noteId;
}

public class BlockComponent : SharedComponentDataWrapper<Block>
{

}
