using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[System.Serializable]
public struct Block : ISharedComponentData
{
    public AudioSource audio;

    public AudioClip clip;
}

public class BlockComponent : SharedComponentDataWrapper<Block>
{

}
