using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

public class HostGamePanel : MonoBehaviour
{
    public TMP_Dropdown LevelSelectDropdown;

    public string SelectedLevel
    {
        get
        {
            return LevelSelectDropdown.options[LevelSelectDropdown.value].text;
        }
    }

    public TMP_InputField NameField;
    public Button BackButton;
    private Action onBackPressed;

    public void Initialize(Action onBackPressed)
    {
        this.onBackPressed = onBackPressed;
        BackButton.onClick.AddListener(OnBackButtonPressed);
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        LevelSelectDropdown.ClearOptions();
        StartCoroutine(DownloadLevelsManifest());
    }

    private IEnumerator DownloadLevelsManifest()
    {
        UnityWebRequest request = UnityWebRequest.Get(Constants.REMOTE_MANIFEST_URL);

        yield return request.SendWebRequest();

        string remoteJsonData;
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Failed to download JSON: {request.error}");
        }
        else
        {
            remoteJsonData = request.downloadHandler.text;
            Debug.Log("JSON downloaded successfully!");
            OnRemoteManifestLoaded(remoteJsonData);
        }
    }

    private void OnRemoteManifestLoaded(string json)
    {
        LevelManifestData levelManifest = JsonUtility.FromJson<LevelManifestData>(json);
        List<string> options = new List<string>();
        for (int i = 0, count = levelManifest.Levels.Length; i < count; ++i)
        {
            LevelManifestDataItem levelData = levelManifest.Levels[i];
            options.Add(levelData.Name);
        }
        LevelSelectDropdown.AddOptions(options);
    }

    private void OnBackButtonPressed()
    {
        onBackPressed();
        gameObject.SetActive(false);
    }
}
