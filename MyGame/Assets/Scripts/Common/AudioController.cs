using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : Singleton<AudioController> {

    public AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public void PlayAudio(AudioClip audioClip, Action action = null)
    {
        StartCoroutine(_PlayAudio(audioClip, action));
    }

    IEnumerator _PlayAudio(AudioClip audioClip, Action action = null)
    {
        if (audioClip == null)
        {
            action.Invoke();
            yield break;
        }
        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.clip = audioClip;
        audioSource.Play();
        yield return new WaitForSeconds(audioClip.length);
        if (action != null)
            action.Invoke();
    } 

    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MessageCanvas.Instance.ShowMessageBox_Type2("退出游戏？", delegate { Application.Quit(); },null,-1,null,MessageCanvas.MessageBoxType.TwoBtn);
        }
    }
}
