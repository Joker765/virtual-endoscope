using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class userController : MonoBehaviour
{
    private Rigidbody rb;
    public float rotateSpeed=32f;
    public float translateSpeed=8f;

    // Start is called before the first frame update
    void Start()
    {
        rb=GetComponent<Rigidbody>();
        if (rb==null) print("haha");
        else print("get rigidbody success!");
    }


    void FixedUpdate()//固定时间间隔更新
    {
        if (Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))
        {

            this.transform.Rotate(0,-rotateSpeed*Time.deltaTime,0,Space.Self);
        }
        if (Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))
        {

            this.transform.Rotate(0,rotateSpeed*Time.deltaTime,0,Space.Self);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {

            this.transform.Translate(Vector3.forward * Time.deltaTime * translateSpeed, Space.Self);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.Translate(-Vector3.forward * Time.deltaTime * translateSpeed, Space.Self);
        }

        if (Input.GetKey(KeyCode.W))
        {
            this.transform.Rotate(-rotateSpeed*Time.deltaTime,0,0,Space.Self);
        }
        if (Input.GetKey(KeyCode.S))
        {
            this.transform.Rotate(rotateSpeed*Time.deltaTime,0,0,Space.Self);
        }


    }
}
