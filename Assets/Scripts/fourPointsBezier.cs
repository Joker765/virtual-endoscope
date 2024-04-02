using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fourPointsBezier : MonoBehaviour
{
    public Transform way; //曲线节点的父对象
    public float speed =4.0f;
    public float rotateSpeed = 30.0f;
    public bool auto_rotate =true; 
    private Transform[] p = new Transform[0];
    private Vector3 previousPosition;
    private Quaternion previousRotation; 
    private int id; //每段贝塞尔曲线的起点id
    private Transform obj;
    private float time=0;

    //定义私有变量
    private Vector3 preWay;

    //定义该变量的公开属性
    public Vector3 PreWay{get{return preWay;}}
    
    //定义该变量的公开方法
    public Vector3 GetPreWay()
    {
        return preWay;
    }

    private void Awake(){
        p = way.GetComponentsInChildren<Transform>();  //返回值是子物体的<Transform>组件列表,但是p[0]是父物体
        foreach (Transform t in p) Debug.Log("t的值为: "+t);
        Debug.Log(p.Length);

        obj=this.transform;
        previousPosition=preWay=obj.position=p[1].position;
        id=1; 
    }


    void Update (){
        
        time+=Time.deltaTime*speed/16;
        if(time>1) {
            id++; 
            time--; 
        }
        if(id==1){
            obj.position=BezierBegin(time);
            
        } else if (id<p.Length-2 ) {

            obj.position=Bezier4(time,id);

        } else if(id == p.Length-2){
            obj.position=BezierEnd(time,id);
        }
        if(FrameRotate()) return;
        if(auto_rotate){
            previousRotation = obj.rotation;
            obj.LookAt(previousPosition); //新的rotation
            previousPosition=obj.position;
            obj.rotation = Quaternion.Slerp(previousRotation,obj.rotation,Time.deltaTime); //从前一帧的角度到lookat方向进行插值,平滑镜头。不停地从自己的角度向目标角度靠近
        //Lerp线性插值 对距离进行插值，角度不均  Slerp 球形插值,角度变化均匀。
        }
        preWay=p[id<p.Length-1 ? id : p.Length-1].position;
    }

    bool FrameRotate(){
        bool flag =false;
        if (Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))
        {

            this.transform.Rotate(0,-rotateSpeed*Time.deltaTime,0,Space.Self);
            flag= true;
        }
        if (Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))
        {

            this.transform.Rotate(0,rotateSpeed*Time.deltaTime,0,Space.Self);
            flag= true;
        }


        if (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))
        {
            this.transform.Rotate(-rotateSpeed*Time.deltaTime,0,0,Space.Self);
            flag= true;
        }
        if (Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.Rotate(rotateSpeed*Time.deltaTime,0,0,Space.Self);
            flag= true;
        }
        return flag;
    }


    Vector3 BezierBegin(float t){ //2次贝塞尔曲线
        Vector3 end = p[2].position;
        Vector3 middle = end+(p[1].position-p[3].position)/4;

        float t1 = (1 - t)*(1 - t);
        float t2 = 2*t*(1 - t) ;
        float t3 = t*t;
        return p[1].position * t1 + middle * t2 + end * t3;
    }
    Vector3 BezierEnd(float t,int id){ //2次贝塞尔曲线
        Vector3 end = p[id+1].position;
        Vector3 middle = p[id].position  +(end- p[id-1].position)/4; //设id=2,则middle为 p2+( p2-p1 + p3-p2)/4

        float t1 = (1 - t)*(1 - t);
        float t2 = 2*t*(1 - t) ;
        float t3 = t*t;
        return p[id].position * t1 + middle * t2 + end * t3;
    }

    Vector3 Bezier4(float t,int id){ //3次 4个控制点的贝塞尔方程
        Vector3 end = p[id+1].position;
        Vector3 middle1 = p[id].position +(end- p[id-1].position)/4;  //不加f会报错 error CS0019: Operator '*' cannot be applied to operands of type 'Vector3' and 'double'
        Vector3 middle2 = end+(p[id].position-p[id+2].position)/4;


        float t1 = (1 - t)*(1 - t)*(1 - t);
        float t2 = 3*t*(1 - t) * (1 - t);
        float t3 = 3*t*t* (1 - t);
        float t4 = t*t*t;
        return p[id].position * t1 + middle1 * t2 + middle2 * t3 + end * t4;
    }
}
