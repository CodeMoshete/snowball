using UnityEngine;

public class ExplicitStringProviderAction : CustomActionStringProvider
{
    public string StringValue;

    public override string GetStringValue()
    {
        return StringValue;
    } 
}
