using UnityEngine;
using Utils;

public class GetTransformByNameProvider : TransformProvider
{
    // Leave blank in editor to search everywhere.
    public TransformProvider Parent;
    public string TransformName;
    public override Transform GetTransform()
    {
        if (Parent != null)
        {
            Transform parentTransform = Parent.GetTransform();
            if (parentTransform != null)
            {
                GameObject returnObj = UnityUtils.FindGameObject(parentTransform.gameObject, TransformName);
                if (returnObj != null)
                {
                    return returnObj.transform;
                }
            }
        }
        else
        {
            GameObject returnObj = GameObject.Find(TransformName);
            if (returnObj != null)
            {
                return returnObj.transform;
            }
        }
        return null;
    }
}
