using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System;

public class WebSocketClient : MonoBehaviour

{
    [Header("WebSocket Settings")]
    [SerializeField] private string _defaultUrl = "ws://172.16.172.184:8888";
    //public string url;
    public WebSocket _websocket;
    private Coroutine _sendingCoroutine;
    private DataManager _dataManager;

    //private Coroutine messageCoroutine;
    //public DataManager dataManagerInstance;

    private DataManager.UserState _prevUserState;

    //创建字典 状态 默认值
    private readonly Dictionary<DataManager.UserState, float> _sendIntervals = new()
    {
        { DataManager.UserState.Origin, 0.1f },
        { DataManager.UserState.Zoom, 0.1f },
        { DataManager.UserState.LookPoint, 0.1f },
        { DataManager.UserState.Rotate, 0.1f },
        { DataManager.UserState.Partial, 0.1f },
    };

    // Start is called before the first frame update
    async void Start()
    {
        _websocket = new WebSocket(_defaultUrl);

        //websocket = new WebSocket("ws://192.168.8.143:8888");
        //websocket = new WebSocket(url.ToString());
        //Debug.Log(url) ;

        _websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        _websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        _websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        _websocket.OnMessage += (bytes) =>
        {
            Debug.Log("OnMessage!");
            Debug.Log(bytes);

            // getting the message as a string
            // var message = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log("OnMessage! " + message);
        };

        // Keep sending messages at every 0.3s
        //InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);
        _prevUserState = DataManager.UserState.Origin;
        StartSending();

        // waiting for messages
        await _websocket.Connect();
    }

    //每帧调用一次
    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            if (_websocket != null)
            {
                _websocket.DispatchMessageQueue();
            }
        #endif

        //如果用户状态发生变化，重新开始发送消息
        if (_dataManager != null && _prevUserState != _dataManager.getUserState())
        {
            _prevUserState = _dataManager.getUserState();
            StartSending();
        }
    }

    async void SendWebSocketMessage()
    {
        if (_websocket.State == WebSocketState.Open)
        {
            // Sending bytes
            await _websocket.Send(new byte[] { 10, 20, 30 });

            // Sending plain text
        }
    }

    //发送原点消息
    public async void SendOriginMessage(String s)
    {
        if (_websocket.State == WebSocketState.Open)
        {
            await _websocket.SendText(s);
        }
    }

    /// <summary>
    /// 发送文本消息（通用方法）
    /// </summary>
    public async void SendTextMessage(string message)
    {
        if (_websocket != null && _websocket.State == WebSocketState.Open)
        {
            await _websocket.SendText(message);
        }
        else
        {
            Debug.LogWarning("WebSocket 未连接，无法发送消息");
        }
    }

    private async void OnApplicationQuit()
    {
        await _websocket.Close();
    }

    void StartSending()
    {
        CancelInvoke("SendingMessage");

        InvokeRepeating("SendingMessage",0.0f,interval(_prevUserState));
    }

    async void SendingMessage()
    {
        if (_websocket.State == WebSocketState.Open)
        {
            string text = createJson(_dataManager.getUserState(),
                                    _dataManager.getRealCameraPosition(),
                                    _dataManager.getRealCameraRotaion(), _dataManager.getCameraDis(), _dataManager.getLookPoint2DPos(),0,_dataManager.getAnnotation());
            switch (_dataManager.getUserState())
            {
                case DataManager.UserState.Origin:
                    //await websocket.SendText(text);
                    break;
                case DataManager.UserState.Zoom:
                    await _websocket.SendText(text);
                    break;
                case DataManager.UserState.LookPoint:
                    await _websocket.SendText(text);
                    break;
                case DataManager.UserState.Rotate:
                    await _websocket.SendText(text);
                    break;
                case DataManager.UserState.Partial:
                    await _websocket.SendText(text);
                    break;
            }
        }
    }

    public void StopSendingMessage()
    {
        CancelInvoke("SendingMessage");
    }

    public string createJson(DataManager.UserState userState,Vector3 v,Quaternion q,float d,Vector2 l, float tem, string anno)
    {
        DataManager.Info temp = new DataManager.Info
        {
            infoHead = "Interaction",
            infoUserState = userState,
            position = v,
            dis = d,
            lookPoint = l,
            transfer = tem,
            annotation = anno
        };

        switch (temp.infoUserState)
        {
            case DataManager.UserState.Origin:
                temp.quaternion = q;
                temp.dis = _dataManager.getCameraDis();
                break;

            case DataManager.UserState.Zoom:
                temp.quaternion = q;
                temp.dealt = _dataManager.getDealtForward();
                break;

            case DataManager.UserState.LookPoint:
                temp.position = _dataManager.getDealtPos();
                break;

            case DataManager.UserState.Rotate:
                temp.quaternion = _dataManager.getRotationRelativeToBase(
                    _dataManager.getRealCameraRotaion());
                break;
            case DataManager.UserState.Partial:
                temp.quaternion = _dataManager.getRotationRelativeToBase(
                    _dataManager.getRealCameraRotaion()); ;
                temp.dealt = _dataManager.getDealtForward();
                break;
        }

        string json = JsonUtility.ToJson(temp);
        return json;
    }

    float interval(DataManager.UserState userState)
    {
        if(userState == DataManager.UserState.Origin)
        {
            return 1.0f / 10.0f;
        }
        else if(userState == DataManager.UserState.Zoom)
        {
            return 1.0f / 10.0f;
        }
        else
        {
            return 1.0f / 10.0f;
        }
    }


}
