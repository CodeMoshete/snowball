using UnityEngine;

public enum BlackboardValueType
{
    Bool,
    Int,
    String
}

public class BlackboardValueStringProvider : CustomActionStringProvider
{
    public string BlackboardKey;
    public BlackboardValueType ValueType;

    public override string GetStringValue()
    {
        string output = string.Empty;
        switch (ValueType)
        {
            case BlackboardValueType.Bool:
                output = GameBlackboard.Instance.GetBool(BlackboardKey).ToString();
                break;
            case BlackboardValueType.Int:
                int outputVal = GameBlackboard.Instance.GetInt(BlackboardKey);
                Debug.Log($"[BlackboardValueStringProvider]: Initial blackboard value: {outputVal}");
                output = outputVal.ToString();
                break;
            case BlackboardValueType.String:
                output = GameBlackboard.Instance.GetString(BlackboardKey);
                break;
        }
        Debug.Log($"[BlackboardValueStringProvider]: Final blackboard value: {output}");
        return output;
    }
}
