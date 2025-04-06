using TMPro;
using UnityEngine;

public class PlayerEntityTeamColor : MonoBehaviour
{
    public GameObject TeamColorObject;

    public void SetTeamColor(Color teamColor)
    {
        if (TeamColorObject != null)
        {
            Renderer renderer = TeamColorObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                if (material != null)
                {
                    material.color = teamColor;
                }
            }

            TMP_Text textField = TeamColorObject.GetComponentInChildren<TMP_Text>();
            if (textField != null)
            {
                textField.color = teamColor;
            }
        }
    }
}
