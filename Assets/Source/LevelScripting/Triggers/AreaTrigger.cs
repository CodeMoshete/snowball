using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AreaTrigger : MonoBehaviour
{
	[System.Serializable]
	public struct TriggerCustomActionParams
	{
		public GameObject Parent;
	}

    public bool DisableOnUse;
	public List<TriggerCustomActionParams> EnterActions;
    public List<TriggerCustomActionParams> ExitActions;
    private bool isEnterUsed;
    private bool isExitUsed;

    public void OnTriggerEnter(Collider collisionObject)
    {
        if (DisableOnUse && isEnterUsed)
            return;

		print ("Collision Detected enter");
		if(collisionObject.tag == "Player")
		{
			for(int i = 0, count = EnterActions.Count; i < count; i++)
			{
                isEnterUsed = true;
				EnterActions[i].Parent.GetComponent<CustomAction>().Initiate();
			}
		}
	}

    public void OnTriggerExit(Collider other)
    {
        if (DisableOnUse && isExitUsed)
            return;

        print("Collision Detected exit");
        if (other.tag == "Player")
        {
            for (int i = 0, count = ExitActions.Count; i < count; i++)
            {
                isExitUsed = true;
                ExitActions[i].Parent.GetComponent<CustomAction>().Initiate();
            }
        }
    }
}
