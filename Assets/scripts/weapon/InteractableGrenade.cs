using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using System.Collections;

public class InteractableGrenade : MonoBehaviour
{
    [Header("手雷设置")]
    [Tooltip("爆炸倒计时（秒）")]
    public float explosionTimer = 4f;
    [Tooltip("爆炸半径")]
    public float explosionRadius = 10f;
    [Tooltip("爆炸力度")]
    public float explosionPower = 500f;
    [Tooltip("对敌人的伤害")]
    public float damageToEnemies = 75f;
    [Tooltip("对玩家的伤害")]
    public float damageToPlayer = 50f;
    
    [Header("爆炸预制体")]
    [Tooltip("爆炸特效预制体")]
    public Transform explosionPrefab;
    
    [Header("音效")]
    [Tooltip("撞击音效")]
    public AudioSource impactSound;
    [Tooltip("投掷音效")]
    public AudioClip throwSound;
    [Tooltip("倒计时滴答声")]
    public AudioClip tickSound;
    
    [Header("视觉提示")]
    [Tooltip("倒计时指示灯")]
    public Light indicatorLight;
    [Tooltip("倒计时时灯光颜色变化")]
    public Gradient lightColorGradient;
    
    [Header("调试")]
    public bool showDebugLogs = true;
    public bool showExplosionRadius = true;
    
    [Header("备用激活方式")]
    [Tooltip("是否启用按钮激活手雷（适用于XR模拟器）")]
    public bool enableButtonActivation = true;
    [Tooltip("用于激活手雷的按钮")]
    public InputHelpers.Button activationButton = InputHelpers.Button.SecondaryButton;
    
    // 组件引用
    private VRInteractableObject interactableObject;
    private AudioSource audioSource;
    private Rigidbody rb;
    private bool isArmed = false;
    private bool hasExploded = false;
    private float currentTimer = 0f;
    private float tickInterval = 1f;
    private float lastTickTime = 0f;
    
    // 投掷检测
    private bool wasGrabbed = false;
    private Vector3 lastVelocity;
    private float throwThreshold = 0.5f; // 投掷速度阈值（降低以便更容易触发）
    private XRBaseControllerInteractor currentInteractor; // 当前抓取的控制器
    
    // 备用投掷检测
    private Vector3 lastPosition;
    private float lastTime;
    private Vector3 calculatedVelocity;
    
    void Start()
    {
        // 获取组件
        interactableObject = GetComponent<VRInteractableObject>();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        
        // 确保有必要的组件
        if (interactableObject == null)
        {
            interactableObject = gameObject.AddComponent<VRInteractableObject>();
        }
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D音效
        }
        
        // 确保有撞击音效AudioSource
        if (impactSound == null)
        {
            impactSound = audioSource;
        }
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 设置手雷属性
        rb.mass = 0.5f;
        rb.drag = 0.3f;
        rb.angularDrag = 0.5f;
        
