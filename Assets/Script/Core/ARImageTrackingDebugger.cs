using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

/// <summary>
/// AR 图像追踪调试工具
/// 显示详细的追踪状态和诊断信息
/// </summary>
public class ARImageTrackingDebugger : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager _trackedImageManager;
    
    private Dictionary<string, string> _debugInfo = new Dictionary<string, string>();
    private List<ARTrackedImage> _allTrackedImages = new List<ARTrackedImage>();

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

    void Start()
    {
        if (_trackedImageManager == null)
        {
            _trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        }

        UpdateSystemInfo();
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var image in eventArgs.added)
        {
            Debug.Log($"[DEBUG] ✓✓✓ 新检测到图像: {image.referenceImage.name}");
            Debug.Log($"[DEBUG]   追踪状态: {image.trackingState}");
            Debug.Log($"[DEBUG]   位置: {image.transform.position}");
            Debug.Log($"[DEBUG]   尺寸: {image.size}");
            
            if (!_allTrackedImages.Contains(image))
            {
                _allTrackedImages.Add(image);
            }
        }

        foreach (var image in eventArgs.updated)
        {
            Debug.Log($"[DEBUG] ○ 图像更新: {image.referenceImage.name}, 状态: {image.trackingState}");
        }

        foreach (var image in eventArgs.removed)
        {
            Debug.Log($"[DEBUG] ✗ 图像移除: {image.referenceImage.name}");
            _allTrackedImages.Remove(image);
        }
    }

    void Update()
    {
        UpdateDebugInfo();
    }

    private void UpdateSystemInfo()
    {
        _debugInfo["AR 支持"] = ARSession.state == ARSessionState.SessionTracking ? "✓ 正常" : "✗ 异常";
        
        if (_trackedImageManager != null)
        {
            _debugInfo["Image Manager"] = "✓ 已找到";
            
            var library = _trackedImageManager.referenceLibrary;
            if (library != null)
            {
                _debugInfo["Reference Library"] = $"✓ 已配置 ({library.count} 张图像)";
                
                // 列出所有图像
                for (int i = 0; i < library.count; i++)
                {
                    var refImage = library[i];
                    _debugInfo[$"  图像 {i}"] = $"{refImage.name} ({refImage.size.x:F2}m x {refImage.size.y:F2}m)";
                }
            }
            else
            {
                _debugInfo["Reference Library"] = "✗ 未配置！";
            }
        }
        else
        {
            _debugInfo["Image Manager"] = "✗ 未找到！";
        }
    }

    private void UpdateDebugInfo()
    {
        _debugInfo["追踪图像数量"] = _allTrackedImages.Count.ToString();
        
        if (_trackedImageManager != null)
        {
            int trackingCount = 0;
            foreach (var image in _trackedImageManager.trackables)
            {
                if (image.trackingState == TrackingState.Tracking)
                {
                    trackingCount++;
                }
            }
            _debugInfo["正在追踪"] = $"{trackingCount} 张";
        }
    }

    void OnGUI()
    {
        // 背景框
        GUI.Box(new Rect(10, 150, 350, 300), "AR Image Tracking 诊断");
        
        int y = 175;
        foreach (var kvp in _debugInfo)
        {
            GUI.Label(new Rect(20, y, 330, 20), $"{kvp.Key}: {kvp.Value}");
            y += 22;
        }

        // 当前追踪的图像详情
        y += 10;
        GUI.Label(new Rect(20, y, 330, 20), "=== 当前追踪图像 ===");
        y += 25;

        if (_allTrackedImages.Count == 0)
        {
            GUI.Label(new Rect(20, y, 330, 20), "⚠ 未检测到任何图像");
            y += 25;
            GUI.Label(new Rect(20, y, 330, 60), 
                "请检查：\n" +
                "1. Reference Library 已配置\n" +
                "2. 图像清晰、光线充足\n" +
                "3. 距离 30-100cm");
        }
        else
        {
            foreach (var image in _allTrackedImages)
            {
                if (image != null)
                {
                    string status = image.trackingState == TrackingState.Tracking ? "✓" : 
                                  image.trackingState == TrackingState.Limited ? "○" : "✗";
                    
                    GUI.Label(new Rect(20, y, 330, 20), 
                        $"{status} {image.referenceImage.name} - {image.trackingState}");
                    y += 22;
                }
            }
        }

        // 提示信息
        if (_debugInfo.ContainsKey("Reference Library") && 
            _debugInfo["Reference Library"].Contains("✗"))
        {
            GUIStyle errorStyle = new GUIStyle(GUI.skin.label);
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontSize = 20;
            errorStyle.alignment = TextAnchor.MiddleCenter;
            
            GUI.Label(
                new Rect(Screen.width / 2 - 200, Screen.height - 100, 400, 50),
                "⚠ 错误：Reference Library 未配置！",
                errorStyle
            );
        }
    }
}


