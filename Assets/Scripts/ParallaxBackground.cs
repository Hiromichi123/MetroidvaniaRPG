using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private GameObject cam;
    [SerializeField] private float parallaxEffect; // 视差强度0-1
    private float xPosition; // 背景初始位置
    private float length; // 单幅背景长度
    void Start()
    {
        cam = GameObject.Find("Main Camera"); // 绑定到主摄像机

        xPosition = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x; // 获取背景长度
    }

    void Update()
    {
        float distanceToMove = cam.transform.position.x * parallaxEffect; // 背景需要移动的距离
        transform.position = new Vector3(xPosition + distanceToMove, transform.position.y);

        float distanceMoved = cam.transform.position.x * (1 - parallaxEffect); // 背景已经移动的距离

        if (distanceMoved > xPosition + length) // 向右移动背景
        {
            xPosition += length;

        }
        else if (distanceMoved < xPosition - length) // 向左移动背景
        {
            xPosition -= length;
        }
    }
}