        // 添加随机旋转（参考GrenadeScript）
        rb.AddRelativeTorque(
            Random.Range(500, 1500), // X轴
            Random.Range(0, 0),      // Y轴 
            Random.Range(0, 0)       // Z轴
            * Time.deltaTime * 5000);
    }
    
    // 撞击音效（参考GrenadeScript）
    void OnCollisionEnter(Collision collision)
    {
        if (impactSound != null)
        {
            impactSound.Play();
        }
        
        // 订阅交互事件
        if (interactableObject != null)
        {
            interactableObject.selectEntered.AddListener(OnGrabbed);
            interactableObject.selectExited.AddListener(OnReleased);
        }
        
        // 初始化指示灯
        if (indicatorLight != null)
        {
            indicatorLight.enabled = false;
        }
        
        // 设置初始倒计时
        currentTimer = explosionTimer;
        
        if (showDebugLogs)
        {
            Debug.Log($"可交互手雷已初始化 - 爆炸倒计时: {explosionTimer}秒");
        }
    }
    
    void Update()
    {
        // 记录速度用于投掷检测
        if (rb != null)
        {
            lastVelocity = rb.velocity;
        }
        
        // 如果被抓取，计算手动速度
        if (wasGrabbed && currentInteractor != null)
        {
            Vector3 currentPosition = transform.position;
            float currentTime = Time.time;
            
            if (lastTime > 0)
            {
                float deltaTime = currentTime - lastTime;
                if (deltaTime > 0)
                {
                    calculatedVelocity = (currentPosition - lastPosition) / deltaTime;
                }
            }
            
            lastPosition = currentPosition;
            lastTime = currentTime;
            
            // 检查按钮激活（备用方案）
            if (enableButtonActivation && !isArmed)
            {
                CheckButtonActivation();
            }
        }
        
        // 如果手雷已激活，更新倒计时
        if (isArmed && !hasExploded)
        {
            UpdateTimer();
            UpdateIndicatorLight();
            UpdateTickSound();
        }
    }
    
    void OnGrabbed(SelectEnterEventArgs args)
    {
        wasGrabbed = true;
        
        // 保存抓取的控制器引用
        currentInteractor = args.interactorObject as XRBaseControllerInteractor;
        
        // 初始化位置追踪
        lastPosition = transform.position;
        lastTime = Time.time;
        calculatedVelocity = Vector3.zero;
        
        if (showDebugLogs)
        {
            Debug.Log("手雷被抓取");
            if (currentInteractor != null)
            {
                Debug.Log($"抓取控制器: {currentInteractor.name}");
            }
        }
    }
    
    void OnReleased(SelectExitEventArgs args)
    {
        if (wasGrabbed)
        {
            // 多种方法获取投掷速度
            Vector3 controllerVelocity = Vector3.zero;
            Vector3 finalVelocity = Vector3.zero;
            
            // 方法1: 从VR控制器获取速度
            bool gotControllerVelocity = false;
            if (currentInteractor != null)
            {
                // 尝试多种方式获取控制器速度
                InputDevice inputDevice = new InputDevice();
                
                // 方式1: 通过XRNode获取设备
                if (currentInteractor.name.Contains("Left"))
                {
                    inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                }
                else if (currentInteractor.name.Contains("Right"))
                {
                    inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                }
                
                // 尝试获取速度
                if (inputDevice.isValid && inputDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out controllerVelocity))
                {
                    finalVelocity = controllerVelocity;
                    gotControllerVelocity = true;
                    if (showDebugLogs) Debug.Log("使用控制器速度");
                }
                // 备用方案：直接从控制器组件获取
                else if (currentInteractor.transform != null)
                {
                    // 尝试从Rigidbody获取速度
                    Rigidbody controllerRb = currentInteractor.GetComponent<Rigidbody>();
                    if (controllerRb != null)
                    {
                        controllerVelocity = controllerRb.velocity;
                        finalVelocity = controllerVelocity;
                        gotControllerVelocity = true;
                        if (showDebugLogs) Debug.Log("使用控制器Rigidbody速度");
                    }
                }
            }
            
            if (gotControllerVelocity)
            {
                // 已经设置了finalVelocity
            }
            // 方法2: 使用手动计算的速度
            else if (calculatedVelocity.magnitude > 0.1f)
            {
                finalVelocity = calculatedVelocity;
                if (showDebugLogs) Debug.Log("使用计算速度");
            }
            // 方法3: 使用物体速度（最后备案）
            else
            {
                finalVelocity = lastVelocity;
                if (showDebugLogs) Debug.Log("使用物体速度");
            }
            
            float releaseSpeed = finalVelocity.magnitude;
            
            if (showDebugLogs)
            {
                Debug.Log($"释放速度检测: 控制器速度={controllerVelocity.magnitude:F2}, 计算速度={calculatedVelocity.magnitude:F2}, 物体速度={lastVelocity.magnitude:F2}, 最终速度={releaseSpeed:F2}");
            }
            
            if (releaseSpeed > throwThreshold)
            {
                // 这是一个投掷动作，激活手雷
                ArmGrenade();
                
                // 播放投掷音效
                if (throwSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(throwSound);
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"💣 手雷被投掷！速度: {releaseSpeed:F2} m/s");
                }
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🤏 手雷被轻放，未激活。速度: {releaseSpeed:F2} m/s (阈值: {throwThreshold})");
                }
            }
            
            // 清理控制器引用
            currentInteractor = null;
        }
    }
    
    void ArmGrenade()
    {
        if (isArmed) return;
        
        isArmed = true;
        currentTimer = explosionTimer;
        
        // 启用指示灯
        if (indicatorLight != null)
        {
            indicatorLight.enabled = true;
        }
        
        // 禁用交互（投掷后不能再抓取）
        if (interactableObject != null)
        {
            interactableObject.enabled = false;
        }
        
        if (showDebugLogs)
        {
            Debug.Log("手雷已激活！开始倒计时...");
        }
    }
    
    void UpdateTimer()
    {
        currentTimer -= Time.deltaTime;
        
        if (currentTimer <= 0f && !hasExploded)
        {
            Explode();
        }
    }
    
    void UpdateIndicatorLight()
    {
        if (indicatorLight != null && lightColorGradient != null)
        {
            // 根据剩余时间改变灯光颜色
            float normalizedTime = 1f - (currentTimer / explosionTimer);
            indicatorLight.color = lightColorGradient.Evaluate(normalizedTime);
            
            // 随着时间推移，灯光闪烁越来越快
            float blinkSpeed = Mathf.Lerp(1f, 10f, normalizedTime);
            indicatorLight.intensity = 0.5f + 0.5f * Mathf.Sin(Time.time * blinkSpeed);
        }
    }
    
    void UpdateTickSound()
    {
        if (tickSound != null && audioSource != null)
        {
            // 随着时间推移，滴答声越来越快
            float timeRatio = currentTimer / explosionTimer;
            tickInterval = Mathf.Lerp(0.2f, 1f, timeRatio);
            
            if (Time.time - lastTickTime >= tickInterval)
            {
                audioSource.PlayOneShot(tickSound);
                lastTickTime = Time.time;
            }
        }
    }
    
    void Explode()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        if (showDebugLogs)
        {
            Debug.Log("💥 手雷爆炸！");
        }
        
        Vector3 explosionPos = transform.position;
        
        // 生成爆炸特效（参考GrenadeScript逻辑）
        if (explosionPrefab != null)
        {
            // 检测地面
            RaycastHit checkGround;
            if (Physics.Raycast(transform.position, Vector3.down, out checkGround, 50))
            {
                // 在地面生成爆炸特效，使用正确的朝向
                Instantiate(explosionPrefab, checkGround.point, 
                    Quaternion.FromToRotation(Vector3.forward, checkGround.normal));
            }
        }
        
        // 爆炸效果
        ApplyExplosionEffect(explosionPos);
        
        // 直接销毁手雷对象（参考GrenadeScript）
        Destroy(gameObject);
    }
    
    void ApplyExplosionEffect(Vector3 explosionPos)
    {
        // 爆炸力（参考GrenadeScript逻辑）
        Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
        
        if (showDebugLogs)
        {
            Debug.Log($"爆炸影响了 {colliders.Length} 个对象");
        }
        
        foreach (Collider hit in colliders) 
        {
            if (hit.gameObject == gameObject) continue; // 跳过自己
            
            // 应用物理力（参考GrenadeScript）
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionPower * 5, explosionPos, explosionRadius, 3.0F);
            }
            
            // 计算距离和伤害衰减（额外的伤害逻辑）
            float distance = Vector3.Distance(explosionPos, hit.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius);
            damageMultiplier = Mathf.Clamp01(damageMultiplier);
            
            // 对敌人造成伤害
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                float actualDamage = damageToEnemies * damageMultiplier;
                enemy.Health(actualDamage);
                
                if (showDebugLogs)
                {
                    Debug.Log($"💀 手雷对敌人 {hit.name} 造成 {actualDamage:F1} 点伤害");
                }
            }
            
            // 对玩家造成伤害
            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                float actualDamage = damageToPlayer * damageMultiplier;
                player.PlayerHealth(actualDamage);
                
                if (showDebugLogs)
                {
                    Debug.Log($"💔 手雷对玩家造成 {actualDamage:F1} 点伤害");
                }
            }
            
            // 处理可破坏物体（参考GrenadeScript逻辑）
            HandleDestructibleObjects(hit);
        }
    }
    
    void HandleDestructibleObjects(Collider hit)
    {
        // 直接使用GrenadeScript的逻辑
        
        // 如果爆炸击中"Target"标签且isHit为false
        if (hit.GetComponent<Collider>().tag == "Target" 
            && hit.gameObject.GetComponent<TargetScript>().isHit == false) 
        {
            // 播放目标动画
            hit.gameObject.GetComponent<Animation>().Play("target_down");
            // 切换目标对象的"isHit"状态
            hit.gameObject.GetComponent<TargetScript>().isHit = true;
        }

        // 如果爆炸击中"ExplosiveBarrel"标签
        if (hit.GetComponent<Collider>().tag == "ExplosiveBarrel") 
        {
            // 切换爆炸桶对象的"explode"状态
            hit.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
        }

        // 如果爆炸击中"GasTank"标签
        if (hit.GetComponent<Collider>().tag == "GasTank") 
        {
            // 切换汽油罐对象的"isHit"状态
            hit.gameObject.GetComponent<GasTankScript>().isHit = true;
            // 减少汽油罐对象的爆炸计时器，使其更快爆炸
            hit.gameObject.GetComponent<GasTankScript>().explosionTimer = 0.05f;
        }
    }
    

    
    // 在Scene视图中显示爆炸半径
    void OnDrawGizmosSelected()
    {
        if (showExplosionRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius * 0.5f);
        }
    }
    
    // 公共方法：手动引爆
    public void ManualExplode()
    {
        if (!hasExploded)
        {
            StopAllCoroutines();
            Explode();
        }
    }
    
    // 公共方法：设置投掷阈值
    public void SetThrowThreshold(float threshold)
    {
        throwThreshold = threshold;
    }
    
    // 检查按钮激活
    void CheckButtonActivation()
    {
        if (currentInteractor == null) return;
        
        // 尝试获取输入设备
        InputDevice inputDevice = new InputDevice();
        
        if (currentInteractor.name.Contains("Left"))
        {
            inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }
        else if (currentInteractor.name.Contains("Right"))
        {
            inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }
        
        // 检查按钮是否被按下
        bool buttonPressed = false;
        if (inputDevice.isValid)
        {
            if (activationButton == InputHelpers.Button.PrimaryButton)
            {
                inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out buttonPressed);
            }
            else if (activationButton == InputHelpers.Button.SecondaryButton)
            {
                inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out buttonPressed);
            }
            else if (activationButton == InputHelpers.Button.Trigger)
            {
                inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out buttonPressed);
            }
            else if (activationButton == InputHelpers.Button.Grip)
            {
                inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out buttonPressed);
            }
        }
        
        if (buttonPressed)
        {
            // 按钮激活手雷
            ArmGrenade();
            
            // 播放投掷音效
            if (throwSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(throwSound);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"🔥 手雷通过按钮激活！按钮: {activationButton}");
            }
        }
    }
} 