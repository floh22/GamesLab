using System;
using System.Collections.Generic;
using System.Linq;
using GameManagement;
using Lobby;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    public class PersistentData : MonoBehaviourPunCallbacks
    {
        public static PersistentData Instance;
        public static GameData.Team? Team = null;

        public static bool ConnectedToServer;
        
        public static List<RoomInfo> RoomList = new();
        
        // Start is called before the first frame update
        void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            RoomList = roomList.Where(r => r.RemovedFromList == false && r.PlayerCount > 0).ToList();
            ConnectedToServer = true;
            Debug.Log($"{PersistentData.RoomList.Count} rooms available");
        }
        
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (!otherPlayer.IsMasterClient)
                return;
            
            CheckEscapeToLobby();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            base.OnMasterClientSwitched(newMasterClient);

            CheckEscapeToLobby();
        }


        private void CheckEscapeToLobby()
        {
            Debug.Log("Master left");
            //Is ingame
            if (SceneManager.GetActiveScene().buildIndex == 1)
            {
                LoadingScreenController.Instance.OnMasterLeave();
            }
        }
    }
}
