
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class AutoObjectParenter : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte ParentToObjectEventCode = 9;

    public static void SendParentEvent(GameObject objectToParent)
    {
        object[] content = {objectToParent.GetComponent<PhotonView>().ViewID};
        RaiseEventOptions raiseEventOptions = new() {Receivers = ReceiverGroup.All};
        PhotonNetwork.RaiseEvent(ParentToObjectEventCode, content, raiseEventOptions, SendOptions.SendReliable);
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        if (eventCode == ParentToObjectEventCode)
        {
            object[] data = (object[]) photonEvent.CustomData;
            gameObject.transform.parent = PhotonView.Find((int)data[0]).gameObject.transform;
        }
    }
}