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
        }
    }
}
