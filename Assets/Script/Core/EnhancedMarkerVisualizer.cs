using UnityEngine;

/// <summary>
/// 增强的 Marker 可视化
/// 创建一个更酷炫的可视化效果
/// </summary>
public class EnhancedMarkerVisualizer : MonoBehaviour
{
    private ARMarkerTracking _markerTracking;
    private GameObject _visualGroup;
    private GameObject _centerCube;
    private GameObject[] _orbitCubes;

    [Header("Visual Settings")]
    [SerializeField] private int _orbitCount = 4;
    [SerializeField] private float _orbitRadius = 0.25f; // 增大轨道半径
    [SerializeField] private float _orbitSpeed = 80f; // 加快旋转速度
    [SerializeField] private float _pulseSpeed = 3f; // 加快脉动速度
    [SerializeField] private Color _primaryColor = Color.yellow; // 更醒目的颜色
    [SerializeField] private Color _secondaryColor = Color.cyan; // 对比色

    void Start()
    {
        _markerTracking = FindObjectOfType<ARMarkerTracking>();
    }

    void Update()
    {
        if (_markerTracking.IsTracking && _markerTracking.CurrentMarker != null)
        {
            if (_visualGroup == null)
            {
                CreateEnhancedVisual();
            }
            else
            {
                UpdateVisual();
            }
        }
        else
        {
            if (_visualGroup != null)
            {
                Destroy(_visualGroup);
                _visualGroup = null;
            }
        }
    }

    private void CreateEnhancedVisual()
    {
        _visualGroup = new GameObject("MarkerVisualGroup");
        _visualGroup.transform.SetParent(_markerTracking.CurrentMarker.transform);
        _visualGroup.transform.localPosition = new Vector3(0, 0.2f, 0); // 提高高度到 0.2m
        _visualGroup.transform.localRotation = Quaternion.identity;

        // 中心立方体 - 增大尺寸并添加发光效果
        _centerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _centerCube.transform.SetParent(_visualGroup.transform);
        _centerCube.transform.localPosition = Vector3.zero;
        _centerCube.transform.localScale = Vector3.one * 0.12f; // 增大到 12cm
        
        Renderer centerRenderer = _centerCube.GetComponent<Renderer>();
        centerRenderer.material = new Material(Shader.Find("Standard"));
        centerRenderer.material.color = _primaryColor;
        centerRenderer.material.EnableKeyword("_EMISSION");
        centerRenderer.material.SetColor("_EmissionColor", _primaryColor * 2f);
        centerRenderer.material.SetFloat("_Metallic", 0.7f);
        centerRenderer.material.SetFloat("_Glossiness", 0.9f);

        // 轨道立方体 - 增大尺寸并添加发光
        _orbitCubes = new GameObject[_orbitCount];
        for (int i = 0; i < _orbitCount; i++)
        {
            _orbitCubes[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _orbitCubes[i].transform.SetParent(_visualGroup.transform);
            _orbitCubes[i].transform.localScale = Vector3.one * 0.08f; // 增大到 8cm
            
            Renderer orbitRenderer = _orbitCubes[i].GetComponent<Renderer>();
            orbitRenderer.material = new Material(Shader.Find("Standard"));
            orbitRenderer.material.color = _secondaryColor;
            orbitRenderer.material.EnableKeyword("_EMISSION");
            orbitRenderer.material.SetColor("_EmissionColor", _secondaryColor * 1.5f);
            orbitRenderer.material.SetFloat("_Metallic", 0.5f);
            orbitRenderer.material.SetFloat("_Glossiness", 0.8f);
        }

        Debug.Log("✓ 创建增强可视化效果（大号发光立方体组合）");
    }

    private void UpdateVisual()
    {
        if (_visualGroup == null) return;

        // 更新位置（与创建时的高度保持一致）
        _visualGroup.transform.localPosition = new Vector3(0, 0.2f, 0);

        // 中心立方体脉动（使用新的基础尺寸）
        float pulse = Mathf.Sin(Time.time * _pulseSpeed) * 0.5f + 1f;
        _centerCube.transform.localScale = Vector3.one * 0.12f * pulse;
        _centerCube.transform.Rotate(Vector3.up, Time.deltaTime * 150f); // 加快旋转

        // 轨道立方体旋转
        for (int i = 0; i < _orbitCubes.Length; i++)
        {
            float angle = (360f / _orbitCount) * i + Time.time * _orbitSpeed;
            float rad = angle * Mathf.Deg2Rad;
            
            Vector3 pos = new Vector3(
                Mathf.Cos(rad) * _orbitRadius,
                Mathf.Sin(Time.time * 3f + i) * 0.02f,
                Mathf.Sin(rad) * _orbitRadius
            );
            
            _orbitCubes[i].transform.localPosition = pos;
            _orbitCubes[i].transform.Rotate(Vector3.one, Time.deltaTime * 200f);
        }
    }
}

