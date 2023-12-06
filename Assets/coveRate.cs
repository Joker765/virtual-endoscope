using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class coveRate : MonoBehaviour
{
    public static int zbuffsize = 512;
    private int frameCount = 0;
    public Transform testPos;
    [SerializeField]
    private GameObject colon;
    [SerializeField]
    private Camera mCamera;
    private Vector3 pos;
    private int samplingNum,visibleNum,innerNum=0;

    #region 可见性判断
 
    public bool IsVisableInCamera
    {
        get
        {
            //转化为视角坐标
            Vector3 viewPos = mCamera.WorldToViewportPoint(pos);
            // z<0代表在相机背后
            if (viewPos.z < 0) return false;
            //太远了！看不到了！
            if (viewPos.z > mCamera.farClipPlane)
                return false;
            // x,y取值在 0~1之外时代表在视角范围外;
            if (viewPos.x < 0 || viewPos.y < 0 || viewPos.x > 1 || viewPos.y > 1) return false;
            return true;
        }
    }
    #endregion

    //采样的少量模型点
    private List<Vector3> samplingVertices = new List<Vector3>();
    //在范围内
    private List<bool> f = new List<bool>();
    //看到过
    private List<bool> v = new List<bool>();

    private int[,] zbuff = new int[zbuffsize,zbuffsize]; //初值为0

    private bool isVisible(Vector3 pos){
        //Debug.Log("farClipPlane is "+mCamera.farClipPlane.ToString());
        Vector3 viewPos = mCamera.WorldToViewportPoint(pos);
        Vector3 cameraPos = mCamera.transform.position;
        // z<0代表在相机背后
        if (viewPos.z < 0) return false;
        //太远了！看不到了！
        if (viewPos.z > mCamera.farClipPlane) //远截平面的值我们设为512 它是一个public float
            return false;

        if(Math.Abs(pos.x-cameraPos.x)<50 && Math.Abs(pos.y-cameraPos.y)<50 && Math.Abs(pos.z-cameraPos.z)<50){
            // x,y取值在 0~1之外时代表在视角范围外;
            if (viewPos.x < 0 || viewPos.y < 0 || viewPos.x > 1 || viewPos.y > 1) return false;

        }
        return true;
    }

    private void isVisible(Vector3 pos,int id){
        //Debug.Log("farClipPlane is "+mCamera.farClipPlane.ToString());
        Vector3 viewPos = mCamera.WorldToViewportPoint(pos);
        Vector3 cameraPos = mCamera.transform.position;
        // z<0代表在相机背后
        if (viewPos.z < 0) return;
        //太远了！看不到了！ 视口空间是标准化的、相对于摄像机的空间。摄像机左下角为 (0,0)，右上角为 (1,1)。z 为位置与摄像机的距离，采用世界单位。
        if (viewPos.z > mCamera.farClipPlane) //远截平面的值我们设为512 它是一个public float ? z 不属于[0,1]
            return;

        if(Math.Abs(pos.x-cameraPos.x)<50 && Math.Abs(pos.y-cameraPos.y)<50 && Math.Abs(pos.z-cameraPos.z)<50){

            if(!f[id]) {
                innerNum++;
                f[id]=true;
            }

            // x,y取值在 0~1之外时代表在视角范围外;
            if (viewPos.x < 0 || viewPos.y < 0 || viewPos.x > 1 || viewPos.y > 1) return;
            //因为在执行退镜,新点大部分在老点前面,不会被遮挡,除非拐角处 ,判断褶皱处的遮挡关系时,就使用自建的深度图。

            int ix = (int) (viewPos.x*zbuffsize);
            int iy = (int) (viewPos.y*zbuffsize);
            
            Debug.Log("z "+ix+" , "+iy+" = "+zbuff[ix,iy]);
            if(zbuff[ix,iy]==0 || viewPos.z+1 <= zbuff[ix,iy]){ //zbuff 没有值 或者 viewPos距离摄像机更近
                visibleNum++;
                v[id]=true;
                zbuff[ix,iy] = (int)viewPos.z;
            }

        }
    }

    
    void Start()
    {
        // sharedmesh 通过meshfilter取得资源中的mesh网格,使用<meshfilter>.mesh会复制整个mesh
        Vector3[] vertices = colon.GetComponent<MeshFilter>().sharedMesh.vertices; 
        
        int totalPoint=vertices.Length;
        if(vertices != null) Debug.Log("get meshCollider, vertices count: "+totalPoint);   
        
        for(int i=0;i<totalPoint;i+=128){
            samplingVertices.Add(colon.transform.TransformPoint(vertices[i])); //将 vertices坐标 从本地空间变换到世界空间。
            f.Add(false);
            v.Add(false);
        }
        samplingNum = samplingVertices.Count;
    }

    // Update is called once per frame
    void Update()
    {
        frameCount++;
        if(frameCount>10){
            frameCount=0;
            for(int i=0;i<zbuffsize;i++)
                for(int j=0; j<zbuffsize;j++)
                    zbuff[i,j]=0;
        }
        //f[i]和 v[i] 都为真的点,是路径上已经见过的点,永远都不会再检查
        //但是每一帧都需要更新深度图
        for(int i=0;i<samplingNum;i++){
            if(!f[i]) {//不在相机附近
                isVisible(samplingVertices[i],i); //检查其进入相机附近没有
            } else if(!v[i]){  //在相机附近,不在视野中
                isVisible(samplingVertices[i],i); //检查其进入相机视野没有
            }
        }
        //pos=al[i];
        //Debug.Log(i.ToString()+IsVisableInCamera);
    }

    //游戏进入后台时执行该方法 pause为true 切换回前台时pause为false
    void OnApplicationPause(bool pause)
    {
        if(pause){
            Debug.Log(visibleNum);
        } else{
            Debug.Log("I'm back!");
        }
    }

    void OnApplicationQuit()
    {
        double coverageRate = (double)visibleNum/innerNum;

        Debug.Log("总采样点数："+samplingNum);
        Debug.Log("观察到的顶点数: " + visibleNum + ",  相机路程中的顶点数: " + innerNum + ",  test: "+isVisible(testPos.position));
        Debug.Log("the coverage is "+coverageRate.ToString("P2")); //P表示百分数,后面跟数字表示精度
    }
}
