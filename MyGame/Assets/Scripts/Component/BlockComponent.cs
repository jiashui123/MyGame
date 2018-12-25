using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[System.Serializable]
public struct Block : ISharedComponentData
{
    public AudioSource audio;
    public bool isTouched;
}

public class BlockComponent : SharedComponentDataWrapper<Block>
{

}
