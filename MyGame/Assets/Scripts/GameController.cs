using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.UI;
using Unity.Mathematics;
using Unity.Rendering;

public class GameController : Singleton<GameController>
{
    public static EntityArchetype BlockArchetype;

    public EntityManager manager;
    public Entity entities;

    public Mesh blockMesh;
    public Material blockMaterial;
    public Material blockSelectMaterial;
    public int blockNumer = 16;

    public Texture2D texture;
    //public Color[,] colorDatas;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        EntityManager manager = World.Active.GetOrCreateManager<EntityManager>();

        BlockArchetype = manager.CreateArchetype( typeof(Position),typeof(Scale));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private void Start()
    {
        manager = World.Active.GetOrCreateManager<EntityManager>();
        //GetColorData();
        for (int i = -blockNumer; i < blockNumer; i++)
        {
            for (int j = -blockNumer; j < blockNumer; j++)
            {
                Entity entities = manager.CreateEntity(BlockArchetype);
                manager.SetComponentData(entities, new Position { Value = new float3(i * 4.8f / blockNumer, j * 4.8f / blockNumer, 0) });
                manager.SetComponentData(entities, new Scale { Value = new float3(4.0f / blockNumer, 4.0f / blockNumer, 4.0f / blockNumer) });
                //Material mat = new Material(blockMaterial.shader);
                //mat.color = colorDatas[i + blockNumer, j + blockNumer];
                manager.AddSharedComponentData(entities, new MeshInstanceRenderer
                {
                    mesh = blockMesh,
                    material = blockMaterial
                });
                manager.AddComponentData(entities, new BlockTag { });
            }
        }
    }

    //public void GetColorData()
    //{
    //    colorDatas = new Color[blockNumer*2, blockNumer*2];
    //    for (int i = 0; i < blockNumer*2; i++)
    //    {
    //        for (int j = 0; j < blockNumer*2; j++)
    //        {
    //            colorDatas[i, j] = texture.GetPixelBilinear((float)i/ ((float)(blockNumer*2)), (float)j / ((float)(blockNumer*2)));
    //        }
    //    }
       
    //}
}
