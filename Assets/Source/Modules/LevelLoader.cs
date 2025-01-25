using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LevelLoader : MonoBehaviour
{
    private const bool DEBUG_DOWNLOAD_LOCAL = false;
    private readonly string LOCAL_ASSET_PREFIX = $"{Application.streamingAssetsPath}/Levels";
    private const string REMOTE_ASSET_PREFIX = "https://www.codemoshete.com/snowball/levels";

#if UNITY_STANDALONE_LINUX
    private const string PLATFORM_DIR = "StandaloneLinux64";
#elif UNITY_STANDALONE_WIN
    private const string PLATFORM_DIR = "StandaloneWindows";
#elif UNITY_STANDALONE_OSX
    private const string PLATFORM_DIR = "StandaloneOSX";
#elif UNITY_ANDROID
    private const string PLATFORM_DIR = "Android";
#elif UNITY_IOS
    private const string PLATFORM_DIR = "iOS";
#endif

    private const string PREFAB_SUFFIX = ".prefab";

    private string levelName; // Name of the asset bundle (lowercase)
    private Action<GameObject> onDownloadSuccess;

    public void LoadLevel(string levelName, Action<GameObject> onDownloadSuccess, Action onDownloadFailure)
    {
        this.levelName = levelName.ToLower();
        this.onDownloadSuccess = onDownloadSuccess;

        // Load the asset bundle
        StartCoroutine(GetAssetBundle(this.levelName, OnBundleDownloadComplete, onDownloadFailure));
    }

    private IEnumerator GetAssetBundle(string levelName, Action<AssetBundle> onComplete, Action onFail)
    {
        string urlPrefix = (Constants.IS_OFFLINE_DEBUG || DEBUG_DOWNLOAD_LOCAL) ? LOCAL_ASSET_PREFIX : REMOTE_ASSET_PREFIX;
        string resourceUrl = $"{urlPrefix}/{PLATFORM_DIR}/{levelName}";
        Debug.Log($"Downloading level from {resourceUrl}");
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(resourceUrl);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.LogError($"Asset download error: {www.error}");
            onFail();
        }
        else {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
            onComplete(bundle);
        }
    }

    private void OnBundleDownloadComplete(AssetBundle bundle)
    {
        Debug.Log("Level AssetBundle downloaded successfully!");

        // Load the prefab
        string prefabName = string.Empty;
        string[] assetNames = bundle.GetAllAssetNames();
        for (int i = 0, count = assetNames.Length; i < count; ++i)
        {
            Debug.Log($"Checking asset named: {assetNames[i]}");
            if (assetNames[i].EndsWith(PREFAB_SUFFIX))
            {
                prefabName = assetNames[i];
                break;
            }
        }

        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError($"No prefabs found in level asset bundle for {levelName}");
            return;
        }

        GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
        bundle.Unload(false);

        if (prefab == null)
        {
            Debug.LogError($"Prefab {prefabName} not found in AssetBundle {levelName}.");
            return;
        }

        Debug.Log("Level loaded successfully!");
        onDownloadSuccess(prefab);
    }

    private void OnDestroy()
    {
        onDownloadSuccess = null;
    }
}
