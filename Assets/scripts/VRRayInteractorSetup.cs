using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRRayInteractorSetup : MonoBehaviour
{
    [Header("射线设置")]
    [Tooltip("射线最大距离")]
    public float maxRayDistance = 10f;
    
    [Tooltip("射线颜色 - 正常状态")]
    public Color normalColor = Color.blue;
    
    [Tooltip("射线颜色 - 悬停状态")]
    public Color hoverColor = Color.green;
    
    [Tooltip("射线颜色 - 选中状态")]
    public Color selectColor = Color.red;
    
    [Tooltip("射线宽度")]
    public float lineWidth = 0.005f;
    
    [Header("交互设置")]
    [Tooltip("可交互的图层")]
    public LayerMask interactionLayerMask = -1;
    
    private XRRayInteractor rayInteractor;
    private XRInteractorLineVisual lineVisual;
    private LineRenderer lineRenderer;
    
    void Start()
    {
        SetupRayInteractor();
    }
    
    void SetupRayInteractor()
    {
        // 获取或添加 XRRayInteractor 组件
        rayInteractor = GetComponent<XRRayInteractor>();
        if (rayInteractor == null)
        {
            rayInteractor = gameObject.AddComponent<XRRayInteractor>();
        }
        
        // 设置射线交互器属性
        rayInteractor.maxRaycastDistance = maxRayDistance;
        rayInteractor.raycastMask = interactionLayerMask;
        rayInteractor.selectActionTrigger = XRBaseControllerInteractor.InputTriggerType.StateChange;
        rayInteractor.enableUIInteraction = true;
        
        // 获取或添加线渲染器
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // 设置线渲染器属性
        SetupLineRenderer();
        
        // 获取或添加线可视化组件
        lineVisual = GetComponent<XRInteractorLineVisual>();
        if (lineVisual == null)
        {
            lineVisual = gameObject.AddComponent<XRInteractorLineVisual>();
        }
        
        // 设置线可视化属性
        SetupLineVisual();
        
        Debug.Log($"VR射线交互器已设置在 {gameObject.name}");
    }
    
    void SetupLineRenderer()
    {
        lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.castShadows = false;
        lineRenderer.receiveShadows = false;
    }
    
    void SetupLineVisual()
    {
        lineVisual.lineWidth = lineWidth;
        lineVisual.overrideInteractorLineLength = true;
        lineVisual.lineLength = maxRayDistance;
        
        // 设置颜色梯度
        Gradient validGradient = new Gradient();
        validGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(normalColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        lineVisual.validColorGradient = validGradient;
        
        Gradient invalidGradient = new Gradient();
        invalidGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        lineVisual.invalidColorGradient = invalidGradient;
    }
    
    void Update()
    {
        // 根据交互状态改变射线颜色
        if (rayInteractor != null && lineRenderer != null)
        {
            Color currentColor;
            if (rayInteractor.hasSelection)
            {
                currentColor = selectColor;
            }
            else if (rayInteractor.hasHover)
            {
                currentColor = hoverColor;
            }
            else
            {
                currentColor = normalColor;
            }
            
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
        }
    }
    
    public void SetRayEnabled(bool enabled)
    {
        if (rayInteractor != null)
        {
            rayInteractor.enabled = enabled;
        }
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = enabled;
        }
        
        if (lineVisual != null)
        {
            lineVisual.enabled = enabled;
        }
    }
} 