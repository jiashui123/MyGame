using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageCanvas : Singleton<MessageCanvas>
{

    public enum MessageBoxType
    {
        OnBtn = 0,
        TwoBtn = 1
    }

    public bool StopStatus = false;

    public AudioClip sureClip;
    #region 样式一
    /// <summary>
    /// 确认按钮
    /// </summary>
    [SerializeField]
    private Button sureBtn;
    /// <summary>
    /// 取消按钮
    /// </summary>
    [SerializeField]
    private Button cancelBtn;
    /// <summary>
    /// 标题文字
    /// </summary>
    [SerializeField]
    private Text titleContent;

    /// <summary>
    /// 显示文字
    /// </summary>
    [SerializeField]
    private Text txtContent;
    /// <summary>
    /// 物体
    /// </summary>
    [SerializeField]
    public GameObject messageBox;

    /// <summary>
    /// 点击之后的调用
    /// </summary>
    private Action onCompleted;
    /// <summary>
    /// 点击之后的调用
    /// </summary>
    private Action onCanceled;
    /// <summary>
    /// 显示文字
    /// </summary>
    /// <param name="text"></param>
    public void ShowMessageBox(string text, Action sureAcition = null, Action cancelActionction = null, float showTime = -1, string title = "提示", MessageBoxType messageBoxType = MessageBoxType.OnBtn)
    {
        messageBox2.SetActive(false);
        if ((int)messageBoxType == 0)
        {
            sureBtn.gameObject.SetActive(true);
            cancelBtn.gameObject.SetActive(false);
        }
        else if ((int)messageBoxType == 1)
        {
            sureBtn.gameObject.SetActive(true);
            cancelBtn.gameObject.SetActive(true);
        }
        messageBox.SetActive(true);
        titleContent.text = title;
        txtContent.text = text;
        onCompleted = sureAcition;
        onCanceled = cancelActionction;
        StopGame();
        if (showTime != -1)
            Invoke("Hide", showTime);
    }

    /// <summary>
    /// 隐藏
    /// </summary>
    public void CancelBtn()
    {
        AudioController.Instance.PlayAudio(sureClip, delegate
        {
            messageBox.SetActive(false);
            if (onCanceled != null)
            {
                onCanceled();
                onCanceled = null;
            }
        });
    }

    /// <summary>
    /// 点击确定按钮
    /// </summary>
    public void SureBtn()
    {
        AudioController.Instance.PlayAudio(sureClip, delegate
        {
            messageBox.SetActive(false);
            if (onCompleted != null)
            {
                onCompleted();
                onCompleted = null;
            }
        });
    }

    #endregion

    #region 样式二
    /// <summary>
    /// 确认按钮
    /// </summary>
    [SerializeField]
    private Button sureBtn2;
    /// <summary>
    /// 取消按钮
    /// </summary>
    [SerializeField]
    private Button cancelBtn2;
    /// <summary>
    /// 标题文字
    /// </summary>
    [SerializeField]
    private Text titleContent2;

    /// <summary>
    /// 显示文字
    /// </summary>
    [SerializeField]
    private Text txtContent2;
    /// <summary>
    /// 物体
    /// </summary>
    [SerializeField]
    public GameObject messageBox2;

    /// <summary>
    /// 点击之后的调用
    /// </summary>
    private Action onCompleted2;
    /// <summary>
    /// 点击之后的调用
    /// </summary>
    private Action onCanceled2;
    /// <summary>
    /// 显示文字
    /// </summary>
    /// <param name="text"></param>
    public void ShowMessageBox_Type2(string text, Action sureAcition = null, Action cancelActionction = null, float showTime = -1, string title = "提示", MessageBoxType messageBoxType = MessageBoxType.OnBtn)
    {
        messageBox.SetActive(false);
        if ((int)messageBoxType == 0)
        {
            sureBtn2.gameObject.SetActive(true);
            cancelBtn2.gameObject.SetActive(false);
        }
        else if ((int)messageBoxType == 1)
        {
            sureBtn2.gameObject.SetActive(true);
            cancelBtn2.gameObject.SetActive(true);
        }
        messageBox2.SetActive(true);
        titleContent2.text = title;
        txtContent2.text = text;
        onCompleted2 = sureAcition;
        onCanceled2 = cancelActionction;
        StopGame();
        if (showTime != -1)
            Invoke("Hide2", showTime);
    }

    /// <summary>
    /// 隐藏
    /// </summary>
    public void CancelBtn2()
    {
        AudioController.Instance.PlayAudio(sureClip, delegate
        {
            if (onCanceled2 != null)
            {
                onCanceled2();
                onCanceled2 = null;
            }
            messageBox2.SetActive(false);
        });
    }

    /// <summary>
    /// 点击确定按钮
    /// </summary>
    public void SureBtn2()
    {
        AudioController.Instance.PlayAudio(sureClip, delegate
        {
            if (onCompleted2 != null)
            {
                onCompleted2();
                onCompleted2 = null;
            }
            messageBox2.SetActive(false);
        });
    }
    #endregion
    private void OnEnable()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!StopStatus)
        {
            Time.timeScale = (1);
        }
        else
        {
            Time.timeScale = (0);
        }
    }
    public void StopGame()
    {
        StopStatus = true;
    }

    public void PlayGame()
    {
        StopStatus = false;
    }
}
