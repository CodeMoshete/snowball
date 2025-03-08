using UnityEditor;
using UnityEngine;

[System.Serializable]
public class LightmapDataContainer
{
    public Texture2D[] lightmapColor;
    public Texture2D[] lightmapDir;
    public Texture2D[] shadowMask;
}

public class LevelLightingData : MonoBehaviour
{
    public bool IsMainDirectionalLightDisabled;
    public bool IsFogEnabled;
    public Color FogColor;
    public float FogDensity;
    public Material SkyboxMaterial;
    public LightmapDataContainer lightmapData;
    // public LightingSettings lightingSettings;
    // public LightingDataAsset lightingDataAsset;
    private void Start()
    {
        if (IsMainDirectionalLightDisabled)
        {
            GameObject mainLight = GameObject.Find("Directional Light");
            if (mainLight != null)
            {
                Debug.Log("Disabling Directional Light");
                mainLight.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Directional Light not found");
            }
        }

        RenderSettings.fog = IsFogEnabled;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogDensity = FogDensity;
        RenderSettings.skybox = SkyboxMaterial;

        // if (lightingSettings != null)
        // {
        //     Lightmapping.lightingSettings = lightingSettings;
        // }

        // if (lightingDataAsset != null)
        // {
        //     Lightmapping.lightingDataAsset = lightingDataAsset;
        // }

        if (lightmapData != null && lightmapData.lightmapColor.Length > 0)
        {
            LightmapData[] newLightmaps = new LightmapData[lightmapData.lightmapColor.Length];
            for (int i = 0; i < lightmapData.lightmapColor.Length; i++)
            {
                LightmapData data = new LightmapData();
                data.lightmapColor = lightmapData.lightmapColor[i];

                if (lightmapData.lightmapDir.Length > i)
                    data.lightmapDir = lightmapData.lightmapDir[i];
                
                if (lightmapData.shadowMask.Length > i)
                    data.shadowMask = lightmapData.shadowMask[i];
                
                newLightmaps[i] = data;
            }
            LightmapSettings.lightmaps = newLightmaps;
            Debug.Log("Lightmaps applied!");
        }
    }
}
