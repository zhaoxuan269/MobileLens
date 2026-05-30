using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleManager : MonoBehaviour
{
    public ToggleGroup mToggleGroup;
    public DataManager DataManagerInstance;
    public ButtonHandler ButtonHandlerInstance;
    public SliderHandle SliderHandleInstance;
    private Dictionary<string,DataManager.UserState> toggleToState= new Dictionary<string, DataManager.UserState>();
    private Toggle[] toggles;

    // Start is called before the first frame update
    void Start()
    {
        //初始化字典，将Toggle名称映射到相应的用户状态
        initDictionary();

        //获取Toggle组中的所有toggle
        toggles = mToggleGroup.GetComponentsInChildren<Toggle>();
        foreach(var toggle in toggles)
        {
            toggle.onValueChanged.AddListener(delegate
            {
                ToggleValueChanged(toggle);
            });
        }
        //将所有Toggle设置为不可交互
        setTogglesInteractable(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Toggle值变化
    void ToggleValueChanged(Toggle t)
    {
        if (t.isOn)
        {
            //更新用户状态
            DataManagerInstance.setUserState(toggleToState[t.name]);
            //Debug.Log(t.name + " is Changed");
            //加载相应的滑动条值
            SliderHandleInstance.LoadSliderValue();
        }
        if(t.name == "Toggle_Origin")
        {
            ButtonHandlerInstance.myButton.interactable = t.isOn;
        }
    }

    void initDictionary()
    {
        toggleToState["Toggle_Origin"] = DataManager.UserState.Origin;
        toggleToState["Toggle_Zoom"] = DataManager.UserState.Zoom;
        toggleToState["Toggle_Rotate"] = DataManager.UserState.Rotate;
        toggleToState["Toggle_LookPoint"] = DataManager.UserState.LookPoint;
        toggleToState["Toggle_Partial"] = DataManager.UserState.Partial;
    }

    //设置所有的Toggle的交互状态
    public void setTogglesInteractable(bool interactable)
    {
        foreach (var toggle in toggles)
        {
            toggle.interactable = interactable;
        }
    }
}
