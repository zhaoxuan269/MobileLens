using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderHandle : MonoBehaviour
{
    public Slider slider;
    public WebSocketClient webSocketClientInstance;
    public DataManager dataManagerInstance;

    private const string moveSpeedSliderValueKey = "moveSpeedSliderValue";
    private const string moveSpeedSliderMinValueKey = "moveSpeedSliderMinValue";
    private const string moveSpeedSliderMaxValueKey = "moveSpeedSliderMaxValue";

    private const string rangeSliderValueKey = "rangeSliderValue";
    private const string rangeSliderMinValueKey = "rangeSliderMinValue";
    private const string rangeSliderMaxValueKey = "rangeSliderMaxValue";

    //保存滑动条信息
    public class sliderInfo
    {
        public string infoHead;
        public DataManager.UserState infoUserState;
        public float value;
    }
    // Start is called before the first frame update
    void Start()
    {
        slider.onValueChanged.AddListener(onSliderValueChanged);

        sliderInit();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //滑动条变动
    void onSliderValueChanged(float value)
    {
        //根据用户的状态保存滑动条的值
        if (dataManagerInstance.getUserState() == DataManager.UserState.Zoom)
        {
            PlayerPrefs.SetFloat(moveSpeedSliderValueKey, value);
        }
        else if (dataManagerInstance.getUserState() == DataManager.UserState.LookPoint)
        {
            PlayerPrefs.SetFloat(rangeSliderValueKey, value);
        }
        //发送滑动条的值
        webSocketClientInstance.SendTextMessage(CreateSliderJson(value));
    }

    string CreateSliderJson(float mvalue)
    {
        sliderInfo temp = new sliderInfo {
                            infoHead ="Slider",
                            infoUserState = dataManagerInstance.getUserState(),
                            value = mvalue
        };

        string json = JsonUtility.ToJson(temp);
        return json;
    }

    //加载滑动条的值
    public void LoadSliderValue()
    {
        if (dataManagerInstance.getUserState() == DataManager.UserState.Zoom)
        {
            float savedMinValue = PlayerPrefs.GetFloat(moveSpeedSliderMinValueKey, 0);
            float savedMaxValue = PlayerPrefs.GetFloat(moveSpeedSliderMaxValueKey, 0);
            slider.minValue = savedMinValue;
            slider.maxValue = savedMaxValue;
            float savedValue = PlayerPrefs.GetFloat(moveSpeedSliderValueKey, 0);
            slider.value = savedValue;
        }
        else if (dataManagerInstance.getUserState() == DataManager.UserState.LookPoint)
        {
            float savedMinValue = PlayerPrefs.GetFloat(rangeSliderMinValueKey, 0);
            float savedMaxValue = PlayerPrefs.GetFloat(rangeSliderMaxValueKey, 0);
            slider.minValue = savedMinValue;
            slider.maxValue = savedMaxValue;
            float savedValue = PlayerPrefs.GetFloat(rangeSliderValueKey, 0);
            slider.value = savedValue;
        }
    }

    //初始化滑动条的值
    void sliderInit()
    {
        PlayerPrefs.SetFloat(moveSpeedSliderValueKey, 0.5f);
        PlayerPrefs.SetFloat(moveSpeedSliderMinValueKey, 0.0f);
        PlayerPrefs.SetFloat(moveSpeedSliderMaxValueKey, 1.0f);

        PlayerPrefs.SetFloat(rangeSliderValueKey, 0.5f);
        PlayerPrefs.SetFloat(rangeSliderMinValueKey, 0.0f);
        PlayerPrefs.SetFloat(rangeSliderMaxValueKey, 1.0f);
    }

}
