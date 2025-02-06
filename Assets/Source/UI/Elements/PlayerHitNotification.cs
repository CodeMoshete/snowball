using TMPro;
using UnityEngine;

public class PlayerHitNotification : MonoBehaviour
{
    public TMP_Text HitNotificationText;

    public void ShowNotification(string text)
    {
        ShowNotification(text, Color.white);
    }

    public void ShowNotification(string text, Color textColor)
    {
        HitNotificationText.text = text;
        HitNotificationText.color = textColor;
    }
}
