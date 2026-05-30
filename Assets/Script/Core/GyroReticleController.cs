using UnityEngine;


/// “”“
/// 
/// 集中管理用设备陀螺仪控制屏幕准星的移动
/// 
public abstract class GyroReticleController : MonoBehaviour
{
    [Header("Reticle Settings")]
    [SerializeField] private RectTransform _reticle;
    [SerializeField] private float _sensitivity = 1000.0f;

    private Vector2 _screenCenter;
    protected Vector2 ScreenCenter => _screenCenter;
    protected virtual void Awake()
    {
        InitializeGyro();
    }

    protected void UpdateReticle()
    {
        if (_reticle == null)
        {
            return;
        }

        Vector3 acceleration = Input.acceleration;
        float xMovement = -acceleration.x;
        float yMovement = acceleration.z;

        Vector2 targetPos = _screenCenter + new Vector2(xMovement, yMovement) * _sensitivity;
        targetPos.x = Mathf.Clamp(targetPos.x, 0, Screen.width);
        targetPos.y = Mathf.Clamp(targetPos.y, 0, Screen.height);

        _reticle.position = targetPos;
    }

    private void InitializeGyro()
    {
        Input.gyro.enabled = true;
        _screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }
}

