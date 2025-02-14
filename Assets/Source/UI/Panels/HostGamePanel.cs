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
        Service.LevelLoader.LoadLevelManifest(OnRemoteManifestLoaded, null);
    }

    private void OnRemoteManifestLoaded(LevelManifestData levelManifest)
    {
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
