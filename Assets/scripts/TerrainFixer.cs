 using UnityEngine;

public class TerrainFixer : MonoBehaviour
{
    [Header("Terrain修复工具")]
    [Tooltip("要修复的Terrain对象")]
    public Terrain targetTerrain;
    
    [Tooltip("地面纹理")]
    public Texture2D groundTexture;
    
    void Start()
    {
        if (targetTerrain == null)
        {
            // 自动查找场景中的Terrain
            targetTerrain = FindObjectOfType<Terrain>();
        }
        
        if (targetTerrain != null)
        {
            FixTerrainDisplay();
        }
    }
    
    [ContextMenu("修复Terrain显示")]
    public void FixTerrainDisplay()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("没有找到Terrain对象！");
            return;
        }
        
        TerrainData terrainData = targetTerrain.terrainData;
        
        // 检查是否有纹理层
        if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
        {
            Debug.Log("Terrain没有纹理层，正在创建默认纹理层...");
            CreateDefaultTerrainLayer();
        }
        
        // 检查材质
        if (targetTerrain.materialTemplate == null)
        {
            Debug.Log("Terrain没有材质，正在设置默认材质...");
            SetDefaultTerrainMaterial();
        }
        
        Debug.Log("Terrain显示修复完成！");
    }
    
    void CreateDefaultTerrainLayer()
    {
        TerrainData terrainData = targetTerrain.terrainData;
        
        // 创建新的TerrainLayer
        TerrainLayer newLayer = new TerrainLayer();
        
        if (groundTexture != null)
        {
            newLayer.diffuseTexture = groundTexture;
        }
        else
        {
            // 使用默认的白色纹理
            newLayer.diffuseTexture = Texture2D.whiteTexture;
        }
        
        newLayer.tileSize = new Vector2(15, 15); // 设置纹理平铺大小
        
        // 应用纹理层
        terrainData.terrainLayers = new TerrainLayer[] { newLayer };
        
        // 设置权重，使整个terrain都使用这个纹理
        float[,,] alphamaps = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 1];
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                alphamaps[x, y, 0] = 1.0f; // 设置权重为1（完全使用这个纹理）
            }
        }
        terrainData.SetAlphamaps(0, 0, alphamaps);
        
        Debug.Log("已创建默认地面纹理层");
    }
    
    void SetDefaultTerrainMaterial()
    {
        // 尝试使用标准的Terrain材质
        Material terrainMaterial = new Material(Shader.Find("Nature/Terrain/Standard"));
        if (terrainMaterial.shader == null)
        {
            // 如果标准Terrain shader不可用，使用Standard shader
            terrainMaterial = new Material(Shader.Find("Standard"));
            terrainMaterial.color = new Color(0.5f, 0.7f, 0.3f, 1.0f); // 绿褐色
        }
        
        targetTerrain.materialTemplate = terrainMaterial;
        Debug.Log("已设置默认Terrain材质");
    }
    
    [ContextMenu("创建简单地面纹理")]
    public void CreateSimpleGroundTexture()
    {
        // 创建一个简单的程序化地面纹理
        Texture2D simpleTexture = new Texture2D(256, 256);
        Color[] pixels = new Color[256 * 256];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            // 创建带有一点噪声的绿褐色纹理
            float noise = Mathf.PerlinNoise((i % 256) * 0.1f, (i / 256) * 0.1f);
            pixels[i] = Color.Lerp(new Color(0.4f, 0.6f, 0.2f), new Color(0.6f, 0.5f, 0.3f), noise);
        }
        
        simpleTexture.SetPixels(pixels);
        simpleTexture.Apply();
        
        groundTexture = simpleTexture;
        
        Debug.Log("已创建简单地面纹理");
        FixTerrainDisplay();
    }
    
    [ContextMenu("重置Terrain设置")]
    public void ResetTerrainSettings()
    {
        if (targetTerrain == null) return;
        
        // 重置一些可能导致显示问题的设置
        targetTerrain.heightmapPixelError = 5;
        targetTerrain.basemapDistance = 1000;
        targetTerrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        
        Debug.Log("已重置Terrain设置");
    }
}