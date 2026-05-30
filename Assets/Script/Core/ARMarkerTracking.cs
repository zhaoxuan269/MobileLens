using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// AR Marker 追踪系统
/// 自动识别桌面 Marker，建立参考坐标系，支持旋转/缩放/位置映射
/// </summary>
public class ARMarkerTracking : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARTrackedImageManager _trackedImageManager;
    [SerializeField] private Camera _arCamera;

    [Header("Data Management")]
    [SerializeField] private DataManager _dataManager;

    [Header("Marker Settings")]
    [SerializeField] private string _targetMarkerName = "DesktopMarker";
    [SerializeField] private float _trackingConfidenceThreshold = 0.5f;

    [Header("Calibration")]
    [SerializeField] private bool _autoCalibrate = true;
    [SerializeField] private float _calibrationDelay = 0.5f; // 延迟校准，确保追踪稳定

    // 追踪状态
    private ARTrackedImage _currentMarker;
    private bool _isTracking = false;
    private bool _isCalibrated = false;

    // 基准值（用于计算相对变化）
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _baseDistance;
    private float _calibrationStartTime;

    // 调试可视化
    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private GameObject _markerVisualizerPrefab;
    private GameObject _currentVisualizer;

    void OnEnable()
    {
        if (_trackedImageManager != null)
        {
            _trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }
    }

    void OnDisable()
    {
        if (_trackedImageManager != null)
        {
            _trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 处理新检测到的图像
        foreach (var trackedImage in eventArgs.added)
        {
            HandleTrackedImage(trackedImage);
        }

        // 处理更新的图像
        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateTrackedImage(trackedImage);
        }

        // 处理丢失的图像
        foreach (var trackedImage in eventArgs.removed)
        {
            HandleLostImage(trackedImage);
        }
    }

    void Update()
    {
        if (_isTracking && _isCalibrated)
        {
            UpdateInteractionData();
        }

        // 检查是否需要延迟校准
        if (_isTracking && !_isCalibrated && _autoCalibrate)
        {
            if (Time.time - _calibrationStartTime >= _calibrationDelay)
            {
                PerformCalibration();
            }
        }
    }

    private void HandleTrackedImage(ARTrackedImage trackedImage)
    {
        // 检查是否是目标 Marker
        if (trackedImage.referenceImage.name != _targetMarkerName)
        {
            return;
        }

        // 检查追踪质量
        if (trackedImage.trackingState != TrackingState.Tracking)
        {
            return;
        }

        _currentMarker = trackedImage;
        _isTracking = true;
        _calibrationStartTime = Time.time;

        Debug.Log($"✓ 检测到 Marker: {_targetMarkerName}");

        // 创建可视化标记
        if (_showDebugInfo && _markerVisualizerPrefab != null)
        {
            _currentVisualizer = Instantiate(_markerVisualizerPrefab, trackedImage.transform);
        }
    }

    private void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        // 检查是否是目标 Marker
        if (trackedImage.referenceImage.name != _targetMarkerName)
        {
            return;
        }

        // 如果还没有设置 _currentMarker，现在设置它
        if (_currentMarker == null && trackedImage.trackingState == TrackingState.Tracking)
        {
            _currentMarker = trackedImage;
            _isTracking = true;
            _calibrationStartTime = Time.time;
            Debug.Log($"✓ 检测到 Marker (via update): {_targetMarkerName}");
            
            // 创建可视化标记
            if (_showDebugInfo && _markerVisualizerPrefab != null)
            {
                _currentVisualizer = Instantiate(_markerVisualizerPrefab, trackedImage.transform);
            }
            return;
        }

        // 只更新当前 Marker 的状态
        if (trackedImage != _currentMarker) return;

        // 更新追踪状态
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            _isTracking = true;
        }
        else if (trackedImage.trackingState == TrackingState.Limited)
        {
            Debug.LogWarning("Marker 追踪质量下降");
            _isTracking = false;
        }
        else
        {
            _isTracking = false;
            Debug.LogWarning("Marker 丢失");
        }
    }

    private void HandleLostImage(ARTrackedImage trackedImage)
    {
        if (trackedImage == _currentMarker)
        {
            _isTracking = false;
            Debug.LogWarning("Marker 已移除");

            if (_currentVisualizer != null)
            {
                Destroy(_currentVisualizer);
            }
        }
    }

    /// <summary>
    /// 执行校准，建立参考坐标系
    /// </summary>
    private void PerformCalibration()
    {
        if (_currentMarker == null || !_isTracking)
        {
            Debug.LogError("无法校准：Marker 未追踪");
            return;
        }

        // 记录基准值
        _basePosition = _arCamera.transform.position;
        _baseRotation = _arCamera.transform.rotation;
        _baseDistance = Vector3.Distance(
            _arCamera.transform.position,
            _currentMarker.transform.position
        );

        // 计算相对于 Marker 的原点位置
        Vector3 originPosition = CalculateOriginPosition();
        Quaternion originRotation = CalculateOriginRotation();

        // 更新 DataManager
        _dataManager.setOriginPosition(originPosition);
        _dataManager.setOriginQuaternion(originRotation);
        _dataManager.setCameraDis(_baseDistance);
        _dataManager.setCorrectState(true);

        _isCalibrated = true;

        Debug.Log($"✓✓✓ Marker 校准完成！");
        Debug.Log($"  - 基准距离: {_baseDistance:F2}m");
        Debug.Log($"  - 基准位置: {_basePosition}");
        Debug.Log($"  - 基准旋转: {_baseRotation.eulerAngles}");
    }

    /// <summary>
    /// 手动触发校准（如果需要重新校准）
    /// </summary>
    public void RecalibrateManually()
    {
        if (_isTracking)
        {
            _isCalibrated = false;
            PerformCalibration();
        }
        else
        {
            Debug.LogWarning("无法校准：Marker 未追踪");
        }
    }

    /// <summary>
    /// 实时更新交互数据（旋转、缩放、位置）
    /// </summary>
    private void UpdateInteractionData()
    {
        if (_currentMarker == null) return;

        // 当前值
        Vector3 currentPosition = _arCamera.transform.position;
        Quaternion currentRotation = _arCamera.transform.rotation;
        float currentDistance = Vector3.Distance(
            currentPosition,
            _currentMarker.transform.position
        );

        // 计算旋转增量（相对于基准）
        Quaternion rotationDelta = Quaternion.Inverse(_baseRotation) * currentRotation;
        
        // 计算缩放因子（基于距离变化）
        float scaleFactor = _baseDistance / Mathf.Max(currentDistance, 0.01f);
        
        // 计算前后移动量
        Vector3 positionDelta = currentPosition - _basePosition;
        float forwardMovement = Vector3.Dot(
            positionDelta,
            _arCamera.transform.forward
        );

        // 计算相对于 Marker 的 2D 位置（用于鼠标映射）
        Vector3 localPosition = _currentMarker.transform.InverseTransformPoint(currentPosition);

        // 更新 DataManager（只使用已有方法）
        // DataManager 会自动从 XROrigin 获取实时相机数据
        // 这里我们只需要更新距离即可
        _dataManager.setCameraDis(currentDistance);

        if (_showDebugInfo)
        {
            DebugDisplay(rotationDelta, scaleFactor, forwardMovement);
        }
    }

    private Vector3 CalculateOriginPosition()
    {
        // 计算相对于 Marker 的原点位置
        Vector3 forward = new Vector3(0, 1, 0);
        Vector3 face = _arCamera.transform.localRotation * forward;
        Vector3 originPos = -face.normalized * _baseDistance;
        return originPos;
    }

    private Quaternion CalculateOriginRotation()
    {
        // 计算基准旋转（与现有代码保持一致）
        Quaternion qVirtualBase = new Quaternion(0.0f, 0.0f, 0.0f, -1.0f);
        Quaternion qRealBase = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        Quaternion q = qRealBase * (qVirtualBase * _arCamera.transform.localRotation);
        return q;
    }

    private void DebugDisplay(Quaternion rotation, float scale, float forward)
    {
        // 每秒更新一次日志，避免刷屏
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[AR Marker Tracking]");
            Debug.Log($"  旋转: {rotation.eulerAngles}");
            Debug.Log($"  缩放: {scale:F2}x");
            Debug.Log($"  前后: {forward:F2}m");
            Debug.Log($"  追踪状态: {(_isTracking ? "正常" : "丢失")}");
        }
    }

    // 公共 API
    public bool IsTracking => _isTracking;
    public bool IsCalibrated => _isCalibrated;
    public ARTrackedImage CurrentMarker => _currentMarker;
    public float CurrentDistance => _currentMarker != null ? 
        Vector3.Distance(_arCamera.transform.position, _currentMarker.transform.position) : 0f;

    void OnGUI()
    {
        if (!_showDebugInfo) return;

        GUI.Box(new Rect(10, 10, 250, 120), "AR Marker Tracking");
        
        GUI.Label(new Rect(20, 35, 230, 20), $"Tracking: {(_isTracking ? "✓" : "✗")}");
        GUI.Label(new Rect(20, 55, 230, 20), $"Calibrated: {(_isCalibrated ? "✓" : "✗")}");
        
        if (_currentMarker != null)
        {
            GUI.Label(new Rect(20, 75, 230, 20), $"Marker: {_currentMarker.referenceImage.name}");
            GUI.Label(new Rect(20, 95, 230, 20), $"Distance: {CurrentDistance:F2}m");
        }
        
        if (_isTracking && !_isCalibrated)
        {
            float remaining = _calibrationDelay - (Time.time - _calibrationStartTime);
            GUI.Label(new Rect(20, 115, 230, 20), $"Calibrating in {remaining:F1}s");
        }
    }
}

