using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.UI;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine.Audio;
using MidiPlayerTK;

[System.Serializable]
public class Note
{
    public int clipIndex;
    public float waitTime;
}
[System.Serializable]
public class MidiNoteModel
{
    public int NoteNumber;
    public string NoteName;
    public int Velocity;
    public int NoteLength;
    public MidiNote.EnumLength enumLength;
}
public class GameController : Singleton<GameController>
{
    public static EntityArchetype BlockArchetype;

    public EntityManager manager;
    public Entity entities;

    public AudioSource audio;
    public AudioMixerGroup audioMixerGroup;
    public Mesh blockMesh;
    public Material blockMaterial;
    public Material blockSelectMaterial;
    public int blockNumerX = 16;
    public int blockNumerY = 16;

    public List<AudioClip> clips;
    public List<Material> materials = new List<Material>();
    public int[,] colorData;

    public MidiFilePlayer midiFilePlayer;
    public MidiStreamPlayer midiStreamPlayer;

    public List<Note> notes;
    public List<MidiNoteModel> midiNoteModels;
    public double playerTimeFromStart = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        EntityManager manager = World.Active.GetOrCreateManager<EntityManager>();
        
        BlockArchetype = manager.CreateArchetype( typeof(Position),typeof(Scale));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private void Start()
    {

        //Debug.Log("---------------- " + timeFromStartPlay );
        // Read midi events until this time

        manager = World.Active.GetOrCreateManager<EntityManager>();

        for (int j = 0; j < blockNumerY; j++)
        {
            for (int i = 0; i < blockNumerX; i++)
            {

                Entity entities = manager.CreateEntity(BlockArchetype);
                manager.SetComponentData(entities, new Position { Value = new float3(i - blockNumerX / 2, j - blockNumerY / 2 + 0.5f, 0) });
                manager.SetComponentData(entities, new Scale { Value = new float3(0.9f, 0.9f, 0.9f) });

                //AudioSource audio = gameObject.AddComponent<AudioSource>();
                //audio.outputAudioMixerGroup = audioMixerGroup;
                //audio.clip = clips[i + j * 11];
                manager.AddSharedComponentData(entities, new Block
                {
                    isTouched = false,
                    audio = audio,
                    noteName = clips[i + j * 11].name.Split('-')[1].Trim()
                });
                manager.AddSharedComponentData(entities, new MeshInstanceRenderer
                {
                    mesh = blockMesh,
                    material = materials[0]
                });
                manager.AddComponentData(entities, new BlockTag { });
            }
        }
        NotesToPlay(midiFilePlayer.LoadMusicNote());
    }

    /// <summary>
    /// 根据节拍加载音频
    /// </summary>
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

    public void NotesToPlay(List<MidiNote> notes)
    {
        
        for (int i=0;i< notes.Count;i++)
        {
            if (notes[i].Midi > 40 && notes[i].Midi < 100)// && note.Channel==1)
            {
                Entity entities = manager.CreateEntity(BlockArchetype);

                NoteModel noteModel = new NoteModel();
                noteModel.midiFilePlayer = this.midiFilePlayer;
                noteModel.noteName = notes[i].noteName;
                noteModel.note = notes[i];
                noteModel.noteId = i;
                noteModel.hasBlock = false;
                noteModel.isUsed = false;
                //Debug.Log(notes[i].Duration+"----"+ notes[i].AbsoluteQuantize);
                manager.AddSharedComponentData(entities, noteModel);
                //MPTKNote mptkNote = new MPTKNote() { Delay = 0, Drum = false, Duration = 0.2f, Note = 60, Patch = 10, Velocity = 100 };
                //mptkNote.Play(midiStreamPlayer);
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
