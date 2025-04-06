using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[Billboard]: Main camera not found. Please ensure there is a camera tagged as 'MainCamera' in the scene.");
        }
    }

    void Update()
    {
        transform.LookAt(mainCamera.transform.position);
        transform.Rotate(0, 180, 0); // Rotate to face the camera correctly

        // Lock the rotation on the x and z axes
        Vector3 rotation = transform.eulerAngles;
        rotation.x = 0;
        rotation.z = 0;
        transform.eulerAngles = rotation;
    }
}
