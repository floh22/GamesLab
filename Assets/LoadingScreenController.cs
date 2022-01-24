using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class LoadingScreenController : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static LoadingScreenController Instance;
    
    
    public const byte GameLoadingEventCode = 10;
    public const byte GameStartingEventCode = 11;
    
    [SerializeField] private Image loadingScreen;
    [SerializeField] private VideoPlayer transitionPlayer;
    
    
    public static void SendGameLoadingEvent()
    {
        RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.All }; 
        PhotonNetwork.RaiseEvent(GameLoadingEventCode, Array.Empty<object>(), raiseEventOptions, SendOptions.SendReliable);
    }
    
    public static void SendGameStartingEvent()
    {
        RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.All }; 
        PhotonNetwork.RaiseEvent(GameStartingEventCode, Array.Empty<object>(), raiseEventOptions, SendOptions.SendReliable);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(this);
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

        if (eventCode == GameLoadingEventCode)
        {
            StartCoroutine(PlayTransition(true));
        }

        if (eventCode == GameStartingEventCode)
        {
            StartCoroutine(PlayTransition(false));
        }
    }


    private IEnumerator PlayTransition(bool loadingScreenVisibilityAfter)
    {
        transitionPlayer.Play();
        yield return new WaitForSeconds(0.4f);
        loadingScreen.enabled = loadingScreenVisibilityAfter;

    }
}
