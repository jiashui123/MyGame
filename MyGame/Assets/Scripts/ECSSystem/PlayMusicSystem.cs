using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class PlayMusicSystem : ComponentSystem
{
    /// <summary>
    /// 过滤出需要的实体
    /// </summary>
    struct MusicBlockGroup
    {
        /// <summary>
        /// 必须要定义的筛选条件
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// 定义自定义筛选条件
        /// </summary>
        public EntityArray Entities;
        [ReadOnly] public SharedComponentDataArray<MeshInstanceRenderer> meshs;
        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public ComponentDataArray<BlockTag> tags;
        /// <summary>
        /// 除掉目标标签或者属性
        /// </summary>
        //[ReadOnly] public ComponentDataArray<BlockTag> tag;
    }
    [Inject] MusicBlockGroup musicBlocks;

    private EntityManager manager;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        manager = World.Active.GetOrCreateManager<EntityManager>();
    }
    /// <summary>
    /// 在Update中进行逻辑操作
    /// </summary>
    protected override void OnUpdate()
    {
        for (int i = 0; i < musicBlocks.Length; i++)
        {
            Vector3 blockPosition = new Vector3(musicBlocks.positions[i].Value.x, musicBlocks.positions[i].Value.y, musicBlocks.positions[i].Value.z);
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;
            if (Vector3.Distance(blockPosition, mousePosition)<=0.25f)
            {

                PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[i],
                    new MeshInstanceRenderer
                    {
                        mesh = GameController.Instance.blockMesh,
                        material = GameController.Instance.blockSelectMaterial
                    });
            }
            else
            {
                PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[i],
            new MeshInstanceRenderer
            {
                mesh = GameController.Instance.blockMesh,
                material = GameController.Instance.blockMaterial
            });
            }
        }
    }
}
