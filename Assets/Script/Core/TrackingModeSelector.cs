using UnityEngine;

/// <summary>
/// Unified switch to choose between ArUco tracking and legacy 4-point raycast calibration.
/// Enable this on a scene object and assign the two tracking components.
/// </summary>
public class TrackingModeSelector : MonoBehaviour
{
    public enum TrackingMode
    {
        ArUco,
        Raycast4Point
    }

    [Header("Mode")]
    [SerializeField] private TrackingMode _mode = TrackingMode.ArUco;
    [SerializeField] private bool _logSwitch = true;

    [Header("Components")]
    [SerializeField] private ARMarkerTracking _arMarkerTracking;
    [SerializeField] private ARRaycast _arRaycast;

    [Header("Optional: ArUco only")]
    [SerializeField] private MonoBehaviour[] _arucoBehaviours;
    [SerializeField] private GameObject[] _arucoObjects;

    [Header("Optional: 4-point only")]
    [SerializeField] private MonoBehaviour[] _raycastBehaviours;
    [SerializeField] private GameObject[] _raycastObjects;

    void Start()
    {
        ApplyMode(_mode);
    }

    /// <summary>Switch mode at runtime.</summary>
    public void SetMode(TrackingMode mode)
    {
        if (_mode == mode) return;
        _mode = mode;
        ApplyMode(_mode);
    }

    private void ApplyMode(TrackingMode mode)
    {
        bool useAruco = mode == TrackingMode.ArUco;

        if (_arMarkerTracking != null) _arMarkerTracking.enabled = useAruco;
        if (_arRaycast != null) _arRaycast.enabled = !useAruco;

        SetBehavioursEnabled(_arucoBehaviours, useAruco);
        SetBehavioursEnabled(_raycastBehaviours, !useAruco);

        SetObjectsActive(_arucoObjects, useAruco);
        SetObjectsActive(_raycastObjects, !useAruco);

        if (_logSwitch)
        {
            Debug.Log($"[TrackingModeSelector] Mode -> {mode}");
        }
    }

    private void SetBehavioursEnabled(MonoBehaviour[] behaviours, bool enabled)
    {
        if (behaviours == null) return;
        foreach (var behaviour in behaviours)
        {
            if (behaviour != null) behaviour.enabled = enabled;
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (var go in objects)
        {
            if (go != null) go.SetActive(active);
        }
    }

    void OnValidate()
    {
        // Keep inspector switches in sync while editing
        if (!Application.isPlaying)
        {
            ApplyMode(_mode);
        }
    }
}
