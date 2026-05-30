using UnityEngine;
using TMPro;

/// <summary>
/// Marker 可视化工具
/// 当检测到 Marker 时，在其上方显示 3D 对象或信息
/// </summary>
public class MarkerVisualizer : MonoBehaviour
{
    [Header("Debug Info")]
    [SerializeField] private bool _showDebugText = true;
    [SerializeField] private TextMeshProUGUI _debugText;

    private bool _hasLoggedSuccess;
    private ARMarkerTracking _markerTracking;

    void Start()
    {
        // Find ARMarkerTracking component
        _markerTracking = FindObjectOfType<ARMarkerTracking>();
        
        if (_markerTracking == null)
        {
            Debug.LogError("ARMarkerTracking component not found!");
            enabled = false;
            return;
        }

    }

     void Update()
     {if (_markerTracking.IsTracking && _markerTracking.CurrentMarker != null)
        {
            // Marker detected
            if (!_hasLoggedSuccess)
            {
                Debug.Log("✓ Marker detected");
                _hasLoggedSuccess = true;
            }

            // 更新调试信息
            if (_showDebugText && _debugText != null)
            {
                UpdateDebugText();
            }
        }
        else
        {
            // Marker lost
            if (_showDebugText && _debugText != null)
            {
                _debugText.text = "Waiting for marker...";
            }

            _hasLoggedSuccess = false;
        }
    }

    private void UpdateDebugText()
    {
        if (_markerTracking.IsCalibrated)
        {
            _debugText.text = $"✓ Marker detected\n" +
                            $"Distance: {_markerTracking.CurrentDistance:F2}m\n" +
                            $"Status: Calibrated";
        }
        else if (_markerTracking.IsTracking)
        {
            _debugText.text = $"○ Tracking marker\n" +
                            $"Distance: {_markerTracking.CurrentDistance:F2}m\n" +
                            $"Status: Calibrating...";
        }
    }

}

