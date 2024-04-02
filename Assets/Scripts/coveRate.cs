using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//sing System.Json;
using LitJson;
using System.IO; //StreamWriter

public class coveRate : MonoBehaviour
{
    public static int zbuffsize = 512;
    public static int Far = 200;
    private int frameCount = 0;
    //public Transform testPos; // 测试某点是否可见

    [SerializeField]//定义此属性在Inspector中显示并可序列化
    private GameObject colon;
    [SerializeField]
    private Camera mCamera;
    private Vector3 pos;
    private int samplingNum,visibleNum=0,innerNum=0;

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

    public class Vector3B //LitJson不支持Vector3的float类型
    {
        public double bx { get; set; }
        public double by { get; set; }
        public double bz { get; set; }
    }

    //采样的少量模型点
    private List<Vector3> samplingVertices = new List<Vector3>();
    //可以被看到的点（用于测试）
    private List<Vector3B> visibleVertices = new List<Vector3B>();
    private List<Vector3B> invisibleVertices = new List<Vector3B>();
    //在范围内
    private List<bool> f = new List<bool>();
    //看到过
    private List<bool> v = new List<bool>();

    //深度图
    private float[,] zbuff = new float[zbuffsize,zbuffsize]; //初值为0
    //深度图中存储的点的id
    private int[,] zf = new int[zbuffsize,zbuffsize];

    //public Script fpb; 获取其他脚本类的错误方式
    //public GameObject fpb;
    public fourPointsBezier fpb; //直接拖拽挂载此脚本的游戏物体进行赋值

    public GameObject greenPrefab;
    public GameObject redPrefab;

    /// 從json中讀取坐標信息
    void LoadJson() 
    {
        //第一步 读取路径
        StreamReader sr = new StreamReader(Application.dataPath + "/Resources/visibleVertices.txt");
        string jsondata = sr.ReadLine();
        sr.Close();
        //第二步: 将json文本转换成对象
        visibleVertices = JsonMapper.ToObject<List<Vector3B>>(jsondata);

        StreamReader sr1 = new StreamReader(Application.dataPath + "/Resources/invisibleVertices.txt");
        string jsondata1 =sr1.ReadLine();
        sr1.Close();
        invisibleVertices = JsonMapper.ToObject<List<Vector3B>>(jsondata1);
    }

    void Save()
    {
        //轉json
        string json = JsonMapper.ToJson(visibleVertices);
        //寫入路徑
        string path = Application.dataPath + "/Resources/visibleVertices.txt";
        //生成文件寫入json
        StreamWriter sw = new StreamWriter(path,false); //第二个参数append为false表示不追加,而是改写文件。会先清空文件再写,而不是只改写前面的几行保留后面的
        sw.Write(json);
        //第四步：关闭（此步必不可少）
        sw.Close();

        json = JsonMapper.ToJson(invisibleVertices);
        path = Application.dataPath + "/Resources/invisibleVertices.txt";
        StreamWriter sw1 = new StreamWriter(path,false);
        sw1.Write(json);
        sw1.Close();
    }

    void ShowVertices()
    {
        LoadJson();
        
        for(int i=0;i<visibleVertices.Count;i++){
            Vector3 pos = new Vector3();
            pos.x=(float)visibleVertices[i].bx;
            pos.y=(float)visibleVertices[i].by;
            pos.z=(float)visibleVertices[i].bz;
            Instantiate(greenPrefab,pos,Quaternion.identity); //使用实例化预制体的方法创建对象
            //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.transform.position = new Vector3(0, 0.5f, 0);
        }

        for(int i=0;i<invisibleVertices.Count;i++){
            Vector3 pos = new Vector3();
            pos.x=(float)invisibleVertices[i].bx;
            pos.y=(float)invisibleVertices[i].by;
            pos.z=(float)invisibleVertices[i].bz;
            Instantiate(redPrefab,pos,Quaternion.identity); //使用实例化预制体的方法创建对象
            //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.transform.position = new Vector3(0, 0.5f, 0);
        }
    }

