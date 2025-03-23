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
    
    [ColorUsage(true, true)]
    public Color EnvironmentColor;
    
    public Color FogColor;
    public float FogDensity;
    public Material SkyboxMaterial;
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

        if (EnvironmentColor != Color.white)
        {
            RenderSettings.ambientLight = EnvironmentColor;
        }

        RenderSettings.fog = IsFogEnabled;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogDensity = FogDensity;
        RenderSettings.skybox = SkyboxMaterial;
    }
}
