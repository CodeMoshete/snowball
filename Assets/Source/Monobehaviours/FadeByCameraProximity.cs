using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utils;

public class FadeByCameraProximity : MonoBehaviour
{
    public float FarThreshold = 10f; // Distance at which the object is fully faded
    public float NearThreshold = 5f; // Distance at which the object is fully visible
    
    private Camera mainCamera;
    private float sqrFarThreshold;
    private float sqrNearThreshold;
    private Material localMaterial;
    private TMP_Text textField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        sqrFarThreshold = FarThreshold * FarThreshold;
        sqrNearThreshold = NearThreshold * NearThreshold;

        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            localMaterial = renderer.material;
        }

        List<TMP_Text> textFields = UnityUtils.FindAllComponentsInChildren<TMP_Text>(gameObject);
        if (textFields.Count > 0)
        {
            textField = textFields[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        float sqrDistance = (transform.position - mainCamera.transform.position).sqrMagnitude;
        float alpha = 1f;
        if (sqrDistance > sqrFarThreshold)
        {
            alpha = 0f; // Fully faded
        }
        else if (sqrDistance < sqrNearThreshold)
        {
            alpha = 1f; // Fully visible
        }
        else
        {
            // Interpolate between 0 and 1 based on distance
            alpha = Mathf.Lerp(1f, 0f, (sqrDistance - sqrNearThreshold) / (sqrFarThreshold - sqrNearThreshold));
        }

        if (localMaterial != null)
        {
            Color color = localMaterial.color;
            color.a = alpha;
            localMaterial.color = color;
        }

        if (textField != null)
        {
            Color textColor = textField.color;
            textColor.a = alpha;
            textField.color = textColor;
        }
    }
}
