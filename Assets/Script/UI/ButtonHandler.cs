using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class ButtonHandler : MonoBehaviour
{
    public Button myButton;
    public DataManager dataManagerInstance;
    public TMP_InputField myInputField;
    public ToggleManager toggleManagerInstance;
    public WebSocketClient webSocketClientInstance;
    public ARRaycast ARRaycastInstance;
    public SrsPlayer srsPlayerInstance;
    
    [Header("Marker Tracking (可选)")]
    public ARTrackedImageManager trackedImageManager;
    public ARMarkerTracking markerTrackingInstance;
    public bool useMarkerTracking = true; // 默认使用 Marker（ArUco），旧四点标定仅备用
    // Start is called before the first frame update
    void Start()
    {
        if (myButton != null)
        {
            myButton.onClick.AddListener(onClick);
        }
        
        // 如果使用 Marker 追踪模式，初始时关闭追踪（等待按钮点击）
        if (useMarkerTracking)
        {
            if (trackedImageManager != null)
            {
                trackedImageManager.enabled = false;
                Debug.Log("Marker 追踪已禁用，等待按钮启动");
            }
            
            if (markerTrackingInstance != null)
            {
                markerTrackingInstance.enabled = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void onClick()
    {
        Debug.Log("btn click");

        // 根据配置选择使用 Marker 追踪或四点标定
        if (useMarkerTracking)
        {
            // ===== 使用 Marker 追踪 =====
            Debug.Log("启动 Marker 追踪模式");
            
            // 启用 Marker 追踪
            if (trackedImageManager != null)
            {
                trackedImageManager.enabled = true;
            }
            
            if (markerTrackingInstance != null)
            {
                markerTrackingInstance.enabled = true;
            }
            
            toggleManagerInstance.setTogglesInteractable(true);
            
            // 设置注释
            if (myInputField != null)
            {
                string inputText = myInputField.text;
                Debug.Log("myAnnotation: " + inputText);
                dataManagerInstance.setAnnotation(inputText);
            }
            
            // Marker 追踪会自动完成校准，等待校准完成后发送数据
            StartCoroutine(WaitForMarkerCalibration());
        }
        else
        {
            // ===== 使用原来的四点标定 =====
            //Debug.Log("使用四点标定模式");
            //toggleManagerInstance.setTogglesInteractable(true);
            //Debug.Log("calling initset");
            //ARRaycastInstance.initSet();
            
            //if (dataManagerInstance.getCorrectState())
            //{
                //srsPlayerInstance.Init();
                //string inputText = myInputField.text;
                //Debug.Log("myAnnotation: " + inputText);
                //dataManagerInstance.setAnnotation(inputText);
                //string json = webSocketClientInstance.createJson(dataManagerInstance.getUserState()
                                        //, dataManagerInstance.getOriginPosition()
                                        //, dataManagerInstance.getOriginQuaternion(), dataManagerInstance.getCameraDis(), dataManagerInstance.getLookPoint2DPos(), 0f, dataManagerInstance.getAnnotation());
                //webSocketClientInstance.SendOriginMessage(json);
            //}
            Debug.Log("STOP四点标定模式");

        }
    }
    
    // 等待 Marker 校准完成
    private IEnumerator WaitForMarkerCalibration()
    {
        Debug.Log("等待 Marker 校准...");
        
        // 最多等待 10 秒
        float timeout = 10f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            if (markerTrackingInstance != null && markerTrackingInstance.IsCalibrated)
            {
                Debug.Log("✓ Marker 校准完成，发送初始数据");
                
                // 初始化视频
                srsPlayerInstance.Init();
                
                // 发送初始校准数据
                string json = webSocketClientInstance.createJson(dataManagerInstance.getUserState()
                                        , dataManagerInstance.getOriginPosition()
                                        , dataManagerInstance.getOriginQuaternion()
                                        , dataManagerInstance.getCameraDis()
                                        , dataManagerInstance.getLookPoint2DPos()
                                        , 0f
                                        , dataManagerInstance.getAnnotation());
                webSocketClientInstance.SendOriginMessage(json);
                
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.LogWarning("⚠ Marker 校准超时，请确保对准了 Marker");
    }
}
