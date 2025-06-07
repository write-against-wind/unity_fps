using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR球棒武器脚本 - Trigger版本（适用于敌人碰撞器勾选了Is Trigger的情况）
/// </summary>
public class VRBatWeapon_Trigger : MonoBehaviour
{
    [Header("伤害设置")]
    [Tooltip("球棒造成的最小伤害")]
    public float minDamage = 20f;
    
    [Tooltip("球棒造成的最大伤害")]
    public float maxDamage = 40f;
    
    [Tooltip("触发伤害所需的最小挥动速度")]
    public float minSwingVelocity = 2f;
    
    [Tooltip("伤害冷却时间（防止连续伤害同一敌人）")]
    public float damageCooldown = 0.8f;
    
    [Header("音效设置")]
    [Tooltip("击中敌人的音效")]
    public AudioClip hitEnemySound;
    
    [Tooltip("击中其他物体的音效")]
    public AudioClip hitObjectSound;
    
    [Header("特效设置")]
    [Tooltip("击中敌人时的特效预制体")]
    public GameObject hitEffect;
    
    [Header("碰撞检测设置")]
    [Tooltip("球棒的触发器碰撞器（用于检测敌人）")]
    public Collider triggerCollider;
    
    [Tooltip("是否自动创建触发器碰撞器")]
    public bool autoCreateTriggerCollider = true;
    
    [Header("挥动检测设置")]
    [Tooltip("挥动冷却时间（防止连续触发）")]
    [Range(0.1f, 1f)]
    public float swingCooldownTime = 0.3f;
    
    [Tooltip("最小角速度（度/秒）- 检测旋转挥动")]
    [Range(30f, 300f)]
    public float minAngularVelocity = 90f;
    
    [Tooltip("角加速度阈值（检测主动挥动）")]
    [Range(10f, 200f)]
    public float angularAccelerationThreshold = 50f;
    
    [Tooltip("位置速度阈值（配合旋转检测）")]
    [Range(0.5f, 5f)]
    public float positionVelocityThreshold = 1.5f;
    
    [Header("距离检测设置")]
    [Tooltip("检测球棒是否在接近敌人")]
    public bool enableDistanceCheck = true;
    
    [Tooltip("接近速度阈值（正值表示接近）")]
    [Range(0.1f, 3f)]
    public float approachVelocityThreshold = 0.5f;
    
    // 私有变量
    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private Rigidbody rb;
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private bool isGrabbed = false;
    private Dictionary<Collider, float> lastHitTime = new Dictionary<Collider, float>();
    
    // 速度平滑计算
    private Queue<Vector3> velocityHistory = new Queue<Vector3>();
    private const int velocityHistorySize = 5;
    
    // 当前在触发器内的敌人列表
    private HashSet<Collider> enemiesInTrigger = new HashSet<Collider>();
    
    // 挥动检测相关
    private Vector3 previousVelocity = Vector3.zero;
    private float lastSwingTime = 0f;
    
    // 旋转检测相关
    private Quaternion lastRotation;
    private Vector3 currentAngularVelocity = Vector3.zero;
    private Vector3 previousAngularVelocity = Vector3.zero;
    private Queue<Vector3> angularVelocityHistory = new Queue<Vector3>();
    private const int angularHistorySize = 5;
    
    // 距离检测相关
    private Dictionary<Collider, float> lastDistanceToEnemy = new Dictionary<Collider, float>();
    private Dictionary<Collider, Vector3> lastEnemyPosition = new Dictionary<Collider, Vector3>();
    
    void Start()
    {
        // 获取组件
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        
        // 如果没有AudioSource，添加一个
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D音效
            audioSource.volume = 0.7f;
        }
        
        // 设置触发器碰撞器
        SetupTriggerCollider();
        
        // 绑定抓取事件
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
        
        lastPosition = transform.position;
        
        // 初始化旋转检测
        lastRotation = transform.rotation;
        
