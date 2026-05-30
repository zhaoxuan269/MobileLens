using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TemplateHandle : MonoBehaviour
{
    // Start is called before the first frame update
    public Button myButton;
    public DataManager dataManagerInstance;
    public ToggleManager toggleManagerInstance;
    public WebSocketClient webSocketClientInstance;
    public ARRaycast ARRaycastInstance;
    public SrsPlayer srsPlayerInstance;
    void Start()
    {
        if (myButton != null)
        {
            myButton.onClick.AddListener(onClick);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void onClick()
    {
        //dataManagerInstance.setRealWorldCenter(dataManagerInstance.getRealCameraPosition());
        Debug.Log("template click");
        toggleManagerInstance.setTogglesInteractable(true);
        Debug.Log("calling initset");
        //ARRaycastInstance.initSet();
        //if (dataManagerInstance.getCorrectState())

        //srsPlayerInstance.Init();
        string json = webSocketClientInstance.createJson(dataManagerInstance.getTemplate(), dataManagerInstance.getOriginPosition()
                                    , dataManagerInstance.getOriginQuaternion(), dataManagerInstance.getCameraDis(), dataManagerInstance.getLookPoint2DPos(), dataManagerInstance.getTemplateState(),dataManagerInstance.getAnnotation());
        webSocketClientInstance.SendOriginMessage(json);
        

    }
}
