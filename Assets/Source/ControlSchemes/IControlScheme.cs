using System;
using UnityEngine;

public interface IControlScheme
{
    void Initialize(
        Action<Vector2> onUpdateLook, 
        Action<Vector2> onUpdateMovement, 
        Action onThrow, 
        Action onJump, 
        Action onSpawnWall,
        Action onCycleNextWall,
        Action onCyclePrevWall,
        Action onEscape);
    
    void Dispose();
}