        Debug.Log("VR球棒武器（Trigger版本）初始化完成");
    }
    
    void SetupTriggerCollider()
    {
        if (triggerCollider == null && autoCreateTriggerCollider)
        {
            // 查找现有的触发器碰撞器
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                if (col.isTrigger)
                {
                    triggerCollider = col;
                    break;
                }
            }
            
            // 如果没有找到，创建一个新的
            if (triggerCollider == null)
            {
                BoxCollider newTrigger = gameObject.AddComponent<BoxCollider>();
                newTrigger.isTrigger = true;
                newTrigger.size = new Vector3(0.2f, 1f, 0.2f); // 球棒形状的触发器
                triggerCollider = newTrigger;
                
                Debug.Log("为球棒自动创建了触发器碰撞器");
            }
        }
    }
    
    void Update()
    {
        if (isGrabbed)
        {
            CalculateVelocity();
            CalculateAngularVelocity();
        }
    }
    
    void CalculateVelocity()
    {
        // 计算当前帧的速度
        Vector3 frameVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        
        // 添加到历史记录进行平滑
        velocityHistory.Enqueue(frameVelocity);
        if (velocityHistory.Count > velocityHistorySize)
        {
            velocityHistory.Dequeue();
        }
        
        // 计算平均速度
        Vector3 averageVelocity = Vector3.zero;
        foreach (Vector3 vel in velocityHistory)
        {
            averageVelocity += vel;
        }
        currentVelocity = averageVelocity / velocityHistory.Count;
    }
    
    void CalculateAngularVelocity()
    {
        // 计算当前帧的角速度
        Quaternion frameRotation = transform.rotation;
        Quaternion deltaRotation = frameRotation * Quaternion.Inverse(lastRotation);
        float angle;
        Vector3 axis;
        deltaRotation.ToAngleAxis(out angle, out axis);
        
        // 将角度转换为弧度
        float angleRad = angle * Mathf.Deg2Rad;
        
        // 计算角速度
        float angularVelocity = angleRad / Time.deltaTime;
        
        // 添加到历史记录进行平滑
        angularVelocityHistory.Enqueue(axis * angularVelocity);
        if (angularVelocityHistory.Count > angularHistorySize)
        {
            angularVelocityHistory.Dequeue();
        }
        
        // 计算平均角速度
        Vector3 averageAngularVelocity = Vector3.zero;
        foreach (Vector3 vel in angularVelocityHistory)
        {
            averageAngularVelocity += vel;
        }
        
        // 更新前一帧角速度
        previousAngularVelocity = currentAngularVelocity;
        currentAngularVelocity = averageAngularVelocity / angularVelocityHistory.Count;
        
        // 更新上一次的旋转
        lastRotation = frameRotation;
    }
    
    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        lastPosition = transform.position;
        velocityHistory.Clear();
        angularVelocityHistory.Clear();
        
        Debug.Log("球棒被抓取");
    }
    
    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        
        Debug.Log("球棒被释放");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInTrigger.Add(other);
            Debug.Log($"敌人 {other.name} 进入球棒攻击范围");
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        // 只有在被抓取时才检测攻击
        if (!isGrabbed) return;
        
        // 只检测敌人
        if (!other.CompareTag("Enemy")) return;
        
        // 使用新的挥动检测逻辑
        if (!IsValidSwing(other)) 
        {
            return;
        }
        
        // 检查冷却时间
        if (lastHitTime.ContainsKey(other))
        {
            if (Time.time - lastHitTime[other] < damageCooldown)
            {
                return; // 还在冷却时间内
            }
        }
        
        // 执行攻击
        float swingSpeed = currentVelocity.magnitude;
        float angularSpeed = currentAngularVelocity.magnitude * Mathf.Rad2Deg;
        HitEnemy(other, swingSpeed);
        lastHitTime[other] = Time.time;
        
        // 显示详细的攻击信息
        string distanceInfo = enableDistanceCheck ? "接近中" : "距离检测关闭";
        Debug.Log($"球棒击中敌人！位置速度: {swingSpeed:F2}, 角速度: {angularSpeed:F1}°/s, 状态: {distanceInfo}");
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInTrigger.Remove(other);
            
            // 清理距离记录
            if (lastDistanceToEnemy.ContainsKey(other))
            {
                lastDistanceToEnemy.Remove(other);
            }
            if (lastEnemyPosition.ContainsKey(other))
            {
                lastEnemyPosition.Remove(other);
            }
            
            Debug.Log($"敌人 {other.name} 离开球棒攻击范围");
        }
    }
    
    // 保留原有的OnCollisionEnter用于击中非敌人物体
    void OnCollisionEnter(Collision collision)
    {
        // 只有在被抓取时才处理碰撞
        if (!isGrabbed) return;
        
        // 使用新的挥动检测逻辑（非敌人物体不需要距离检测）
        if (!IsValidSwing(null)) return;
        
        // 只处理非敌人物体的碰撞
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            float swingSpeed = currentVelocity.magnitude;
            HitObject(collision, swingSpeed);
        }
    }
    
    void HitEnemy(Collider enemyCollider, float swingSpeed)
    {
        // 计算伤害（基于挥动速度）
        float speedMultiplier = Mathf.Clamp(swingSpeed / minSwingVelocity, 1f, 2.5f);
        float finalDamage = Random.Range(minDamage, maxDamage) * speedMultiplier;
        
        // 获取敌人组件并造成伤害
        Enemy enemy = enemyCollider.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Health(finalDamage);
            Debug.Log($"球棒击中敌人 {enemyCollider.name}！造成 {finalDamage:F1} 点伤害（速度: {swingSpeed:F2}）");
        }
        
        // 播放击中敌人音效
        if (hitEnemySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitEnemySound);
        }
        
        // 生成击中特效
        if (hitEffect != null)
        {
            Vector3 hitPoint = enemyCollider.ClosestPoint(transform.position);
            GameObject effect = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(transform.position - hitPoint));
            Destroy(effect, 3f); // 3秒后销毁特效
        }
        
        // 添加震动反馈
        AddHapticFeedback(0.4f, 0.3f);
    }
    
    void HitObject(Collision collision, float swingSpeed)
    {
        Debug.Log($"球棒击中物体: {collision.gameObject.name}（速度: {swingSpeed:F2}）");
        
        // 播放击中物体音效
        if (hitObjectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitObjectSound);
        }
        
        // 轻微震动反馈
        AddHapticFeedback(0.2f, 0.1f);
    }
    
    void AddHapticFeedback(float amplitude, float duration)
    {
        // 给当前抓取的控制器添加震动反馈
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            var interactor = grabInteractable.firstInteractorSelecting as XRBaseControllerInteractor;
            if (interactor != null)
            {
                StartCoroutine(SendHapticFeedback(interactor, amplitude, duration));
            }
        }
    }
    
    IEnumerator SendHapticFeedback(XRBaseControllerInteractor interactor, float amplitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            interactor.SendHapticImpulse(amplitude, 0.1f);
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    // 清理过期的击中记录和无效的敌人引用
    void LateUpdate()
    {
        // 清理过期的击中记录
        if (lastHitTime.Count > 0)
        {
            List<Collider> toRemove = new List<Collider>();
            foreach (var kvp in lastHitTime)
            {
                if (kvp.Key == null || Time.time - kvp.Value > damageCooldown * 2)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var collider in toRemove)
            {
                lastHitTime.Remove(collider);
            }
        }
        
        // 清理无效的敌人引用
        enemiesInTrigger.RemoveWhere(enemy => enemy == null);
        
        // 清理无效的距离记录
        if (lastDistanceToEnemy.Count > 0)
        {
            List<Collider> toRemoveDistance = new List<Collider>();
            foreach (var kvp in lastDistanceToEnemy)
            {
                if (kvp.Key == null)
                {
                    toRemoveDistance.Add(kvp.Key);
                }
            }
            
            foreach (var collider in toRemoveDistance)
            {
                lastDistanceToEnemy.Remove(collider);
                lastEnemyPosition.Remove(collider);
            }
        }
    }
    
    // 调试用：在Scene视图中显示当前速度和触发器范围
    void OnDrawGizmos()
    {
        if (isGrabbed && Application.isPlaying)
        {
            // 显示速度向量
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, currentVelocity);
            
            // 显示速度数值
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
        
        // 显示触发器范围
        if (triggerCollider != null)
        {
            Gizmos.color = Color.green;
            if (triggerCollider is BoxCollider boxCol)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position + boxCol.center, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, boxCol.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
        
        // 显示检测到的敌人
        if (Application.isPlaying && enemiesInTrigger.Count > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (Collider enemy in enemiesInTrigger)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(transform.position, enemy.transform.position);
                }
            }
        }
    }
    
    /// <summary>
    /// 检测是否是有效的挥动动作 - 基于旋转和距离检测
    /// </summary>
    bool IsValidSwing(Collider targetEnemy = null)
    {
        // 检查挥动冷却时间
        if (Time.time - lastSwingTime < swingCooldownTime) return false;
        
        // 获取当前角速度大小（度/秒）
        float currentAngularSpeed = currentAngularVelocity.magnitude * Mathf.Rad2Deg;
        
        // 检查基本角速度要求
        if (currentAngularSpeed < minAngularVelocity) return false;
        
        // 获取位置速度
        float positionSpeed = currentVelocity.magnitude;
        
        // 检查是否同时有位置移动（确保是真正的挥动而不是原地旋转）
        if (positionSpeed < positionVelocityThreshold) return false;
        
        // 距离检测（如果启用且有目标敌人）
        if (enableDistanceCheck && targetEnemy != null)
        {
            if (!IsApproachingEnemy(targetEnemy))
            {
                Debug.Log($"球棒正在远离敌人 {targetEnemy.name}，不认为是有效攻击");
                return false;
            }
        }
        
        // 检查角加速度（检测主动挥动）
        Vector3 previousAngularVel = previousAngularVelocity;
        Vector3 currentAngularVel = currentAngularVelocity;
        float angularAcceleration = (currentAngularVel - previousAngularVel).magnitude * Mathf.Rad2Deg / Time.deltaTime;
        
        // 如果角加速度足够大，说明是主动挥动
        if (angularAcceleration > angularAccelerationThreshold)
        {
            lastSwingTime = Time.time;
            Debug.Log($"检测到主动挥动 - 角速度: {currentAngularSpeed:F1}°/s, 角加速度: {angularAcceleration:F1}°/s², 位置速度: {positionSpeed:F2}");
            return true;
        }
        
        // 如果角速度很高，也认为是有效挥动
        if (currentAngularSpeed > minAngularVelocity * 1.5f)
        {
            lastSwingTime = Time.time;
            Debug.Log($"检测到高速挥动 - 角速度: {currentAngularSpeed:F1}°/s, 位置速度: {positionSpeed:F2}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检测球棒是否在接近敌人
    /// </summary>
    bool IsApproachingEnemy(Collider enemy)
    {
        if (enemy == null) return false;
        
        Vector3 currentBatPosition = transform.position;
        Vector3 currentEnemyPosition = enemy.transform.position;
        float currentDistance = Vector3.Distance(currentBatPosition, currentEnemyPosition);
        
        // 如果是第一次检测这个敌人，记录初始距离
        if (!lastDistanceToEnemy.ContainsKey(enemy))
        {
            lastDistanceToEnemy[enemy] = currentDistance;
            lastEnemyPosition[enemy] = currentEnemyPosition;
            return true; // 第一次检测默认允许
        }
        
        float lastDistance = lastDistanceToEnemy[enemy];
        Vector3 lastEnemyPos = lastEnemyPosition[enemy];
        
        // 计算距离变化率（负值表示接近，正值表示远离）
        float distanceChange = currentDistance - lastDistance;
        float approachVelocity = -distanceChange / Time.deltaTime; // 负号使接近为正值
        
        // 考虑敌人的移动，计算相对接近速度
        Vector3 enemyMovement = currentEnemyPosition - lastEnemyPos;
        Vector3 batMovement = currentBatPosition - transform.position; // 这里应该用上一帧的位置
        
        // 更新记录
        lastDistanceToEnemy[enemy] = currentDistance;
        lastEnemyPosition[enemy] = currentEnemyPosition;
        
        // 检查是否在接近
        bool isApproaching = approachVelocity > approachVelocityThreshold;
        
        if (!isApproaching)
        {
            Debug.Log($"球棒与敌人 {enemy.name} 距离变化: {distanceChange:F3}, 接近速度: {approachVelocity:F2} (阈值: {approachVelocityThreshold})");
        }
        
        return isApproaching;
    }
} 