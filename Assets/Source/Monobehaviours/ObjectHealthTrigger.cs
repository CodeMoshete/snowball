using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DamageStage
{
    public int StageHealth;
    public GameObject StageObject;
}

public class ObjectHealthTrigger : MonoBehaviour
{
    public int Health;
    public CustomAction OnHealthDepleted;
    public List<DamageStage> DamageStages;
    public GameObject CurrentDamageState;

    public void OnHit(int damage = 1)
    {
        Health -= damage;
        Debug.Log($"Health now set to {Health}");

        if (Health <= 0 && OnHealthDepleted != null)
        {
            OnHealthDepleted.Initiate();
            return;
        }

        for (int i = 0, numStages = DamageStages.Count; i < numStages; ++i)
        {
            DamageStage stage = DamageStages[i];
            if (stage.StageHealth == Health)
            {
                CurrentDamageState.SetActive(false);
                CurrentDamageState = stage.StageObject;
                CurrentDamageState.SetActive(true);
            }
        }
    }
}
