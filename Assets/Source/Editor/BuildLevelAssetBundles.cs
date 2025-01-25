using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildLevelAssetBundles : MonoBehaviour
{
    private const string LEVELS_FOLDER_PATH = "Assets/Levels";
    private const string OUTPUT_FOLDER_PATH = "Assets/StreamingAssets/Levels";

    [MenuItem("Tools/Build Level Bundles - All")]
    public static void BuildBundlesForAllPlatforms()
    {
        BuildBundlesForPlatform(BuildTarget.StandaloneWindows);
        BuildBundlesForPlatform(BuildTarget.Android);
        BuildBundlesForPlatform(BuildTarget.StandaloneLinux64);
        BuildBundlesForPlatform(BuildTarget.StandaloneOSX);
    }

    [MenuItem("Tools/Build Level Bundles - Current")]
    public static void BuildBundlesForCurrentPlatform()
    {
        BuildTarget currentTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildBundlesForPlatform(currentTarget);
    }

    [MenuItem("Tools/Build Level Bundles - Windows")]
    public static void BuildBundlesForWindows()
    {
        BuildBundlesForPlatform(BuildTarget.StandaloneWindows);
    }

    [MenuItem("Tools/Build Level Bundles - Android")]
    public static void BuildBundlesForAndroid()
    {
        BuildBundlesForPlatform(BuildTarget.Android);
    }

    [MenuItem("Tools/Build Level Bundles - Linux")]
    public static void BuildBundlesForLinux()
    {
        BuildBundlesForPlatform(BuildTarget.StandaloneLinux64);
    }

    [MenuItem("Tools/Build Level Bundles - Mac")]
    public static void BuildBundlesForMac()
    {
        BuildBundlesForPlatform(BuildTarget.StandaloneOSX);
    }

    private static void BuildBundlesForPlatform(BuildTarget platform)
    {
        // Ensure the output directories exist
        CreateOutputFolderForPlatform(platform);

        // Get all subdirectories in the Levels folder
        var levelDirectories = Directory.GetDirectories(LEVELS_FOLDER_PATH);

        if (levelDirectories.Length == 0)
        {
            Debug.LogWarning("No level folders found in Assets/Levels.");
            return;
        }

        // Prepare a list to hold asset bundle build configurations
        var bundleBuilds = new AssetBundleBuild[levelDirectories.Length];

        // Iterate through each level folder and prepare the bundle builds
        for (int i = 0; i < levelDirectories.Length; i++)
        {
            string folderPath = levelDirectories[i];
            string levelName = Path.GetFileName(folderPath);

            // Get all assets in the level folder
            string[] assetPaths = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            assetPaths = FilterValidAssetPaths(assetPaths);

            if (assetPaths.Length == 0)
            {
                Debug.LogWarning($"No valid assets found in folder: {folderPath}");
                continue;
            }

            // Configure the asset bundle build for this level
            bundleBuilds[i] = new AssetBundleBuild
            {
                assetBundleName = levelName.ToLower(), // Asset bundle name in lowercase
                assetNames = assetPaths
            };

            Debug.Log($"Prepared bundle for level: {levelName}, Assets: {assetPaths.Length}");
        }

        // Build the asset bundles
        BuildPipeline.BuildAssetBundles($"{OUTPUT_FOLDER_PATH}/{platform}", bundleBuilds, BuildAssetBundleOptions.None, platform);

        Debug.Log("Level asset bundles built successfully!");
        AssetDatabase.Refresh();
    }

    private static void CreateOutputFolderForPlatform(BuildTarget platform)
    {
        string outputFolderPath = $"{OUTPUT_FOLDER_PATH}/{platform.ToString()}";
        // Ensure the output directory exists
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }
    }

    /// <summary>
    /// Filters valid asset paths (ignores meta files and unsupported types).
    /// </summary>
    private static string[] FilterValidAssetPaths(string[] paths)
    {
        return System.Array.FindAll(paths, path =>
        {
            string extension = Path.GetExtension(path);
            return !string.IsNullOrEmpty(extension) && extension != ".meta";
        });
    }
}
