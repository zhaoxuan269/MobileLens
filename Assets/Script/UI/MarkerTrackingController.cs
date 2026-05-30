using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Marker 追踪控制器
/// 通过 Button 控制 Marker 检测的启动和停止
/// </summary>
public class MarkerTrackingController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ARTrackedImageManager _trackedImageManager;
    [SerializeField] private ARMarkerTracking _markerTracking;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _stopButton;

    [Header("UI Feedback")]
    [SerializeField] private Text _statusText;

    private bool _isTracking = false;

    void Start()
    {
        // 查找组件
        if (_trackedImageManager == null)
        {
            _trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        }

        if (_markerTracking == null)
        {
            _markerTracking = FindObjectOfType<ARMarkerTracking>();
        }

        // 设置按钮事件
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(StartTracking);
        }

        if (_stopButton != null)
        {
            _stopButton.onClick.AddListener(StopTracking);
        }

        // 初始状态：关闭追踪
        StopTracking();
        UpdateUI();
    }

    /// <summary>
    /// 开始 Marker 追踪
    /// </summary>
    public void StartTracking()
    {
        if (_trackedImageManager != null)
        {
            _trackedImageManager.enabled = true;
            _isTracking = true;
            Debug.Log("✓ Marker 追踪已启动");
        }

        if (_markerTracking != null)
        {
            _markerTracking.enabled = true;
        }

        UpdateUI();
    }

    /// <summary>
    /// 停止 Marker 追踪
    /// </summary>
    public void StopTracking()
    {
        if (_trackedImageManager != null)
        {
            _trackedImageManager.enabled = false;
            _isTracking = false;
            Debug.Log("✗ Marker 追踪已停止");
        }

        if (_markerTracking != null)
        {
            _markerTracking.enabled = false;
        }

        UpdateUI();
    }

    /// <summary>
    /// 切换追踪状态
    /// </summary>
    public void ToggleTracking()
    {
        if (_isTracking)
        {
            StopTracking();
        }
        else
        {
            StartTracking();
        }
    }

    private void UpdateUI()
    {
        // 更新按钮状态
        if (_startButton != null)
        {
            _startButton.interactable = !_isTracking;
        }

        if (_stopButton != null)
        {
            _stopButton.interactable = _isTracking;
        }

        // 更新状态文字
        if (_statusText != null)
        {
            _statusText.text = _isTracking ? "检测中..." : "等待启动";
            _statusText.color = _isTracking ? Color.green : Color.gray;
        }
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(StartTracking);
        }

        if (_stopButton != null)
        {
            _stopButton.onClick.RemoveListener(StopTracking);
        }
    }

    // 提供给其他脚本调用的属性
    public bool IsTracking => _isTracking;
}


