using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class moveLine : MonoBehaviour
{
    //通过添加[Serializable]特性确保当前类可以被实例化。
    [Serializable]
    //创建一个类获取移动的路径点，移动时间，等待时间
    public class Path {
        public Transform Poitn;//路径点
        public float MoveTime;//移动时间
        public Vector3 Speed;//移动速度
    }
    public Path[] path = new Path[0];
    private int Id;
    public Transform target;

    // Start is called before the first frame update
    void Start () {
        //让移动的物体的位置更变为第一个点的位置
        target.position = path[0].Poitn.position;
        //计算出每两个点之间的速度-speed
        for (int i = 1;i<path.Length;i++) {
            path[i].Speed = (path[i].Poitn.position - path[i - 1].Poitn.position) / path[i].MoveTime;
        }
    }


    // Update is called once per frame
    void Update () {
        if (Id<path.Length ) {
            Path p = path[Id];
            //当移动的时间大于0时让物体向下一个点移动
            if (p.MoveTime > 0)
            {
                p.MoveTime -= Time.deltaTime;
                target.position += p.Speed * Time.deltaTime;
            }
            else{
                //当等待的时间大于0时，物体停止不动等待时间归零
                target.position = p.Poitn.position;
                Id++;
                
            }
        }
	}
    //在每两个点之间画出一条线（便于观察，不是必要的，去除对功能无影响）
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (path[i].Poitn && path[i + 1].Poitn)
            {
                Gizmos.DrawLine(path[i].Poitn.position, path[i + 1].Poitn.position);
            }
        }
    }
}

