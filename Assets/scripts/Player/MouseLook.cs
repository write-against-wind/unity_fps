using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Tooltip("视野灵敏度")]public float mouseSenstivity = 400f;
    private Transform playerBody;//玩家位置
    private float yRotation = 0f;//摄像机上下旋转的数值
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;//锁定鼠标
        playerBody = transform.GetComponentInParent<PlayerController>().transform;//获取玩家位置
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSenstivity * Time.deltaTime;//获取鼠标移动的数值
        float mouseY = Input.GetAxis("Mouse Y") * mouseSenstivity * Time.deltaTime;//获取鼠标移动的数值
        yRotation -= mouseY;//摄像机上下旋转的数值
        yRotation = Mathf.Clamp(yRotation, -60f, 60f);//限制摄像机上下旋转的数值
        transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);//设置摄像机上下旋转的数值
        playerBody.Rotate(Vector3.up * mouseX);//设置玩家左右旋转的数值
    }
}
