using MidiPlayerTK;
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
        [ReadOnly] public SharedComponentDataArray<Block> blocks;
        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public ComponentDataArray<BlockTag> tags;
        /// <summary>
        /// 除掉目标标签或者属性
        /// </summary>
        //[ReadOnly] public ComponentDataArray<BlockTag> tag;
    }
    [Inject] MusicBlockGroup musicBlocks;

    /// <summary>
    /// 过滤出需要的实体
    /// </summary>
    struct MusicNoteGroup
    {
        /// <summary>
        /// 必须要定义的筛选条件
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// 定义自定义筛选条件
        /// </summary>
        public EntityArray Entities;
        [ReadOnly] public SharedComponentDataArray<NoteModel> notes;
        /// <summary>
        /// 除掉目标标签或者属性
        /// </summary>
        //[ReadOnly] public ComponentDataArray<BlockTag> tag;
    }
    [Inject] MusicNoteGroup musicNotes;

    private EntityManager manager;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        manager = World.Active.GetOrCreateManager<EntityManager>();
    }
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }
    /// <summary>
    /// 在Update中进行逻辑操作
    /// </summary>
    protected override void OnUpdate()
    {
        //ShowNote();
        PlayTest();
        //MouseMode();
    }

    public void ShowNote()
    {
        for (int i = 0; i < musicBlocks.Length; i++)
        {
            bool isUsed = false;
            for (int j = 0; j < musicNotes.Length; j++)
            {
                if (musicNotes.notes[j].noteName == musicBlocks.blocks[i].noteName)
                {
                    isUsed = true;
                    PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[i],
                        new MeshInstanceRenderer
                        {
                            mesh = GameController.Instance.blockMesh,
                            material = GameController.Instance.materials[1]
                        });
                    PostUpdateCommands.DestroyEntity(musicNotes.Entities[j]);
                }
            }
            if (!isUsed)
            {
                PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[i],
                    new MeshInstanceRenderer
                    {
                        mesh = GameController.Instance.blockMesh,
                        material = GameController.Instance.materials[0]
                    });
            }
        }
    }

    public void PlayTest()
    {
        bool hasNote = false;
        for (int i = 0; i < musicNotes.Length; i++)
        {
            if (!musicNotes.notes[i].isUsed && (musicNotes.notes[i].note.AbsoluteQuantize) <= (GameController.Instance.playerTimeFromStart))
            {
                hasNote = true;
                if (musicNotes.notes[i].hasBlock)
                    continue;
                int id = -1;
                do
                {
                    id = UnityEngine.Random.Range(0, musicBlocks.blocks.Length);
                }
                while (musicBlocks.blocks[id].isTouched);

                if (!musicBlocks.blocks[id].isTouched)
                {
                    PostUpdateCommands.SetSharedComponent(musicNotes.Entities[i],
                           new NoteModel
                           {
                               midiFilePlayer = musicNotes.notes[i].midiFilePlayer,
                               noteName = musicNotes.notes[i].noteName,
                               note = musicNotes.notes[i].note,
                               noteId = musicNotes.notes[i].noteId,
                               hasBlock = true,
                               isUsed = false
                           });
                    PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[id],
                        new MeshInstanceRenderer
                        {
                            mesh = GameController.Instance.blockMesh,
                            material = GameController.Instance.materials[1]
                        });
                    PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[id], new Block
                    {
                        audio = musicBlocks.blocks[id].audio,
                        noteName = musicBlocks.blocks[id].noteName,
                        noteId = musicNotes.notes[i].noteId,
                        isTouched = true
                    });
                }
            }
        }

        if (!hasNote)
        {
            GameController.Instance.playerTimeFromStart += 240;
        }

        MouseMode();
    }

    public void MusicMode()
    {
        GameController.Instance.Visulization();
        for (int i = 0; i < musicBlocks.Length; i++)
        {
            int blockNumerX = GameController.Instance.blockNumerX;
            int blockNumerY = GameController.Instance.blockNumerY;
            int x = (int)(musicBlocks.positions[i].Value.x * blockNumerX / 4.8f);
            int y = (int)(musicBlocks.positions[i].Value.y * blockNumerX / 4.8f);
            int z = 0;

            MeshInstanceRenderer meshInstance = new MeshInstanceRenderer();
            meshInstance.mesh = GameController.Instance.blockMesh;
            meshInstance.material = GameController.Instance.materials[1];

            PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[i], meshInstance);
        }
    }

    public void MouseMode()
    {
        for (int i = 0; i < musicBlocks.Length; i++)
        {
            if (musicBlocks.blocks[i].isTouched)
            {
                for (int k = 0; k < Input.touches.Length; k++)
                {
                    Vector3 blockPosition = new Vector3(musicBlocks.positions[i].Value.x, musicBlocks.positions[i].Value.y, musicBlocks.positions[i].Value.z);
                    Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.touches[k].position);
                    mousePosition.z = 0;
                    if ((blockPosition.x + 0.45f > mousePosition.x && blockPosition.x - 0.45f < mousePosition.x) && (blockPosition.y + 0.45f > mousePosition.y && blockPosition.y - 0.45f < mousePosition.y))
                    {

                        for (int j = 0; j < musicNotes.Length; j++)
                        {
                            if (musicBlocks.blocks[i].noteId == musicNotes.notes[j].noteId)
                            {
                                musicNotes.notes[j].midiFilePlayer.MPTK_PlayNote(musicNotes.notes[j].note);
                                //GameController.Instance.playerTimeFromStart += 240;

                                PostUpdateCommands.SetSharedComponent(musicNotes.Entities[j],
                                    new NoteModel
                                    {
                                        midiFilePlayer = musicNotes.notes[j].midiFilePlayer,
                                        noteName = musicNotes.notes[j].note.noteName,
                                        note = musicNotes.notes[j].note,
                                        noteId = musicNotes.notes[j].noteId,
                                        hasBlock = false,
                                        isUsed = true
                                    });
                            }
                        }

                        PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[i], new Block
                        {
                            audio = musicBlocks.blocks[i].audio,
                            noteName = musicBlocks.blocks[i].noteName,
                            noteId = -1,
                            isTouched = false
                        });

                        PostUpdateCommands.SetSharedComponent(musicBlocks.Entities[i],
                            new MeshInstanceRenderer
                            {
                                mesh = GameController.Instance.blockMesh,
                                material = GameController.Instance.materials[0]
                            });

                    }
                }
            }
        }
    }
}
