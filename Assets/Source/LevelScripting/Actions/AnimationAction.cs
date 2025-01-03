using System.Collections.Generic;
using UnityEngine;

public class AnimationAction : CustomAction
{
    [System.Serializable]
    public struct IntTrigger
    {
        public Animator Target;
        public string Key;
        public int Value;
    }

    [System.Serializable]
    public struct BoolTrigger
    {
        public Animator Target;
        public string Key;
        public bool Value;
    }

    [System.Serializable]
    public struct StringTrigger
    {
        public string Trigger;
        public Animator Target;
    }

    [System.Serializable]
    public struct DirectPlay
    {
        public string AnimationName;
        public Animator Target;
    }

    public List<IntTrigger> IntTriggers;
    public List<BoolTrigger> BoolTriggers;
    public List<StringTrigger> StringTriggers;
    public List<DirectPlay> DirectPlays;

    public CustomAction OnComplete;

    public override void Initiate()
    {
        int count = IntTriggers.Count;
        for (int i = 0; i < count; ++i)
        {
            IntTriggers[i].Target.SetInteger(IntTriggers[i].Key, IntTriggers[i].Value);
        }

        count = BoolTriggers.Count;
        for (int i = 0; i < count; ++i)
        {
            BoolTriggers[i].Target.SetBool(BoolTriggers[i].Key, BoolTriggers[i].Value);
        }

        count = StringTriggers.Count;
        for (int i = 0; i < count; ++i)
        {
            StringTriggers[i].Target.SetTrigger(StringTriggers[i].Trigger);
        }

        count = DirectPlays.Count;
        for (int i = 0; i < count; ++i)
        {
            DirectPlays[i].Target.Play(DirectPlays[i].AnimationName);
        }

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
