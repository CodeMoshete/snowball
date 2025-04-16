using TMPro;
using UnityEngine;

public class PlayerEntityTeamColor : MonoBehaviour
{
    public GameObject TeamColorObject;
    
    // For use with objects containing multiple materials.
    public string MaterialName; 

    public void SetTeamColor(Color teamColor)
    {
        Material mat = null;
        if (TeamColorObject != null)
        {
            if (MaterialName != null && MaterialName.Length > 0)
            {
                Renderer[] renderers = TeamColorObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    // Iterate over materials
                    foreach (Material m in r.materials)
                    {
                        if (m.name.Contains(MaterialName))
                        {
                            mat = m;
                            break;
                        }
                    }
                }
            }
            else
            {
                mat = TeamColorObject.GetComponent<Renderer>().material;
            }

            
            if (mat != null)
            {
                mat.color = teamColor;
            }

            TMP_Text textField = TeamColorObject.GetComponentInChildren<TMP_Text>();
            if (textField != null)
            {
                textField.color = teamColor;
            }
        }
    }
}
