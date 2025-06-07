using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportSetup : MonoBehaviour
{
    [Header("传送设置")]
    [Tooltip("传送区域的材质")]
    public Material teleportMaterial;
    
    [Tooltip("自动创建传送区域")]
    public bool autoCreateTeleportAreas = true;
    
    [Tooltip("传送区域的大小")]
    public Vector3 teleportAreaSize = new Vector3(2f, 0.1f, 2f);
    
    void Start()
    {
        if (autoCreateTeleportAreas)
        {
            CreateTeleportAreas();
        }
    }
    
    void CreateTeleportAreas()
    {
        // 在场景中寻找所有的平面物体，为它们添加传送功能
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // 检查是否是地面或平台
            if (obj.name.ToLower().Contains("plane") || 
                obj.name.ToLower().Contains("ground") || 
                obj.name.ToLower().Contains("floor"))
            {
                AddTeleportArea(obj);
            }
        }
        
        Debug.Log("传送区域设置完成！");
    }
    
    void AddTeleportArea(GameObject targetObject)
    {
        // 检查是否已经有传送组件
        if (targetObject.GetComponent<TeleportationAnchor>() != null)
            return;
            
        // 添加传送锚点组件
        TeleportationAnchor teleportAnchor = targetObject.AddComponent<TeleportationAnchor>();
        
        // 如果没有 Collider，添加一个
        if (targetObject.GetComponent<Collider>() == null)
        {
            BoxCollider collider = targetObject.AddComponent<BoxCollider>();
            collider.size = teleportAreaSize;
        }
        
        Debug.Log($"为 {targetObject.name} 添加了传送功能");
    }
    
    [ContextMenu("手动创建传送区域")]
    public void ManualCreateTeleportAreas()
    {
        CreateTeleportAreas();
    }
    
    [ContextMenu("创建传送平台")]
    public void CreateTeleportPlatform()
    {
        // 在当前位置创建一个传送平台
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Teleport Platform";
        platform.transform.position = transform.position;
        platform.transform.localScale = teleportAreaSize;
        
        // 添加传送功能
        AddTeleportArea(platform);
        
        // 应用材质
        if (teleportMaterial != null)
        {
            platform.GetComponent<Renderer>().material = teleportMaterial;
        }
        
        Debug.Log("创建了传送平台");
    }
} 