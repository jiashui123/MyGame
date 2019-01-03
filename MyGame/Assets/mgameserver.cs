using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using System;

public class mgameserver : MonoBehaviour,IPhotonPeerListener {

    private static mgameserver instance = null;
    private PhotonPeer peer;
    private ConnectionProtocol protocol = ConnectionProtocol.Tcp;
    private string serverAddress = "127.0.0.1:4530";
    private string applicationName = "this is unity3d's client";
    private bool connected = false;//用来标识是否已经连接。
    int nextSendTickCount = Environment.TickCount;
    bool isFinished = false;

    void Awake()
    {
 
        instance = this;
        peer = new PhotonPeer(this,protocol);
        DontDestroyOnLoad(gameObject);
        isFinished = true;
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        
    }

    public void OnEvent(EventData eventData)
    {
        
    }

    /// <summary>
    /// 向服务器发请求
    /// </summary>
    /// <param name="code">操作码</param>
    /// <param name="SubCode">子操作码</param>
    /// <param name="parameters">参数</param>
    public void Request()
    {
        OperationRequest operationRequest = new OperationRequest();
        operationRequest.OperationCode = 227;
        Dictionary<byte, object> dict = new Dictionary<byte, object>();
        dict.Add(0, "123");
        operationRequest.Parameters = dict;
        
        peer.OpCustom(operationRequest, true,0,false);
    }

    public void OnReceive(byte[] data)
    {
        byte[] bytes = data;
        Debug.Log("我们这里是执行data数据" + bytes.Length);
        Debug.Log("第0位:" + bytes[0].ToString());
        Debug.Log("第0位:" + bytes[1].ToString());
        Debug.Log("第0位:" + bytes[2].ToString());
        Debug.Log("第0位:" + bytes[3].ToString());

        //bytes[0] = 243;
        //bytes[1] = 188;
        //bytes[2] = 125;
        //bytes[3] = 50;
    }

    public void OnOperationResponse(OperationResponse operationResponse)
    {
        //peer.OpCustom
    }

    //连接完成之后回调这里
    public void OnStatusChanged(StatusCode statusCode)
    {
        Debug.Log("连接状态" + statusCode.ToString());
        switch (statusCode)
        {
            case StatusCode.Connect:
                connected = true;
                break;
            case StatusCode.Disconnect:
                connected = false;
                break;
          
            default:
                break;
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (isFinished)
        {
            if (!connected)
            {
                peer.Connect(serverAddress, applicationName);
                
            }
            peer.Service();

            nextSendTickCount = Environment.TickCount + 100;
        }
	}

    private void OnDestroy()
    {
        peer.Disconnect();
    }

}