    /*
    pos 遍历的某点的位置
    */
    private void isVisible(Vector3 pos,int id){
        //Debug.Log("farClipPlane is "+mCamera.farClipPlane.ToString());
        Vector3 viewPos = mCamera.WorldToViewportPoint(pos);
        //Debug.Log(id+": "+viewPos.x);
        Vector3 cameraPos = mCamera.transform.position;
        // z<0代表在相机背后
        if (viewPos.z < 0) return;
        //太远了！看不到了！ 视口空间是标准化的、相对于摄像机的空间。摄像机左下角为 (0,0)，右上角为 (1,1)。z 为位置与摄像机的距离，采用世界单位。
        if (viewPos.z > 100) //Far = mCamera.farClipPlane 远截平面的值我们设为256 它是一个public float     z 不属于[0,1],确实,100多的数都有
            return;

        if(Math.Abs(pos.x-cameraPos.x)<35 && Math.Abs(pos.y-cameraPos.y)<35 && Math.Abs(pos.z-cameraPos.z)<35){ //在范围内
            if(!f[id]) {
                innerNum++;
                f[id]=true;
                //Debug.Log(viewPos);
            }
            // x,y取值在 0~1之外时代表在视角范围外;

            if (viewPos.x < -0.2F || viewPos.y < -0.2F || viewPos.x > 1.2F || viewPos.y > 1.2F) return;
            if (viewPos.x < 0.0F || viewPos.y < 0.0F || viewPos.x > 1.0F || viewPos.y > 1.0F) {
                v[id]=true;//因为编辑器实际显示的画面比摄像机视锥体空间大一点,对画面边缘的赋值不影响中心重叠的判定
                return;
            }
            //因为在执行退镜,新点大部分在老点前面,不会被遮挡,除非拐角处 ,判断褶皱处的遮挡关系时,就使用自建的深度图。
            int ix = (int) (viewPos.x*zbuffsize);
            int iy = (int) (viewPos.y*zbuffsize);
            //Debug.Log("z["+ix+", "+iy+"] = "+zbuff[ix,iy]);
            // if(zbuff[ix,iy]==0) { //zbuff 没有值 
            //     if(v[id]==false){ // 点id第一次被看到 
            //         visibleNum++;
            //         v[id]=true;
            //     }
            //     zbuff[ix,iy] = (int)viewPos.z;
            //} else 
            if(viewPos.z < zbuff[ix,iy]){  //点id 距离摄像机更近 或者 zbuff 没有值 
                zbuff[ix,iy] = viewPos.z;
                zf[ix,iy]=id;
            }
        }
    }

    
    void Start()
    {
        // sharedmesh 通过meshfilter取得资源中的mesh网格,使用<meshfilter>.mesh会复制整个mesh
        Vector3[] vertices = colon.GetComponent<MeshFilter>().sharedMesh.vertices; 
        
        int totalPoint=vertices.Length;
        if(vertices != null) Debug.Log("get meshCollider success, vertices count is: "+totalPoint);   
        
        f.Clear();v.Clear();
        for(int i=0;i<totalPoint;i+=16){
            samplingVertices.Add(colon.transform.TransformPoint(vertices[i])); //将 vertices坐标 从本地空间变换到世界空间。
            f.Add(false);
            v.Add(false);
        }
        v.Add(false);// v数组多一个 id==samplingNum
        samplingNum = samplingVertices.Count;

        //初始化帧计数器
        frameCount=0;
        //初始化顶点个数统计
        innerNum=0;
        //初始化深度图
        for(int i=0;i<zbuffsize;i++)
            for(int j=0; j<zbuffsize;j++){
                zbuff[i,j]=Far+1;
                zf[i,j]=samplingNum;
            }
        //ShowVertices();
    }

    // Update is called once per frame
    void Update()
    {
        
        frameCount++;
        if(frameCount>2){
            frameCount=0;
            for(int i=0;i<zbuffsize;i++)
                for(int j=0; j<zbuffsize;j++){
                    zbuff[i,j]=Far+1;
                    
                    v[zf[i,j]]=true;
                    zf[i,j]=samplingNum;
                }
            for(int i=0;i<samplingNum;i++){
                // if(!f[i]) {//不在相机附近
                //     isVisible(samplingVertices[i],i); //检查其进入相机附近、进入视野没有
                // } else if(!v[i]){  //在相机附近,不在视野中
                //     isVisible(samplingVertices[i],i); //检查其进入相机视野没有
                // } else { //f[i]和 v[i] 都为真的点, 需要更新深度图
                //     isVisible(samplingVertices[i],i); //因为没有判断点 出视野 f=false,所以也需要经过整个isVisible()流程
                // }
                isVisible(samplingVertices[i],i);//v[i]=true也要搞,更新深度图
            }
        }
        //f[i]和 v[i] 都为真的点,是路径上已经见过的点,永远都不会再检查        //但是每一帧都需要更新深度图
        // for(int i=0;i<samplingNum;i++){
        //     if(!f[i]) {//不在相机附近
        //         isVisible(samplingVertices[i],i); //检查其进入相机附近没有
        //     } else if(!v[i]){  //在相机附近,不在视野中
        //         isVisible(samplingVertices[i],i); //检查其进入相机视野没有
        //     }
        // }
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
        Vector3 preWay = fpb.GetPreWay();
        //Debug.Log("前一个位置："+preWay);
        Vector3 cameraPos = mCamera.transform.position;
        Vector3 pos =new Vector3(0,0,0);
        int finalBoxNum=0;
        
        //初始化可见点
        visibleNum=0;
        visibleVertices.Clear();
        invisibleVertices.Clear();

        for(int i=0;i<samplingNum;i++) {
            pos = samplingVertices[i];
            Vector3B posB=new Vector3B();
            posB.bx=pos.x;
            posB.by=pos.y;
            posB.bz=pos.z;
            if(v[i]) {
                visibleNum++;  //统计看见点个数
                visibleVertices.Add(posB);
            }else if(f[i]){ //在应该被看见的范围内,又没有看见的点
                invisibleVertices.Add(posB);
            }
            
            if(Math.Abs(pos.x-cameraPos.x)<30 && Math.Abs(pos.y-cameraPos.y)<30 && Math.Abs(pos.z-cameraPos.z)<30){
                finalBoxNum++;
            }
        }
        Save();
        //Debug.Log("innerNum:"+innerNum+",  finalBoxNum: "+finalBoxNum);
        if(preWay==cameraPos) {
            innerNum = innerNum- finalBoxNum/2;
        } else innerNum = innerNum- finalBoxNum/2;

        double coverageRate = (double)visibleNum/innerNum;

        Debug.Log("总采样点数："+samplingNum);
        Debug.Log("观察到的顶点数: " + visibleNum + ",  相机路程中的顶点数: " + innerNum);// + ",  test: "+isVisible(testPos.position));
        Debug.Log("the coverage is "+coverageRate.ToString("P2")); //P表示百分数,后面跟数字表示精度
    }
}
