using UnityEngine;

public class CameraArmature : MonoBehaviour
{
    private Transform referenceTarget;

    public void Initialize(Transform referenceTarget)
    {
        this.referenceTarget = referenceTarget;
    }

    private void Update()
    {
        if (referenceTarget == null)
            return;

        
    }
}
