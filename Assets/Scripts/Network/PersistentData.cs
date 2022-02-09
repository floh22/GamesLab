using System;
using System.Collections.Generic;
using System.Linq;
using GameManagement;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Network
{
    public class PersistentData : MonoBehaviourPunCallbacks
    {
        public static GameData.Team? Team = null;

        public static bool ConnectedToServer;
        
        public static List<RoomInfo> RoomList = new();
        
        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            RoomList = roomList.Where(r => r.RemovedFromList == false && r.PlayerCount > 0).ToList();
            ConnectedToServer = true;
            Debug.Log($"{PersistentData.RoomList.Count} rooms available");
        }
    
    }
}
