using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponVRCanvasManager : MonoBehaviour
{
    private Canvas weaponCanvas;
    private RectTransform canvasRect;
    
    [Header("Canvas Settings")]
    [Tooltip("UI与摄像机的距离")]
    public float distanceFromCamera = 1f;
    
    [Tooltip("UI整体缩放")]
    public float uiScale = 1f;

    [Header("Position Settings")]
    [Tooltip("UI在屏幕上的位置")]
    public Vector2 screenPosition = new Vector2(0.5f, 0.3f); // x和y的值从0到1，0.5是中间

    [Tooltip("是否固定在视野中心")]
    public bool centerInView = true;
    
    void Start()
    {
        SetupCanvas();
    }
    
    void SetupCanvas()
    {
        // 获取Canvas组件
        weaponCanvas = GetComponent<Canvas>();
        if (weaponCanvas == null)
        {
            Debug.LogError("WeaponVRCanvasManager: No Canvas component found!");
            return;
        }

        canvasRect = GetComponent<RectTransform>();
        
        // 设置Canvas为Screen Space - Camera模式
        weaponCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        weaponCanvas.worldCamera = Camera.main;
        weaponCanvas.planeDistance = distanceFromCamera;
        
        // 获取或添加Canvas Scaler
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        
        // 设置Canvas Scaler
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // 调整所有Text组件的大小
        AdjustTextSizes();

        // 调整Canvas位置
        UpdateCanvasPosition();
    }

    void UpdateCanvasPosition()
    {
        if (canvasRect == null) return;

        if (centerInView)
        {
            // 居中显示
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            // 使用自定义位置
            canvasRect.anchorMin = screenPosition;
            canvasRect.anchorMax = screenPosition;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
        }

        // 确保所有子UI元素都在视野内
        foreach (RectTransform child in GetComponentsInChildren<RectTransform>())
        {
            if (child != canvasRect)
            {
                // 重置子元素的相对位置
                child.localPosition = new Vector3(child.localPosition.x, child.localPosition.y, 0);
            }
        }
    }
    
    void AdjustTextSizes()
    {
        // 调整TextMeshPro组件
        foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>())
        {
            tmp.fontSize *= uiScale;
            // 确保文本清晰可见
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
        }
        
        // 调整传统Text组件
        foreach (Text text in GetComponentsInChildren<Text>())
        {
            text.fontSize = Mathf.RoundToInt(text.fontSize * uiScale);
            // 确保文本清晰可见
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
        }
        
        // 调整RectTransform大小
        foreach (RectTransform rect in GetComponentsInChildren<RectTransform>())
        {
            if (rect != transform)
            {
                rect.sizeDelta *= uiScale;
            }
        }
    }

    void LateUpdate()
    {
        if (!centerInView)
        {
            UpdateCanvasPosition();
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            SetupCanvas();
        }
    }
} 