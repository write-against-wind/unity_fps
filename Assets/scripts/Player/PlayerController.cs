using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // 添加UI命名空间引用
using UnityEngine.XR;  // 添加XR命名空间
using UnityEngine.XR.Interaction.Toolkit;  // 添加XR Interaction Toolkit命名空间

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    public Vector3 moveDirection;
    private AudioSource audioSource;

    private Inventory inventory;

    [Header("玩家数值")]
    [Tooltip("移动速度")]public float walkSpeed;
    [Tooltip("奔跑速度")]public float runSpeed;
    [Tooltip("下蹲行走速度")]public float crouchSpeed;
    [Tooltip("玩家血量")]public float playerHealth;

    [Tooltip("跳跃力")]public float jumpForce;
    [Tooltip("重力")]public float fallForce;
    public float Speed;


    [Header("玩家按键")]
    [Tooltip("奔跑按键")]public KeyCode runKey = KeyCode.LeftShift;
    [Tooltip("下蹲按键")]public KeyCode crouchKey = KeyCode.LeftControl;
    [Tooltip("跳跃按键")]public KeyCode jumpKey = KeyCode.Space;


    [Header("玩家状态")]
    public PlayerState playerState;
    // private CollisionFlags collisionFlags;
    [Tooltip("是否在奔跑")]public bool isRunning;
    [Tooltip("是否在下蹲")]public bool isCrouching;
    [Tooltip("是否在跳跃")]public bool isJumping;
    [Tooltip("是否在行走")]public bool isWalk;
    [Tooltip("是否在地面")]public bool isGround = true;
    [Tooltip("是否死亡")]private bool playerisDead;
    [Tooltip("是否收到伤害")]private bool isDamage;
    // 垂直速度
    private float verticalVelocity = 0f;
    public Text playerHealthUI;

    [Header("音效")]
    [Tooltip("行走声")]public AudioClip walkSound;
    [Tooltip("奔跑声")]public AudioClip runningSound;


    [Header("下蹲设置")]
    [Tooltip("站立时的高度")]public float standingHeight = 2.0f;
    [Tooltip("下蹲时的高度")]public float crouchHeight = 1.0f;
    [Tooltip("下蹲速度")]public float crouchSpeed_Lerp = 10f;
    [Tooltip("相机位置")]public Transform playerCamera;
    [Tooltip("站立时相机Y位置")]public float standingCameraY = 1.4f;
    [Tooltip("下蹲时相机Y位置")]public float crouchingCameraY = 0.8f;
    [Tooltip("检测头顶的距离")]public float headCheckDistance = 0.1f;
    
    // 记录原始高度和中心点
    private float originalHeight;
    private Vector3 originalCenter;
    private bool canStand = true;
    
    [Header("VR控制器")]
    [Tooltip("左手控制器")] public XRController leftController;
    [Tooltip("右手控制器")] public XRController rightController;
    
    // VR按钮状态跟踪（避免连续触发）
    private bool wasLeftAButtonPressed = false;
    private bool wasRightXButtonPressed = false;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        inventory = GetComponentInChildren<Inventory>();
        audioSource = GetComponent<AudioSource>();
        walkSpeed = 4f;
        runSpeed = 6f;
        crouchSpeed = 2f;
        fallForce = 20f;  // 设置重力值
        jumpForce = 8f;   // 设置跳跃力
        playerHealth = 100f;
        // 保存原始高度和中心点
        originalHeight = characterController.height;
        originalCenter = characterController.center;
        
        // 确保站立高度与角色控制器初始高度匹配
        standingHeight = originalHeight;
        
        // 如果没有指定相机，尝试在子对象中查找
        if (playerCamera == null)
        {
            Camera mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera != null)
            {
                playerCamera = mainCamera.transform;
            }
        }
        
        // 自动查找VR控制器（如果没有手动设置）
        if (leftController == null || rightController == null)
        {
            Debug.Log("开始自动查找VR控制器...");
            
            // 方法1：通过XRController组件查找
            XRController[] controllers = FindObjectsOfType<XRController>();
            Debug.Log($"找到的XRController组件数量: {controllers.Length}");
            
            foreach (XRController controller in controllers)
            {
                Debug.Log($"检查控制器: {controller.name}, Node: {controller.controllerNode}");
                if (controller.controllerNode == XRNode.LeftHand && leftController == null)
                {
                    leftController = controller;
                    Debug.Log("找到左手控制器: " + controller.name);
                }
                else if (controller.controllerNode == XRNode.RightHand && rightController == null)
                {
                    rightController = controller;
                    Debug.Log("找到右手控制器: " + controller.name);
                }
            }
            
            // 方法2：如果没找到XRController组件，直接使用InputDevice
            if (leftController == null || rightController == null)
            {
                Debug.Log("未找到XRController组件，尝试直接使用InputDevice...");
                StartCoroutine(FindVRDevices());
            }
        }
        
        playerHealthUI.text = "生命值：" + playerHealth;
    }

    // Update is called once per frame
    void Update()
    {
        Jump();
        ApplyGravity();
        Crouch();
        PlayerFootSoundSet();
        Moving();
    }

    // 应用重力
    public void ApplyGravity()
    {
        // 检查是否在地面上
        isGround = characterController.isGrounded;
        
        // 应用重力
        if (isGround && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // 确保角色贴紧地面
        }
        else
        {
            verticalVelocity -= fallForce * Time.deltaTime; // 应用重力
        }
    }
    
    // 下蹲功能
    public void Crouch()
    {
        // 检查头顶是否有障碍物
        canStand = !CheckHeadObstacle();
        
        // 获取VR控制器下蹲输入 - 左手A键 - 添加调试信息
        bool leftAButtonPressed = false;
        if (leftController != null)
        {
            // 尝试不同的按钮映射
            bool primaryButton = false;
            bool secondaryButton = false;
            
            leftController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);
            leftController.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);
            
            leftAButtonPressed = primaryButton;
            
            // 调试信息
            if (primaryButton || secondaryButton)
            {
                Debug.Log($"左手按钮状态 - Primary(X): {primaryButton}, Secondary(Y): {secondaryButton}");
            }
        }
        
        // 检测下蹲输入
        if (leftAButtonPressed && isGround && !isJumping)
        {
            isCrouching = true;
            playerState = PlayerState.Crouching;
        }
        else if (canStand && !leftAButtonPressed)
        {
            // 没有障碍物且没有按下下蹲键，恢复站立
            isCrouching = false;
            if (isWalk)
            {
                playerState = isRunning ? PlayerState.Running : PlayerState.Walking;
            }
            else
            {
                playerState = PlayerState.Idle;
            }
        }
        
        // 应用下蹲效果
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        characterController.height = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchSpeed_Lerp);
        
        // 设置控制器中心位置，确保角色始终站在地面上
        Vector3 center = originalCenter;
        center.y = originalCenter.y - (originalHeight - characterController.height) / 2;
        characterController.center = center;
        
        // 调整相机位置
        if (playerCamera != null)
        {
            Vector3 cameraPos = playerCamera.localPosition;
            float targetCameraY = isCrouching ? crouchingCameraY : standingCameraY;
            cameraPos.y = Mathf.Lerp(cameraPos.y, targetCameraY, Time.deltaTime * crouchSpeed_Lerp);
            playerCamera.localPosition = cameraPos;
        }
    }
    
    // 播放音效
    public void PlayerFootSoundSet(){
        // 根据不同状态播放不同的脚步声
        if (isWalk && isGround)
        {
            if (isRunning && !isCrouching)
            {
                // 播放奔跑音效
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = runningSound;
                    audioSource.Play();
                }
            }
            else if (!isCrouching)
            {
                // 播放行走音效
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = walkSound;
                    audioSource.Play(); 
                }
            }
        }
        else
        {
            // 停止移动时停止音效
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

    }
    
    // 检查头顶是否有障碍物
    private bool CheckHeadObstacle()
    {
        // 计算检测起点和距离
        Vector3 start = transform.position + characterController.center;
        float checkDistance = standingHeight - characterController.height + headCheckDistance;
        
        // 只有在下蹲状态才进行检测
        if (!isCrouching || checkDistance <= 0)
            return false;
            
        // 进行射线检测
        Ray ray = new Ray(start, Vector3.up);
        RaycastHit hit;
        
        // 绘制调试射线
        Debug.DrawRay(start, Vector3.up * checkDistance, Color.red);
        
        // 检测并返回结果
        return Physics.Raycast(ray, out hit, checkDistance);
    }
    
    public void Moving(){
        // 获取左手摇杆输入
        Vector2 leftStickInput = Vector2.zero;
        if (leftController != null)
        {
            leftController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStickInput);
        }

        float moveX = leftStickInput.x;
        float moveZ = leftStickInput.y;
        
        // 用右手控制器Y键控制奔跑 - 添加调试信息
        bool runButtonPressed = false;
        if (rightController != null)
        {
            // 尝试不同的按钮映射
            bool primaryButton = false;
            bool secondaryButton = false;
            
            rightController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);
            rightController.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);
            
            runButtonPressed = primaryButton;
            
            // 调试信息
            if (primaryButton || secondaryButton)
            {
                Debug.Log($"右手按钮状态 - Primary(A): {primaryButton}, Secondary(B): {secondaryButton}");
            }
        }
        isRunning = runButtonPressed;
        
        isWalk = (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f) ? true : false;
        
        // 根据状态设置速度
        if(isCrouching && isWalk)
        {
            playerState = PlayerState.Crouching;
            Speed = crouchSpeed;
        }
        else if(isRunning && isWalk && !isCrouching)
        {
            playerState = PlayerState.Running;
            Speed = runSpeed;
        }
        else if(isWalk && !isCrouching)
        {
            playerState = PlayerState.Walking;
            Speed = walkSpeed;
        }
        else if(!isWalk && !isJumping)
        {
            playerState = isCrouching ? PlayerState.Crouching : PlayerState.Idle;
        }
        
        //设置人物移动方向
        moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        // 应用水平移动
        Vector3 horizontalMove = moveDirection * Speed * Time.deltaTime;
        
        // 应用垂直移动
        Vector3 verticalMove = new Vector3(0, verticalVelocity * Time.deltaTime, 0);
        //移动
        characterController.Move(horizontalMove + verticalMove);
    }
    
    public void Jump(){
        // 获取VR控制器跳跃输入 - 右手X键 - 添加调试信息
        bool rightXButtonPressed = false;
        if (rightController != null)
        {
            // 尝试不同的按钮映射
            bool primaryButton = false;
            bool secondaryButton = false;
            
            rightController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);
            rightController.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);
            
            rightXButtonPressed = secondaryButton; // 使用secondary作为X键
            
            // 调试信息
            if (primaryButton || secondaryButton)
            {
                Debug.Log($"右手跳跃按钮状态 - Primary(A): {primaryButton}, Secondary(B): {secondaryButton}");
            }
        }
        
        // 检测跳跃输入 - 下蹲时不能跳跃
        if(rightXButtonPressed && !wasRightXButtonPressed && isGround && !isCrouching){
            // 设置跳跃状态
            playerState = PlayerState.Jumping;
            isJumping = true;
            
            // 应用跳跃力
            verticalVelocity = jumpForce;
            
            Debug.Log("跳跃触发！");
        }
        
        // 更新按钮状态
        wasRightXButtonPressed = rightXButtonPressed;
        
        // 如果不在地面上，说明可能在跳跃或下落
        if(!isGround){
            isJumping = true;
        }
        else{
            // 着地后重置跳跃状态
            if(isJumping){
                isJumping = false;
                if(!isWalk){
                    playerState = PlayerState.Idle;
                }
            }
        }
    }
    public void PickUpWeapon(int itemID,GameObject weapon){
        if(inventory.weapons.Contains(weapon)){
            weapon.GetComponent<Weapon_AutomaticGun>().bulletLeft = weapon.GetComponent<Weapon_AutomaticGun>().bulletMag * 5;
            weapon.GetComponent<Weapon_AutomaticGun>().UpdateAmmoUI();
            Debug.Log("背包中已存在该武器，补充子弹。");
            return;
        }
        Debug.Log("捡起了武器。");
        inventory.AddWeapon(weapon);
    }
    public void PlayerHealth(float damage){
        playerHealth -= damage;
        isDamage = true;
        playerHealthUI.text="生命值："+playerHealth;
        if(playerHealth <=0){
            playerisDead =true;
            playerHealthUI.text="玩家死亡";
            Time.timeScale=0;
        }
    }
    public enum PlayerState{
        Idle,
        Walking,
        Running,
        Crouching,
        Jumping
    }

    // 添加协程来查找VR设备
    IEnumerator FindVRDevices()
    {
        yield return new WaitForSeconds(2f); // 等待VR系统初始化
        
        var leftDevices = new List<InputDevice>();
        var rightDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
        
        Debug.Log($"通过XRNode找到的左手设备数量: {leftDevices.Count}");
        Debug.Log($"通过XRNode找到的右手设备数量: {rightDevices.Count}");
        
        // 如果找到了设备但没有XRController组件，创建一个临时的引用
        if (leftDevices.Count > 0)
        {
            leftInputDevice = leftDevices[0];
            Debug.Log($"设置左手InputDevice: {leftInputDevice.name}");
        }
        
        if (rightDevices.Count > 0)
        {
            rightInputDevice = rightDevices[0];
            Debug.Log($"设置右手InputDevice: {rightInputDevice.name}");
        }
    }
    
    // 添加InputDevice变量作为备用
    private InputDevice leftInputDevice;
    private InputDevice rightInputDevice;
}
