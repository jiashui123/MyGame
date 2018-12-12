using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct BlockTag : IComponentData {  }

public class BlockTagComponent : ComponentDataWrapper<BlockTag> {

    
}
