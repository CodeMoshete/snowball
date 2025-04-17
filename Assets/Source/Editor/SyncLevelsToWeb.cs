using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

public class JsonDownloader : EditorWindow
{
    // private const string MANIFEST_URL = "https://codemoshete.s3.us-east-2.amazonaws.com/snowball/levels/levels-manifest.json";
    // private const string MANIFEST_URL = "https://www.codemoshete.com/snowball/levels/levels-manifest.json";
    private const string MANIFEST_LOCAL_PATH = "Levels/levels-manifest.json";
    private const string LEVELS_LOCAL_PATH = "StreamingAssets/Levels";
    private const string LEVELS_DEST_PATH = "public_html/snowball/levels";
    private const string DEFAULT_SYNC_TEXT = "Download remote manifest to generate sync list...";
    private readonly string[] PLATFORMS = {
        "Android",
        "StandaloneLinux64",
        "StandaloneOSX",
        "StandaloneWindows64"
    };
    private string remoteJsonData = "No data downloaded yet.";

    private List<string> levelsToUpdate = new List<string>();
    private string levelsToSyncText = DEFAULT_SYNC_TEXT;

    [MenuItem("Tools/Sync Levels to Web")]
    public static void ShowWindow()
    {
        GetWindow<JsonDownloader>("Manifest Updater");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Download Remote Manifest"))
        {
            DownloadJsonFile(Constants.REMOTE_MANIFEST_URL);
        }

        if (GUILayout.Button("Sync for Local Testing"))
        {
            SyncForLocalTesting();
            UnityEngine.Debug.Log("Copied local manifest for local testing purposes.");
        }

        GUILayout.Label("Levels to sync:");
        GUILayout.TextArea(levelsToSyncText, GUILayout.Height(200));

        if (GUILayout.Button("Sync Level Changes"))
        {
            SyncAssets();
        }
    }

    // Copy Assets/Levels/levels-manifest.json to StreamingAssets/Levels/debug-levels-manifest.json
    private void SyncForLocalTesting()
    {
        string sourcePath = Path.Combine(Application.dataPath, MANIFEST_LOCAL_PATH);
        string destPath = Path.Combine(Application.dataPath, LEVELS_LOCAL_PATH, "debug-levels-manifest.json");
        File.Copy(sourcePath, destPath, true);

        // Get a list of all sub-folders inside Assets/Levels
        string levelFolderPath = Path.Combine(Application.dataPath, "Levels");
        string[] levelFolders = Directory.GetDirectories(levelFolderPath);
        for (int i = 0, count = levelFolders.Length; i < count; ++i)
        {
            string levelFolder = levelFolders[i];
            string levelFolderName = Path.GetFileName(levelFolder);
            UnityEngine.Debug.Log($"Scanning level folder {levelFolder}, name: {levelFolderName}");
            // Scan for prefab files in each sub-folder
            string[] prefabFiles = Directory.GetFiles(levelFolder, "*.prefab");
            if (prefabFiles.Length == 1)
            {
                // Copy the prefab into Resources/Levels
                string prefabPath = Path.Combine(levelFolderPath, prefabFiles[0]);
                string destinationPath = Path.Combine(Application.dataPath, "Resources", "Levels", $"{levelFolderName.ToLower()}.prefab");

                // Copy prefab file to destination path.
                File.Copy(prefabPath, destinationPath, true);
            }

            string[] sceneFiles = Directory.GetFiles(levelFolder, "*.unity");
            if (sceneFiles.Length == 1)
            {
                // Copy the prefab into Resources/Levels
                string scenePath = Path.Combine(levelFolderPath, sceneFiles[0]);
                string destinationPath = Path.Combine(Application.dataPath, "Resources", "Levels", $"{levelFolderName.ToLower()}.unity");

                // Copy prefab file to destination path.
                File.Copy(scenePath, destinationPath, true);
            }
        }
        AssetDatabase.Refresh();
    }

    private void DownloadJsonFile(string url)
    {
        // Start the download coroutine
        EditorCoroutineUtility.StartCoroutineOwnerless(DownloadJsonCoroutine(url));
    }

    private IEnumerator DownloadJsonCoroutine(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            UnityEngine.Debug.LogError($"Failed to download JSON: {request.error}");
            remoteJsonData = $"Error: {request.error}";
        }
        else
        {
            remoteJsonData = request.downloadHandler.text;
            UnityEngine.Debug.Log($"JSON downloaded successfully!:\n{remoteJsonData}");
            ParseAndCompareJson();
        }

