using System;
using System.Collections.Generic;
using System.Linq;
using GameManagement;
using Lobby;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Network
{
    public class LauncherController : MonoBehaviourPunCallbacks
    {
        #region Private Serializable Fields

        /// <summary>
        /// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
        /// </summary>
        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField]
        private byte maxPlayersPerRoom = 2;
    
        /// <summary>
        /// This client's version number. Users are separated from each other by gameVersion, allows breaking changes
        /// </summary>
        [Tooltip("This client's version number. Users are separated from each other by gameVersion, allows breaking changes")]
        public StringVersion GameVersion { get; } = new(1, 0, 0);

        [SerializeField]
        private Button joinButton;

        [SerializeField] private TMP_InputField playerNameField;

        [SerializeField] private TMP_Text connectionInfo;
        
        [SerializeField] private TMP_Text connectionStatus;

        [SerializeField] private Button forceStart;

        [SerializeField] private PersistentData persistentData;


        [Header("Scene Data")] [SerializeField]
        private List<GameObject> playerLights;

        [SerializeField] private List<GameObject> playerObjects;
        [SerializeField] private GameObject slenderman;

        [SerializeField] private LobbyCameraController cameraController;

        #endregion


        #region Private Fields

        private List<RoomInfo> RoomList = new();


        private bool InLobby;
        private bool IsReady;
        private bool JoinWhenReady;
        #endregion


        private void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        // Start is called before the first frame update
        void Start()
        {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = GameVersion.ToString();
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    
        /// <summary>
        /// Start the connection process.
        /// - If already connected, we attempt joining a random room
        /// - if not yet connected, Connect this application instance to Photon Cloud Network
        /// </summary>
        public void Connect()
        {
            // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected && PhotonNetwork.NickName != "")
            {
                if (!IsReady)
                {
                    JoinWhenReady = true;
                    return;
                }
                
                if (RoomList.Count == 0)
                {
                    CreateRoom();
                    return;
                }

                try
                {
                    Debug.Log("Attempting to join room");
                    PhotonNetwork.JoinRoom(
                        (RoomList.FirstOrDefault(r => r.IsVisible && r.IsOpen )?? throw new NoAvailableRoomFoundException()).Name);

                }
                catch (NoAvailableRoomFoundException noRoomFound)
                {
                    Debug.Log("No open room found");
                    CreateRoom();
                }
                catch (ArgumentNullException e)
                {
                    Debug.LogError("Game Running not set on lobby");
                }
          
            }
            else
            {
                // #Critical, we must first and foremost connect to Photon Online Server.
                Debug.Log($"Is Connected: {PhotonNetwork.IsConnected}, Nickname: {PhotonNetwork.NickName}");
            }
        }
    
        #region MonoBehaviourPunCallbacks Callbacks


        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Server");
            PhotonNetwork.JoinLobby();
            connectionStatus.text = "Connected to Server";
        }

        public void OnConnectedToServer()
        {
            IsReady = true;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
            HideConnectionInfo();
            // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
            CreateRoom();
        }

        public override void OnJoinedRoom()
        {
            playerObjects.ForEach(o => o.SetActive(true));
            cameraController?.MoveToWaitingForPlayers(() =>
            {
                slenderman?.SetActive(false);
                UpdatePlayerLights();
                
            });
            Debug.Log($"Joined room {PhotonNetwork.CurrentRoom.Name}");
            ShowConnectionInfo($"Waiting for Players\n{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            PersistentData.Team = (GameData.Team)PhotonNetwork.PlayerList.Length - 1;
            
            

            InLobby = true;
            CheckGameStart();
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            UpdatePlayerLights();
            ShowConnectionInfo($"Waiting for Players\n{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

            CheckGameStart();
        }

        private void UpdatePlayerLights()
        {
            for (var i = 0; i < playerLights.Count; i++)
            {
                playerLights[i].SetActive(i < PhotonNetwork.CurrentRoom.PlayerCount);
            }
        }

        private void CheckGameStart()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom)
            {
                StartLobby();
            }
        }
        
        public void StartLobby()
        {
            if (!PhotonNetwork.IsMasterClient || !InLobby)
                return;
            if (!IsReady)
            {
                JoinWhenReady = true;
            }
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.LoadLevel(1);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            RoomList = roomList;
            IsReady = true;
            Debug.Log($"{RoomList.Count} rooms available");
            if (JoinWhenReady)
            {
                CreateRoom();
                JoinWhenReady = false;
            }
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
        }

        #endregion


        private void CreateRoom()
        {
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom, IsOpen = true, IsVisible = true}, TypedLobby.Default);
        
            ShowConnectionInfo("Joining Game");
        }
    
    
        private void ShowConnectionInfo(string info)
        {
            joinButton.gameObject.SetActive(false);
            forceStart.gameObject.SetActive(true);
            playerNameField.gameObject.SetActive(false);
            connectionInfo.gameObject.SetActive(true);
            connectionStatus.gameObject.SetActive(false);
            connectionInfo.SetText(info);
        }

        private void HideConnectionInfo()
        {
            joinButton.gameObject.SetActive(true);
            forceStart.gameObject.SetActive(false);
            playerNameField.gameObject.SetActive(true);
            connectionInfo.gameObject.SetActive(false);
            connectionStatus.gameObject.SetActive(true);
        }

    }
}
