using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DisplayMessageAction : CustomNetworkAction
{
    public string Message;
    public List<CustomActionStringProvider> DynamicStringProviders;
    public float DisplayTime;
    public PlayerEntityProvider TargetPlayer;
    public CustomAction OnComplete;
    public CustomAction OnMessageDone;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        string outputMessage = Message;
        if (DynamicStringProviders != null && DynamicStringProviders.Count > 0)
        {
            Debug.Log($"[DisplayMessageAction]: Initial dynamic message: {outputMessage}");
            string[] dynamicStrings = DynamicStringProviders.ConvertAll(provider => provider.GetStringValue()).ToArray();
            outputMessage = string.Format(Message, dynamicStrings);
            Debug.Log($"[DisplayMessageAction]: Result dynamic message: {outputMessage}");
        }
        
        Debug.Log($"Display message: {outputMessage}");
        if (TargetPlayer == null)
        {
            Service.EventManager.SendEvent(EventId.DisplayMessage, outputMessage);
            Service.TimerManager.CreateTimer(DisplayTime, DisplayTimeExpired, null);
        }
        else
        {
            PlayerEntity playerEntity = TargetPlayer.GetPlayerEntity();
            if (playerEntity != null)
            {
                ulong clientId = playerEntity.OwnerClientId;
                BaseRpcTarget rpcParams = NetworkManager.Singleton.RpcTarget.Single(clientId, RpcTargetUse.Temp);
                playerEntity.DisplayMessageToPlayerRpc(outputMessage, DisplayTime, rpcParams);
            }
        }
            
        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }

    private void DisplayTimeExpired(object cookie)
    {
        Service.EventManager.SendEvent(EventId.HideMessage, null);
        if (OnMessageDone != null)
        {
            OnMessageDone.Initiate();
        }
    }
}
