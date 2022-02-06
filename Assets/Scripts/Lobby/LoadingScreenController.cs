using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using GameManagement;
using Network;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Lobby
{
    public class LoadingScreenController : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public static LoadingScreenController Instance;
    
    
        public const byte GameLoadingEventCode = 10;
        public const byte GameStartingEventCode = 11;

        [SerializeField] private LoadingScreenMovement loadingMovementController;

        [SerializeField] private List<LoadingIndicator> availableIndicators = new();
        private Dictionary<GameData.Team, LoadingIndicator> loadingIndicators = new(4);

        [SerializeField] private CanvasGroup canvasGroup;
    
    
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

            int i = 0;
            foreach (LoadingIndicator indicator in availableIndicators)
            {
                loadingIndicators.Add((GameData.Team)i++, indicator);
            }
            
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

            switch (eventCode)
            {
                case GameLoadingEventCode:
                {
                    loadingMovementController.Close();
                    canvasGroup.blocksRaycasts = true;
                
                    foreach (Player p in PhotonNetwork.PlayerList)
                    {
                        GameData.Team t = (GameData.Team) p.CustomProperties["Team"];
                    
                        loadingIndicators[t].gameObject.SetActive(true);
                        loadingIndicators[t].Show();
                        loadingIndicators[t].PlayerName = p.NickName;
                    }

                    break;
                }
                case GameStartingEventCode:
                    loadingMovementController.Open();
                    canvasGroup.blocksRaycasts = false;
                    loadingIndicators.Values.Where(i => i.gameObject.activeSelf).ToList().ForEach(indicator =>
                    {
                        indicator.Hide();
                    });
                    break;
                case GameStateController.PlayerLoadedEventCode:
                {
                    GameData.Team team = (GameData.Team)((object[])photonEvent.CustomData)[0];
                    
                    loadingIndicators[team].LoadingFinished = true;
                    break;
                }
                default:
                    break;
            }
        }
        
    }
}
