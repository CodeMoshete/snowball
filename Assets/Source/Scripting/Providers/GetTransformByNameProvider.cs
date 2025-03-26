using UnityEngine;
using Utils;

public class GetTransformByNameProvider : TransformProvider
{
    // Leave blank in editor to search everywhere.
    public TransformProvider Parent;
    public string TransformName;

    private Transform _cachedTransform;
    private Transform cachedTransform
    {
        get
        {
            if (_cachedTransform == null)
            {
                if (Parent != null)
                {
                    Transform parentTransform = Parent.GetTransform();
                    if (parentTransform != null)
                    {
                        GameObject returnObj = UnityUtils.FindGameObject(parentTransform.gameObject, TransformName);
                        if (returnObj != null)
                        {
                            _cachedTransform = returnObj.transform;
                        }
                    }
                }
                else
                {
                    GameObject returnObj = GameObject.Find(TransformName);
                    if (returnObj != null)
                    {
                        _cachedTransform = returnObj.transform;
                    }
                }
            }
            return _cachedTransform;
        }
    }

    public override Transform GetTransform()
    {
        return cachedTransform;
    }

    public override Vector3 GetTransformPosition()
    {
        return cachedTransform.position;
    }

    public override Vector3 GetTransformRotation()
    {
        return cachedTransform.eulerAngles;
    }
}
