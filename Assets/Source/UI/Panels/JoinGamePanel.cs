using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class JoinGamePanel : MonoBehaviour
{
    public TMP_InputField NameField;
    public Button BackButton;
    private Action onBackPressed;

    public void Initialize(Action onBack)
    {
        onBackPressed = onBack;
        BackButton.onClick.AddListener(OnBackPressed);
    }

    private void OnBackPressed()
    {
        gameObject.SetActive(false);
        onBackPressed();
    }
}
