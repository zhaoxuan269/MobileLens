using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARSubsystems;

public class CameraLocation : MonoBehaviour
{
    public RectTransform rectTransform;
    private Vector2 screenCenter;
    private float sensitivity = 1000.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        screenCenter = new Vector2(Screen.width / 2,Screen.height/2);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 acceleration = Input.acceleration;
        float xMovement = -acceleration.x;
        float yMovement = acceleration.z;

        Vector2 targetPos = screenCenter + new Vector2(xMovement, yMovement)*sensitivity;

        targetPos.x = Mathf.Clamp(targetPos.x, 0, Screen.width);
        targetPos.y = Mathf.Clamp(targetPos.y, 0, Screen.height);

        rectTransform.position = targetPos;
        Debug.Log(rectTransform.position);
    }
}







