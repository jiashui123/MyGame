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

    public AudioSource audio;
    public Mesh blockMesh;
    public Material blockMaterial;
    public Material blockSelectMaterial;
    public int blockNumerX = 16;
    public int blockNumerY = 16;

    public List<AudioClip> clips;
    public List<Material> materials = new List<Material>();
    public int[,] colorData;
    [Range(0, 10)]
    public float colorMultiplyer = 1;
    [Range(0, 1)]
    public float  s = 1;
    [Range(0, 1)]
    public float  v = 1;
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
        
        for (int i = -blockNumerX; i < blockNumerX; i++)
        {
            for (int j = -blockNumerY; j < blockNumerY; j++)
            {
                Entity entities = manager.CreateEntity(BlockArchetype);
                manager.SetComponentData(entities, new Position { Value = new float3(i * 4.8f / blockNumerX, j * 4.8f / blockNumerX, 0) });
                manager.SetComponentData(entities, new Scale { Value = new float3(4.0f / blockNumerX, 4.0f / blockNumerX, 4.0f / blockNumerX) });

                AudioSource audio = gameObject.AddComponent<AudioSource>();
                manager.AddSharedComponentData(entities, new Block
                {
                    audio = audio,
                    clip = clips[i+ blockNumerX]
                });
                manager.AddSharedComponentData(entities, new MeshInstanceRenderer
                {
                    mesh = blockMesh,
                    material = blockMaterial
                });
                manager.AddComponentData(entities, new BlockTag { });
            }
        }
    }


    public void Visulization()
    {
        colorData = new int[blockNumerX * 2, blockNumerY * 2];
        float[] musicData=new float[256];
        audio.GetSpectrumData(musicData, 0, FFTWindow.Rectangular);
        for (int i = 0; i < blockNumerX*2; i++)
        {
            for (int j = 0; j < blockNumerY*2; j++)
            {
                if(((float)(j)/(blockNumerY * 2))> ((musicData[i]*10)))
                {
                    colorData[i, j] = 0;
                    continue;
                }
        
                if (musicData[i] >= 0.0f && musicData[i] < 0.01f)
                {
                    colorData[i, j] = 0;
                }
                else if (musicData[i] >= 0.01f && musicData[i] < 0.02f)
                {
                    colorData[i, j] = 1;
                }
                else if (musicData[i] >= 0.02f && musicData[i] < 0.03f)
                {
                    colorData[i, j] = 2;
                }
                else if (musicData[i] >= 0.03f && musicData[i] < 0.04f)
                {
                    colorData[i, j] = 3;
                }
                else if (musicData[i] >= 0.04f && musicData[i] < 0.05f)
                {
                    colorData[i, j] = 4;
                }
                else if (musicData[i] >= 0.05f && musicData[i] < 0.06f)
                {
                    colorData[i, j] = 5;
                }
                else if (musicData[i] >= 0.06f && musicData[i] < 0.07f)
                {
                    colorData[i, j] = 6;
                }
                else if (musicData[i] >= 0.07f && musicData[i] < 0.08f)
                {
                    colorData[i, j] = 7;
                }
                else if (musicData[i] >= 0.08f && musicData[i] < 0.09f)
                {
                    colorData[i, j] = 8;
                }
                else if (musicData[i] >= 0.09f && musicData[i] < 0.10f)
                {
                    colorData[i, j] = 9;
                }
                else if (musicData[i] >= 0.10f)
                {
                    colorData[i, j] = 10;
                }
            }
        }
    }

    #region Static
    public static Color HSVtoRGB(float hue, float saturation, float value, float alpha)
    {
        while (hue > 1f)
        {
            hue -= 1f;
        }
        while (hue < 0f)
        {
            hue += 1f;
        }
        while (saturation > 1f)
        {
            saturation -= 1f;
        }
        while (saturation < 0f)
        {
            saturation += 1f;
        }
        while (value > 1f)
        {
            value -= 1f;
        }
        while (value < 0f)
        {
            value += 1f;
        }
        if (hue > 0.999f)
        {
            hue = 0.999f;
        }
        if (hue < 0.001f)
        {
            hue = 0.001f;
        }
        if (saturation > 0.999f)
        {
            saturation = 0.999f;
        }
        if (saturation < 0.001f)
        {
            return new Color(value * 255f, value * 255f, value * 255f);

        }
        if (value > 0.999f)
        {
            value = 0.999f;
        }
        if (value < 0.001f)
        {
            value = 0.001f;
        }

        float h6 = hue * 6f;
        if (h6 == 6f)
        {
            h6 = 0f;
        }
        int ihue = (int)(h6);
        float p = value * (1f - saturation);
        float q = value * (1f - (saturation * (h6 - (float)ihue)));
        float t = value * (1f - (saturation * (1f - (h6 - (float)ihue))));
        switch (ihue)
        {
            case 0:
                return new Color(value, t, p, alpha);
            case 1:
                return new Color(q, value, p, alpha);
            case 2:
                return new Color(p, value, t, alpha);
            case 3:
                return new Color(p, q, value, alpha);
            case 4:
                return new Color(t, p, value, alpha);
            default:
                return new Color(value, p, q, alpha);
        }
    }
    #endregion
}
