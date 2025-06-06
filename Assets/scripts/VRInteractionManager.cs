using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class VRInteractionManager : MonoBehaviour
{
    [Header("XR设置")]
    [Tooltip("XR交互管理器")]
    public XRInteractionManager xrInteractionManager;
    
    [Header("控制器设置")]
    [Tooltip("左手控制器")]
    public Transform leftController;
    
    [Tooltip("右手控制器")]
    public Transform rightController;
    
    [Header("交互设置")]
    [Tooltip("默认交互图层")]
    public LayerMask defaultInteractionLayer = 1;
    
    [Tooltip("可抓取物体的标签")]
    public List<string> grabbableTags = new List<string> { "Grabbable", "Interactable" };
    
    [Header("射线设置")]
    [Tooltip("是否启用射线交互")]
    public bool enableRayInteraction = true;
    
    [Tooltip("射线最大距离")]
    public float rayMaxDistance = 10f;
    
    private List<VRRayInteractorSetup> rayInteractors = new List<VRRayInteractorSetup>();
    
    void Start()
    {
        SetupVRInteractionSystem();
    }
    
    void SetupVRInteractionSystem()
    {
        // 确保有XR交互管理器
        if (xrInteractionManager == null)
        {
            xrInteractionManager = FindObjectOfType<XRInteractionManager>();
            if (xrInteractionManager == null)
            {
                GameObject managerGO = new GameObject("XR Interaction Manager");
                xrInteractionManager = managerGO.AddComponent<XRInteractionManager>();
            }
        }
        
        // 设置控制器射线交互
        if (enableRayInteraction)
        {
            SetupControllerRayInteractor(leftController, "Left");
            SetupControllerRayInteractor(rightController, "Right");
        }
        
        // 自动设置场景中的可交互物体
        SetupInteractableObjects();
        
        Debug.Log("VR交互系统设置完成");
    }
    
    void SetupControllerRayInteractor(Transform controller, string handName)
    {
        if (controller == null)
        {
            Debug.LogWarning($"{handName}手控制器未分配");
            return;
        }
        
        // 检查是否已经有射线交互器
        var existingRay = controller.GetComponentInChildren<XRRayInteractor>();
        if (existingRay != null)
        {
            Debug.Log($"{handName}手控制器已有射线交互器");
            return;
        }
        
        // 创建射线交互器游戏对象
        GameObject rayGO = new GameObject($"{handName} Ray Interactor");
        rayGO.transform.SetParent(controller);
        rayGO.transform.localPosition = Vector3.zero;
        rayGO.transform.localRotation = Quaternion.identity;
        
        // 添加射线设置脚本
        VRRayInteractorSetup raySetup = rayGO.AddComponent<VRRayInteractorSetup>();
        raySetup.maxRayDistance = rayMaxDistance;
        raySetup.interactionLayerMask = defaultInteractionLayer;
        
        rayInteractors.Add(raySetup);
        
        Debug.Log($"{handName}手射线交互器设置完成");
    }
    
    void SetupInteractableObjects()
    {
        // 查找所有带有可抓取标签的游戏对象
        foreach (string tag in grabbableTags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                SetupObjectAsInteractable(obj);
            }
        }
        
        // 也可以查找所有带有特定组件的物体
        VRInteractableObject[] existingInteractables = FindObjectsOfType<VRInteractableObject>();
        foreach (var interactable in existingInteractables)
        {
            EnsureProperSetup(interactable.gameObject);
        }
    }
    
    public void SetupObjectAsInteractable(GameObject obj)
    {
        if (obj == null) return;
        
        // 检查是否已经是可交互物体
        if (obj.GetComponent<VRInteractableObject>() != null)
        {
            EnsureProperSetup(obj);
            return;
        }
        
        // 添加VR可交互组件
        VRInteractableObject interactable = obj.AddComponent<VRInteractableObject>();
        
        // 确保有必要的组件
        EnsureProperSetup(obj);
        
        Debug.Log($"物体 {obj.name} 已设置为可交互");
    }
    
    void EnsureProperSetup(GameObject obj)
    {
        // 确保有Rigidbody
        if (obj.GetComponent<Rigidbody>() == null)
        {
            obj.AddComponent<Rigidbody>();
        }
        
        // 确保有Collider
        if (obj.GetComponent<Collider>() == null)
        {
            obj.AddComponent<BoxCollider>();
        }
        
        // 设置图层
        if (obj.layer == 0) // 如果还在Default层
        {
            obj.layer = GetLayerFromMask(defaultInteractionLayer);
        }
    }
    
    int GetLayerFromMask(LayerMask mask)
    {
        int layer = 0;
        int maskValue = mask.value;
        while (maskValue > 1)
        {
            maskValue >>= 1;
            layer++;
        }
        return layer;
    }
    
    public void ToggleRayInteraction(bool enabled)
    {
        foreach (var rayInteractor in rayInteractors)
        {
            if (rayInteractor != null)
            {
                rayInteractor.SetRayEnabled(enabled);
            }
        }
    }
    
    public void CreateTestCube()
    {
        // 创建一个测试立方体
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Test Grabbable Cube";
        cube.transform.position = new Vector3(0, 2, 2);
        cube.tag = "Grabbable";
        
        // 添加一个醒目的材质
        Renderer renderer = cube.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.red;
        renderer.material = mat;
        
        SetupObjectAsInteractable(cube);
        
        Debug.Log("测试立方体已创建");
    }
} 