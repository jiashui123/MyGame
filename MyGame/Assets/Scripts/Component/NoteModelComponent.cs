using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MidiPlayerTK;

[System.Serializable]
public struct NoteModel : ISharedComponentData
{
    public MidiFilePlayer midiFilePlayer;
    public bool isUsed;
    public bool hasBlock;
    public MidiNote note;
    public string noteName;
    public int noteId;
}

public class NoteModelComponent : SharedComponentDataWrapper<NoteModel>
{

}
