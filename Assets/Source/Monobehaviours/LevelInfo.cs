using UnityEngine;

public class LevelInfo : MonoBehaviour
{
    public string LevelDisplayName;
    
    // Make this display as a multi-line text field in the inspector
    [Multiline]
    public string LevelDescription;
}
