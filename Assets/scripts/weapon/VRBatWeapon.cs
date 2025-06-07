using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRBatWeapon : MonoBehaviour
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
        
        // 绑定抓取事件
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
        
        lastPosition = transform.position;
        
        Debug.Log("VR球棒武器初始化完成");
    }
    
    void Update()
    {
        if (isGrabbed)
        {
            CalculateVelocity();
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
    
    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        lastPosition = transform.position;
        velocityHistory.Clear();
        
        Debug.Log("球棒被抓取");
    }
    
    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        
        Debug.Log("球棒被释放");
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 只有在被抓取时才能造成伤害
        if (!isGrabbed) return;
        
        // 检查挥动速度
        float swingSpeed = currentVelocity.magnitude;
        if (swingSpeed < minSwingVelocity)
        {
            Debug.Log($"挥动速度太慢: {swingSpeed:F2} < {minSwingVelocity}");
            return;
        }
        
        Collider hitCollider = collision.collider;
        
        // 检查冷却时间
        if (lastHitTime.ContainsKey(hitCollider))
        {
            if (Time.time - lastHitTime[hitCollider] < damageCooldown)
            {
                return; // 还在冷却时间内
            }
        }
        
        // 更新击中时间
        lastHitTime[hitCollider] = Time.time;
        
        // 检查是否击中敌人
        if (collision.gameObject.CompareTag("Enemy"))
        {
            HitEnemy(collision, swingSpeed);
        }
        else
        {
            HitObject(collision, swingSpeed);
        }
    }
    
    void HitEnemy(Collision collision, float swingSpeed)
    {
        // 计算伤害（基于挥动速度）
        float speedMultiplier = Mathf.Clamp(swingSpeed / minSwingVelocity, 1f, 2.5f);
        float finalDamage = Random.Range(minDamage, maxDamage) * speedMultiplier;
        
        // 获取敌人组件并造成伤害
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Health(finalDamage);
            Debug.Log($"球棒击中敌人 {collision.gameObject.name}！造成 {finalDamage:F1} 点伤害（速度: {swingSpeed:F2}）");
        }
        
        // 播放击中敌人音效
        if (hitEnemySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitEnemySound);
        }
        
        // 生成击中特效
        if (hitEffect != null)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            GameObject effect = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(collision.contacts[0].normal));
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
    
    // 清理过期的击中记录
    void LateUpdate()
    {
        if (lastHitTime.Count > 0)
        {
            List<Collider> toRemove = new List<Collider>();
            foreach (var kvp in lastHitTime)
            {
                if (Time.time - kvp.Value > damageCooldown * 2) // 保留时间是冷却时间的2倍
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var collider in toRemove)
            {
                lastHitTime.Remove(collider);
            }
        }
    }
    
    // 调试用：在Scene视图中显示当前速度
    void OnDrawGizmos()
    {
        if (isGrabbed && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, currentVelocity);
            
            // 显示速度数值
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
} 