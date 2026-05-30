using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.iOS;
using TMPro;
using System;
///“”“
/// 
/// 
/// 
/// 
/// ”“”
public class ARRaycast : GyroReticleController
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager _raycastManager;//管理ar射线检测的管理器
    [SerializeField] private Camera _camera;//进行射线检测的摄像机

    private int currentStep = 0; //当前射线检测的步骤计数器
    Vector3[] cornerPositions = new Vector3[4];


    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _myText;

    [Header("Data")]
    [SerializeField] private DataManager _dataManagerInstance;//管理数据

    //private Vector2 lookPoint = new Vector2(1775, 1000);
    // Start is called before the first frame update

    protected override void Awake()

    {
        base.Awake();
    }

    // Update is called once per frame
    [System.Obsolete]
    void Update()
    {
        UpdateReticle();
            
    }

    public void initSet()
    {
        //以屏幕中心点进行射线检测
        Ray ray = _camera.ScreenPointToRay(ScreenCenter);
        var hits = new List<ARRaycastHit>();

        
        // 用于存储屏幕3个角在3D世界中的位置，以及lookpioint的位置  
        //左下  左上 右上  目标点
        if (_raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
        {
            // 获取射线击中的位置，并存储到cornerPositions数组中
            cornerPositions[currentStep] = hits[0].pose.position;
            Debug.Log($"Corner {currentStep} position: {cornerPositions[currentStep]}");
            float dis = Vector3.Distance(_camera.transform.position, cornerPositions[currentStep]);
            Debug.Log("Dis:" + dis);
            //Debug.Log("camera:"+camera.transform.rotation);
            //Debug.Log("gyro:" + Input.gyro.attitude);

            //CalculateLookPoint2D(cornerPositions);

            //计算相机方向并更新dataManagerInstance
            Vector3 forward = new Vector3(0, 1, 0);
            Vector3 face = _camera.transform.localRotation * forward;
            Vector3 pos = -face.normalized * dis;

            _dataManagerInstance.setOriginPosition(pos);
            _dataManagerInstance.setCameraDis(dis);


            Quaternion qVirtualBase = new Quaternion(0.0f, 0.0f, 0.0f, -1.0f);
            Quaternion qRealBase = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
            Quaternion qVirtualBaseInverse = Quaternion.Inverse(qVirtualBase);

            Quaternion q = qRealBase * (qVirtualBase * _camera.transform.localRotation);
            _dataManagerInstance.setOriginQuaternion(q);

            //更新ui文本
            _myText.text = q.ToString();
        }
        else
        {
            Debug.LogWarning($"Failed to detect corner. Please adjust the iPad's position or orientation.");
        }


        currentStep++;
        if (currentStep == 4)
        {
            Vector2 lookPoint2D = CalculateLookPoint2D(cornerPositions);
            if (lookPoint2D != Vector2.zero)
            {
                //必须是在计算出来正确的lookpoint2d坐标之后才执行
                Debug.LogWarning($"lookzz");
                _dataManagerInstance.setCorrectState(true);
                _dataManagerInstance.setLookPoint2DPos(lookPoint2D);

            }
            //重制currentstep
            currentStep = 0;
        }
        
    }

    public Vector2 CalculateLookPoint2D(Vector3[] inputPositions)
    {
        // 检查输入数组的长度是否为4
        if (inputPositions.Length != 4)
        {
            return Vector2.zero;

        }

        // 获取屏幕三个角的3D坐标
        Vector3 p0 = inputPositions[0]; // 左下角的3D坐标
        Vector3 p1 = inputPositions[1]; // 左上角的3D坐标
        Vector3 p2 = inputPositions[2]; // 右上角的3D坐标
        Vector3 lookPoint3D = inputPositions[3]; // lookpoint 的3D坐标

        // 步骤1：优化投影计算 - 通过平面拟合找到最佳平面
        Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized; // 计算平面法向量
        if (normal == Vector3.zero)
        {
            Debug.LogWarning("Calculated normal vector is zero, which may lead to NaN results.");
            return Vector2.zero;
        }
        // 计算lookpoint在最佳平面上的投影
        Vector3 projection = lookPoint3D - normal * Vector3.Dot(lookPoint3D - p0, normal);

        // 步骤2：改进投影的比例映射 - 使用更精确的比例映射将投影点转换到2D坐标系
        Vector3 screenXAxis = (p2 - p1).normalized; // X轴为右上角到左上角的向量
        Vector3 screenYAxis = (p1 - p0).normalized; // Y轴为左上角到左下角的向量


        // 计算投影点在屏幕平面内的相对坐标
        float x = Vector3.Dot(projection - p1, screenXAxis);
        float y = Vector3.Dot(projection - p0, screenYAxis);

        // 计算屏幕的宽度和高度
        float screenWidth = Vector3.Distance(p2, p1);  // 对应的实际屏幕宽度
        float screenHeight = Vector3.Distance(p1, p0); // 对应的实际屏幕高度
        if (screenWidth == 0 || screenHeight == 0)
        {
            Debug.LogWarning("Screen width or height is zero, which may lead to NaN results.");
            return Vector2.zero;
        }
        // 将相对坐标转换为2D坐标
        float u = x / screenWidth;
        float v = y / screenHeight;

        // 计算lookpoint在PC显示器的2D坐标
        Vector2 lookPoint2D = new Vector2(u, v);
        //Vector2 lookPoint2D = new Vector2(u * 3550, v * 2000);

        Debug.Log($"Calculated LookPoint2D: {lookPoint2D}");
        return lookPoint2D;



    }





}
