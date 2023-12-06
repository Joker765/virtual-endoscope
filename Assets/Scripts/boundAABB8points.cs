using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boundAABB8points : MonoBehaviour
{
public List<Transform> TargetTransformList; //目标对象 列表。扔进去几个，生成几个


    void Start()
    {
        TargetTransformList.ForEach(GetBound8Vertex);
    }


    /// <summary>
    /// 获取8个顶点
    /// </summary>
    private void GetBound8Vertex(Transform targetTransform)
    {
        var           targetBoundsExtents = targetTransform.GetComponent<MeshFilter>().mesh.bounds.extents; //得到目标物包围盒范围：extents
        List<Vector3> vertex8List         = new List<Vector3>();                                            //来一个空数组，准备装数据。
        float         x                   = targetBoundsExtents.x;                                          //范围这里是三维向量，分别取得X Y Z
        float         y                   = targetBoundsExtents.y;
        float         z                   = targetBoundsExtents.z;
        /*
         * 点出8个点的位置信息，加载数组中备用
         * 理解诀窍是：
         * 1：保证X为正 yz只有4种可能
         * 2：同理X为负 yz 同理
         */
        vertex8List.Add(new Vector3(x,  y,  z));
        vertex8List.Add(new Vector3(x,  -y, z));
        vertex8List.Add(new Vector3(x,  y,  -z));
        vertex8List.Add(new Vector3(x,  -y, -z));
        vertex8List.Add(new Vector3(-x, y,  z));
        vertex8List.Add(new Vector3(-x, -y, z));
        vertex8List.Add(new Vector3(-x, y,  -z));
        vertex8List.Add(new Vector3(-x, -y, -z));
        //我们已经得到了8个点。
        //来实例化 预设物到顶点，来帮助理解。
        foreach (var t in vertex8List)
        {
            print(t);
            Instantiate(Resources.Load<Transform>("Chinar-Gauge Point"), t + targetTransform.position, Quaternion.identity);
        }

        vertex8List.Clear();
    }

}
