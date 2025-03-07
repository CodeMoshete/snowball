using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationActionData : CustomAction
{
	public List<string> StringTriggers;
	public List<Animator> StringTriggerObjects;

	public List<int> IntTriggers;
	public List<string> IntTriggerNames;
	public List<Animator> IntTriggerObjects;

	public List<bool> BoolTriggers;
	public List<string> BoolTriggerNames;
	public List<Animator> BoolTriggerObjects;

	public override void Initiate()
	{
		int count = StringTriggers.Count;
		if (count == StringTriggerObjects.Count)
		{
			for (int i = 0; i < count; ++i)
			{
				Animator currentAnimator = StringTriggerObjects [i];
				currentAnimator.SetTrigger (StringTriggers [i]);
			}
		}

		count = IntTriggers.Count;
		if (count == IntTriggerObjects.Count)
		{
			for (int i = 0; i < count; ++i)
			{
				Animator currentAnimator = IntTriggerObjects [i];
                currentAnimator.SetInteger(IntTriggerNames[i], IntTriggers[i]);
			}
		}

		count = BoolTriggers.Count;
		if (count == BoolTriggerObjects.Count)
		{
			for (int i = 0; i < count; ++i)
			{
				Animator currentAnimator = BoolTriggerObjects [i];
				currentAnimator.SetBool (BoolTriggerNames[i], BoolTriggers [i]);
			}
		}
	}
}