        Repaint(); // Refresh the editor window to show the updated data
    }

    private void ParseAndCompareJson()
    {
        LevelManifestData remoteManifest = JsonUtility.FromJson<LevelManifestData>(remoteJsonData);

        string jsonFilePath = Path.Combine(Application.dataPath, MANIFEST_LOCAL_PATH);
        string localJsonData = File.ReadAllText(jsonFilePath);
        UnityEngine.Debug.Log($"Loaded JSON from {jsonFilePath}:\n{localJsonData}");
        LevelManifestData localManifest = JsonUtility.FromJson<LevelManifestData>(localJsonData);
        
        UnityEngine.Debug.Log("All JSON Parsed!");

        levelsToUpdate = new List<string>();
        levelsToSyncText = string.Empty;
        for (int i = 0, count = localManifest.Levels.Length; i < count; ++i)
        {
            LevelManifestDataItem localItem = localManifest.Levels[i];
            LevelManifestDataItem remoteItem = FindLevelInManifest(remoteManifest, localItem.Name);

            if (remoteItem == null)
            {
                UnityEngine.Debug.Log($"Level {localItem.Name} does not exist in remote manifest.");
                // levelsToUpdate.Add(localItem.Name);
                AddLevelToUpload(localItem, levelsToUpdate);
            }
            else if (remoteItem.Version < localItem.Version)
            {
                UnityEngine.Debug.Log($"Level {localItem.Name} is outdated in remote manifest ({remoteItem.Version} < {localItem.Version}).");
                // levelsToUpdate.Add(localItem.Name);
                AddLevelToUpload(localItem, levelsToUpdate);
            }
            else if (remoteItem.Version > localItem.Version)
            {
                UnityEngine.Debug.LogError($"Remote version for {localItem.Name} is greater than local version! Update your manifest!");
            }
        }

        if (levelsToUpdate.Count > 0)
        {
            levelsToUpdate.Add($"ftp_upload {LEVELS_DEST_PATH} {jsonFilePath}");
        }

        string commandsString = "Levels to update:\n";
        for (int i = 0, count = levelsToUpdate.Count; i < count; ++i)
        {
            commandsString += $"{levelsToUpdate[i]}\n";
        }
        UnityEngine.Debug.Log(commandsString);
    }

    private void AddLevelToUpload(LevelManifestDataItem level, List<string> uploadCommands)
    {
        levelsToSyncText += $"{level.Name}\n";
        for (int i = 0, count = PLATFORMS.Length; i < count; ++i)
        {
            string sourcePath = Path.Combine(Application.dataPath, LEVELS_LOCAL_PATH, PLATFORMS[i], level.Name);
            string manifestSourcePath = Path.Combine(Application.dataPath, LEVELS_LOCAL_PATH, PLATFORMS[i], $"{level.Name}.manifest");

            string destPath = Path.Combine(LEVELS_DEST_PATH, PLATFORMS[i]);

            uploadCommands.Add($"ftp_upload {destPath} {sourcePath}");
            uploadCommands.Add($"ftp_upload {destPath} {manifestSourcePath}");
        }
    }

    private void SyncAssets()
    {
        for (int i = 0, count = levelsToUpdate.Count; i < count; ++i)
        {
            UnityEngine.Debug.Log($"Execute command: {levelsToUpdate[i]}");
            ExecuteCommand(levelsToUpdate[i]);
        }
        levelsToUpdate.Clear();
        levelsToSyncText = DEFAULT_SYNC_TEXT;
    }

    private string ExecuteCommand(string command)
    {
        Process process = new Process();
        
        // Set up the process start information
        process.StartInfo.FileName = "/bin/zsh"; // Use "cmd.exe" for Windows
        process.StartInfo.Arguments = $"-c \"{command}\""; // Wrap command for proper execution
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        
        // Start process and read output
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.Close();

        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogError($"Error: {error}");
        }

        return output;
    }

    private LevelManifestDataItem FindLevelInManifest(LevelManifestData manifest, string levelName)
    {
        for (int i = 0, count = manifest.Levels.Length; i < count; ++i)
        {
            LevelManifestDataItem item = manifest.Levels[i];
            if (item.Name == levelName)
            {
                return(item);
            }
        }
        return null;
    }
}
