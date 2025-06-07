using UnityEngine;
using Unity.XR.CoreUtils;

public class VRHeadRotationSync : MonoBehaviour
{
    [Header("旋转设置")]
    public Transform playerCharacter; // 人物角色模型
    public bool syncYRotation = true; // 是否同步Y轴旋转（水平旋转）
    public bool syncXRotation = false; // 是否同步X轴旋转（点头）
    public bool syncZRotation = false; // 是否同步Z轴旋转（侧倾）
    
    [Header("平滑设置")]
    public bool useSmoothing = true;
    public float rotationSmoothSpeed = 5f;
    
    [Header("旋转限制")]
    public bool limitXRotation = true;
    public float minXRotation = -30f; // 最小俯仰角
    public float maxXRotation = 30f;  // 最大俯仰角
    
    [Header("旋转偏移")]
    public Vector3 rotationOffset = Vector3.zero; // 旋转偏移量
    
    private XROrigin xrOrigin;
    private Camera xrCamera;
    private Vector3 targetRotation;
    
    private void Start()
    {
        // 获取XR Origin和相机
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            xrCamera = xrOrigin.Camera;
        }
        
        if (xrCamera == null)
        {
            Debug.LogError("未找到XR相机！请确保场景中有XR Origin。");
            return;
        }
        
        if (playerCharacter == null)
        {
            Debug.LogWarning("未设置人物角色！请在Inspector中设置playerCharacter。");
        }
        
        // 初始化目标旋转
        if (playerCharacter != null)
        {
            targetRotation = playerCharacter.eulerAngles;
        }
    }
    
    private void Update()
    {
        if (xrCamera == null || playerCharacter == null) return;
        
        // 获取头显的旋转
        Vector3 headRotation = xrCamera.transform.eulerAngles;
        
        // 构建目标旋转
        Vector3 newRotation = targetRotation;
        
        if (syncYRotation)
        {
            newRotation.y = headRotation.y + rotationOffset.y;
        }
        
        if (syncXRotation)
        {
            float xRot = headRotation.x + rotationOffset.x;
            
            // 处理角度范围（Unity的角度是0-360）
            if (xRot > 180f) xRot -= 360f;
            
            // 应用限制
            if (limitXRotation)
            {
                xRot = Mathf.Clamp(xRot, minXRotation, maxXRotation);
            }
            
            newRotation.x = xRot;
        }
        
        if (syncZRotation)
        {
            float zRot = headRotation.z + rotationOffset.z;
            if (zRot > 180f) zRot -= 360f;
            newRotation.z = zRot;
        }
        
        targetRotation = newRotation;
        
        // 应用旋转
        if (useSmoothing)
        {
            // 平滑旋转
            playerCharacter.rotation = Quaternion.Slerp(
                playerCharacter.rotation,
                Quaternion.Euler(targetRotation),
                Time.deltaTime * rotationSmoothSpeed
            );
        }
        else
        {
            // 直接旋转
            playerCharacter.rotation = Quaternion.Euler(targetRotation);
        }
    }
    
    // 用于调试的方法
    private void OnDrawGizmos()
    {
        if (xrCamera != null && playerCharacter != null)
        {
            // 绘制头显朝向
            Gizmos.color = Color.red;
            Gizmos.DrawLine(xrCamera.transform.position, 
                           xrCamera.transform.position + xrCamera.transform.forward * 2f);
            
            // 绘制人物朝向
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(playerCharacter.position, 
                           playerCharacter.position + playerCharacter.forward * 2f);
        }
    }
} 