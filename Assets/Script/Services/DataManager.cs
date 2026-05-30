using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;

public class DataManager : MonoBehaviour
{
    public enum UserState
    {
        Origin,
        Zoom,
        Rotate,
        LookPoint,
        Partial,
        texture1,
        texture2
    }


    public class Info
    {
        public string infoHead;
        public UserState infoUserState;
        public Vector3 position;
        public Vector2 lookPoint;
        public Quaternion quaternion;
        public float transfer;
        public string annotation;

        public float dealt;
        public float dis;
    }

    public XROrigin xrOrigin;

    private UserState userState;
    private Vector3 originPosition;
    private Quaternion originQuaternion;

    private Vector3 realCameraPosition;
    private Quaternion realCameraRotation;
    private Vector3 prevCameraPosition;
    private Vector3 disPlacement;
    private float dealtForward;
    private float preCameraDis;
    private bool correctState;
    private Vector2 lookPoint2Dpos;
    private string annotat;
    // Start is called before the first frame update
    //private Template template;
    void Start()
    {
        userState = UserState.Origin;
        originPosition = new Vector3(0.0f,0.0f,0.0f);
        preCameraDis = 0;
        correctState = false;
    }

    // Update is called once per frame
    void Update()
    {
        //获取相机当前位置和旋转
        realCameraPosition = xrOrigin.Camera.transform.localPosition;
        //realCameraRotation = Input.gyro.attitude;
        realCameraRotation = xrOrigin.Camera.transform.localRotation;

        //更新相机前进距离
        updateDealtForward();
        prevCameraPosition = realCameraPosition;
        //Debug.Log(dealtForward.ToString());
    }

    public void setUserState(UserState newState)
    {
        userState = newState;
        Debug.Log("state changed");
    }

    public UserState getUserState()
    {
        return userState;
    }

    public UserState getTemplate()
    {
        return UserState.texture1;
    }



    //public void setTemplate(Template newtemplate)
    //{
    //    template = newtemplate;
    //    Debug.Log("template changed");
    //}

    //public Template getTemplate()
    //{
    //    return template;
    //}
    public void setAnnotation(string ann)
    {
        annotat = ann;
    }

    public string getAnnotation()
    {
        return annotat;
    }
    public void setCameraDis(float dis)
    {
        preCameraDis = dis;
    }

    public float getCameraDis()
    {
        return preCameraDis;
    }

    public Vector3 getOriginPosition()
    {
        return originPosition;
    }

    public Quaternion getOriginQuaternion()
    {
        return originQuaternion;
    }

    public void setOriginPosition(Vector3 v)
    {
        originPosition = v;
    }

    public void setOriginQuaternion(Quaternion q)
    {
        originQuaternion = q;
    }

    public Vector3 getRealCameraPosition()
    {
        return realCameraPosition;
    }

    public Quaternion getRealCameraRotaion()
    {
        return realCameraRotation;
    }

    private void updateDealtForward()
    {
        disPlacement = realCameraPosition - prevCameraPosition;
        dealtForward = Vector3.Dot(disPlacement, xrOrigin.Camera.transform.forward);
    }
     
    public float getDealtForward()
    {
        return dealtForward;
    }

    //获取相对于基准的旋转
    public Quaternion getRotationRelativeToBase(Quaternion q)
    {
        Quaternion Qbase = new Quaternion(0, 0, 0, 1);
        Quaternion temp = originQuaternion * (Qbase * q);
        return temp;
        //return new Quaternion(temp.x,temp.y,-temp.z,temp.w);
    }

    public Vector3 getDealtPos()
    {
        return disPlacement;
    }
    public void setCorrectState(bool state)
    {
        correctState = state;
    }

    public bool getCorrectState()
    {
        return correctState;
    }
    public void setLookPoint2DPos(Vector2 pos)
    {
        lookPoint2Dpos = pos;
    }
    public Vector2 getLookPoint2DPos()
    {
        return lookPoint2Dpos;
    }
    public float getTemplateState()
    {
        return 1.0f;
    }
    // ===== AR Marker 追踪扩展 =====

private Quaternion rotationRelativeToBase;
private float scaleFactor = 1.0f;
private Vector2 localPosition2D;

// 设置相对于基准的旋转
public void setRotationRelativeToBase(Quaternion q)
{
    rotationRelativeToBase = q;
}

// 获取相对于基准的旋转
public Quaternion getRotationDelta()
{
    return rotationRelativeToBase;
}

// 设置缩放因子
public void setScaleFactor(float scale)
{
    scaleFactor = scale;
}

// 获取缩放因子
public float getScaleFactor()
{
    return scaleFactor;
}

// 设置2D局部位置（用于鼠标映射）
public void setLocalPosition2D(Vector2 pos)
{
    localPosition2D = pos;
}

// 获取2D局部位置
public Vector2 getLocalPosition2D()
{
    return localPosition2D;
}

// 设置实时相机位置（由 Marker 追踪更新）
public void setRealCameraPosition(Vector3 pos)
{
    realCameraPosition = pos;
}

// 设置实时相机旋转（由 Marker 追踪更新）
public void setRealCameraRotation(Quaternion rot)
{
    realCameraRotation = rot;
}

// 设置前进移动量
public void setDealtForward(float forward)
{
    dealtForward = forward;
}
}
