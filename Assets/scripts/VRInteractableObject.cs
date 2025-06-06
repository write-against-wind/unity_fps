using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRInteractableObject : XRGrabInteractable
{
    [Header("物体设置")]
    [Tooltip("是否在抓取时隐藏手柄")]
    public bool hideHandOnGrab = true;
    
    [Header("抓取位置设置")]
    [Tooltip("抓取时物体相对于手柄的偏移距离")]
    public Vector3 grabOffset = new Vector3(0, 0, 0.05f);
    
    [Tooltip("是否自动调整抓取位置到物体中心")]
    public bool autoAdjustGrabPoint = true;
    
    [Tooltip("抓取距离倍数（1.0为默认距离，0.5为更近，2.0为更远）")]
    [Range(0.1f, 2.0f)]
    public float grabDistanceMultiplier = 0.7f;
    
    [Header("材质设置")]
    [Tooltip("抓取时的材质（可选，留空则使用自动变亮）")]
    public Material grabMaterial;
    
    [Tooltip("正常状态的材质（可选，留空则使用当前材质）")]
    public Material normalMaterial;
    
    [Tooltip("悬停时的材质（可选，留空则使用轻微变亮）")]
    public Material hoverMaterial;
    
    [Header("自动变亮设置")]
    [Tooltip("是否启用自动变亮（当未设置自定义材质时）")]
    public bool useAutoBrightening = true;
    
    [Tooltip("抓取时的亮度倍数")]
    [Range(1.0f, 3.0f)]
    public float grabBrightness = 1.5f;
    
    [Tooltip("悬停时的亮度倍数")]
    [Range(1.0f, 2.0f)]
    public float hoverBrightness = 1.2f;
    
    [Tooltip("自动变亮时使用的发光强度")]
    [Range(0.0f, 1.0f)]
    public float emissionIntensity = 0.3f;
    
    private Renderer objectRenderer;
    private Rigidbody rb;
    private Collider col;
    private Material originalMaterial;
    private Material brightMaterial;
    private Material lightBrightMaterial;
    private Color originalColor;
    private Transform originalAttachTransform;
    private GameObject attachPoint;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 获取组件
        objectRenderer = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        // 确保有刚体组件
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 确保有碰撞器
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        
        // 设置抓取点
        SetupGrabPoint();
        
        // 保存原始材质和颜色
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            if (originalMaterial != null)
            {
                originalColor = originalMaterial.color;
                
                // 如果启用自动变亮且未设置自定义材质，创建变亮材质
                if (useAutoBrightening)
                {
                    CreateBrightMaterials();
                }
            }
        }
        
        // 设置初始材质
        if (normalMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = normalMaterial;
            originalMaterial = normalMaterial;
            originalColor = normalMaterial.color;
        }
    }
    
    private void SetupGrabPoint()
    {
        // 保存原始的attachTransform
        originalAttachTransform = attachTransform;
        
        // 创建一个新的附着点
        attachPoint = new GameObject("GrabAttachPoint");
        attachPoint.transform.SetParent(transform);
        
        // 设置附着点位置
        if (autoAdjustGrabPoint && col != null)
        {
            // 自动调整到物体中心附近
            Vector3 center = col.bounds.center;
            Vector3 localCenter = transform.InverseTransformPoint(center);
            
            // 应用偏移和距离倍数
            Vector3 finalOffset = grabOffset * grabDistanceMultiplier;
            attachPoint.transform.localPosition = localCenter + finalOffset;
        }
        else
        {
            // 使用手动设置的偏移
            Vector3 finalOffset = grabOffset * grabDistanceMultiplier;
            attachPoint.transform.localPosition = finalOffset;
        }
        
        // 设置为新的附着点
        attachTransform = attachPoint.transform;
    }
    
    private void CreateBrightMaterials()
    {
        if (originalMaterial == null) return;
        
        // 创建悬停时的变亮材质
        if (hoverMaterial == null)
        {
            lightBrightMaterial = new Material(originalMaterial);
            lightBrightMaterial.color = originalColor * hoverBrightness;
            
            // 添加轻微发光效果
            if (lightBrightMaterial.HasProperty("_EmissionColor"))
            {
                lightBrightMaterial.EnableKeyword("_EMISSION");
                lightBrightMaterial.SetColor("_EmissionColor", originalColor * (emissionIntensity * 0.5f));
            }
        }
        
        // 创建抓取时的变亮材质
        if (grabMaterial == null)
        {
            brightMaterial = new Material(originalMaterial);
            brightMaterial.color = originalColor * grabBrightness;
            
            // 添加发光效果
            if (brightMaterial.HasProperty("_EmissionColor"))
            {
                brightMaterial.EnableKeyword("_EMISSION");
                brightMaterial.SetColor("_EmissionColor", originalColor * emissionIntensity);
            }
        }
    }
    
    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        
        // 悬停时改变材质
        if (objectRenderer != null)
        {
            if (hoverMaterial != null)
            {
                objectRenderer.material = hoverMaterial;
            }
            else if (useAutoBrightening && lightBrightMaterial != null)
            {
                objectRenderer.material = lightBrightMaterial;
            }
        }
        
        Debug.Log($"物体 {gameObject.name} 被悬停");
    }
    
    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);
        
        // 取消悬停时恢复材质
        if (!isSelected && objectRenderer != null)
        {
            Material targetMaterial = normalMaterial != null ? normalMaterial : originalMaterial;
            if (targetMaterial != null)
            {
                objectRenderer.material = targetMaterial;
            }
        }
        
        Debug.Log($"物体 {gameObject.name} 取消悬停");
    }
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        
        // 被抓取时改变材质
        if (objectRenderer != null)
        {
            if (grabMaterial != null)
            {
                objectRenderer.material = grabMaterial;
            }
            else if (useAutoBrightening && brightMaterial != null)
            {
                objectRenderer.material = brightMaterial;
            }
        }
        
        // 隐藏手柄（如果设置了的话）
        if (hideHandOnGrab)
        {
            var controller = args.interactorObject as XRBaseControllerInteractor;
            if (controller != null)
            {
                controller.hideControllerOnSelect = true;
            }
        }
        
        Debug.Log($"物体 {gameObject.name} 被抓取");
    }
    
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        
        // 释放时恢复材质
        if (objectRenderer != null)
        {
            Material targetMaterial = normalMaterial != null ? normalMaterial : originalMaterial;
            if (targetMaterial != null)
            {
                objectRenderer.material = targetMaterial;
            }
        }
        
        // 显示手柄
        if (hideHandOnGrab)
        {
            var controller = args.interactorObject as XRBaseControllerInteractor;
            if (controller != null)
            {
                controller.hideControllerOnSelect = false;
            }
        }
        
        Debug.Log($"物体 {gameObject.name} 被释放");
    }
    
    /// <summary>
    /// 运行时调整抓取距离
    /// </summary>
    /// <param name="newMultiplier">新的距离倍数</param>
    public void AdjustGrabDistance(float newMultiplier)
    {
        grabDistanceMultiplier = Mathf.Clamp(newMultiplier, 0.1f, 2.0f);
        
        if (attachPoint != null)
        {
            // 重新计算附着点位置
            if (autoAdjustGrabPoint && col != null)
            {
                Vector3 center = col.bounds.center;
                Vector3 localCenter = transform.InverseTransformPoint(center);
                Vector3 finalOffset = grabOffset * grabDistanceMultiplier;
                attachPoint.transform.localPosition = localCenter + finalOffset;
            }
            else
            {
                Vector3 finalOffset = grabOffset * grabDistanceMultiplier;
                attachPoint.transform.localPosition = finalOffset;
            }
        }
    }
    
    private void OnDestroy()
    {
        // 清理创建的材质
        if (brightMaterial != null)
        {
            DestroyImmediate(brightMaterial);
        }
        if (lightBrightMaterial != null)
        {
            DestroyImmediate(lightBrightMaterial);
        }
        
        // 清理附着点
        if (attachPoint != null)
        {
            DestroyImmediate(attachPoint);
        }
    }
} 