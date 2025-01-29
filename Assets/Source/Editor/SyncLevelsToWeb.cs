using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

[Serializable]
public class LevelManifestDataItem
{
    public string Name;
    public uint Version;
}

[Serializable]
public class LevelManifestData
{
    public LevelManifestDataItem[] Levels;
}

public class JsonDownloader : EditorWindow
{
    // private const string MANIFEST_URL = "https://codemoshete.s3.us-east-2.amazonaws.com/snowball/levels/levels-manifest.json";
    private const string MANIFEST_URL = "https://www.codemoshete.com/snowball/levels/levels-manifest.json";
    private const string MANIFEST_LOCAL_PATH = "Levels/levels-manifest.json";
    private const string LEVELS_LOCAL_PATH = "StreamingAssets/Levels";
    private const string LEVELS_DEST_PATH = "public_html/snowball/levels";
    private const string DEFAULT_SYNC_TEXT = "Download remote manifest to generate sync list...";
    private readonly string[] PLATFORMS = {
        "Android",
        "StandaloneLinux64",
        "StandaloneOSX",
        "StandaloneWindows"
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
            DownloadJsonFile(MANIFEST_URL);
        }

        GUILayout.Label("Levels to sync:");
        GUILayout.TextArea(levelsToSyncText, GUILayout.Height(200));

        if (GUILayout.Button("Sync Level Changes"))
        {
            SyncAssets();
        }
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
            UnityEngine.Debug.Log("JSON downloaded successfully!");
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
                UnityEngine.Debug.Log($"Level {localItem.Name} is outdated in remote manifest.");
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

            string destPath = Path.Combine(LEVELS_DEST_PATH, level.Name);

            uploadCommands.Add($"ftp_upload {destPath} {sourcePath}");
            uploadCommands.Add($"ftp_upload {destPath} {manifestSourcePath}");
        }
    }

    private void SyncAssets()
    {
        for (int i = 0, count = levelsToUpdate.Count; i < count; ++i)
        {
            UnityEngine.Debug.Log($"Execute command: {levelsToUpdate[i]}");
            // ExecuteCommand(levelsToUpdate[i]);
        }
        levelsToUpdate.Clear();
        levelsToSyncText = DEFAULT_SYNC_TEXT;
    }

    private string ExecuteCommand(string command)
    {
        Process process = new Process();
        
        // Set up the process start information
        process.StartInfo.FileName = "/bin/bash"; // Use "cmd.exe" for Windows
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
