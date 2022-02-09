using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using GameManagement;
using Lobby;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;
using Random = System.Random;

namespace Network
{
    public class LauncherController : MonoBehaviourPunCallbacks
    {

        public static LauncherController Instance;
        
        #region Private Serializable Fields

        /// <summary>
        /// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
        /// </summary>
        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField]
        private byte maxPlayersPerRoom = 4;
    
        /// <summary>
        /// This client's version number. Users are separated from each other by gameVersion, allows breaking changes
        /// </summary>
        [Tooltip("This client's version number. Users are separated from each other by gameVersion, allows breaking changes")]
        public StringVersion GameVersion { get; } = new(1, 0, 0);

        [SerializeField] private Button forceStart;

        [SerializeField] private PersistentData persistentData;


        [Header("Scene Data")] 
        [SerializeField] private List<GameObject> playerLights;
        [SerializeField] private List<GameObject> playerObjects;
        [SerializeField] private GameObject slenderman;

        [SerializeField] private LobbyCameraController cameraController;
        [SerializeField] private ExitLobbySignBehavior lobbyExitSign;

        #endregion

        #region Private Fields


        private bool InLobby;
        private bool JoinWhenReady;

        private Coroutine playerNameDisplayRoutine;
        private float displayRoutineVelocity;
        
        private static readonly int Offset = Animator.StringToHash("VerticalSpeed");
        private static readonly int Speed = Animator.StringToHash("AirborneVerticalSpeed");

        #endregion
        
        private void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        // Start is called before the first frame update
        void Start()
        {
            if(Instance != null)
                Destroy(this);
            Instance = this;
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = GameVersion.ToString();
            
            foreach (GameObject playerObject in playerObjects)
            {
                playerObject.GetComponent<Animator>().SetFloat(Offset, UnityEngine.Random.Range(0, 2f));
                playerObject.GetComponent<Animator>().SetFloat(Speed, UnityEngine.Random.Range(0.85f, 1.15f));
            }
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
                if (!PersistentData.ConnectedToServer)
                {
                    JoinWhenReady = true;
                    return;
                }

                if (PersistentData.RoomList.Count == 0)
                {
                    CreateRoom();
                    return;
                }

                try
                {
                    Debug.Log("Attempting to join room");
                    PhotonNetwork.JoinRoom(
                        (PersistentData.RoomList.FirstOrDefault(r => r.IsVisible && r.IsOpen )?? throw new NoAvailableRoomFoundException()).Name);

                }
                catch (NoAvailableRoomFoundException noRoomFound)
                {
                    Debug.Log("No open room found " + noRoomFound.ToString());
                    CreateRoom();
                }
                catch (ArgumentNullException e)
                {
                    Debug.LogError("Game Running not set on lobby " + e.ToString());
                }
          
            }
            else
            {
                // #Critical, we must first and foremost connect to Photon Online Server.
                Debug.Log($"Is Connected: {PhotonNetwork.IsConnected}, Nickname: {PhotonNetwork.NickName}");
            }
        }

        public void Leave()
        {
            if (!PhotonNetwork.InRoom)
                return;
            PhotonNetwork.LeaveRoom(false);
            PhotonNetwork.JoinLobby();
            
            
            slenderman?.SetActive(true);
            LobbyUIController.Instance.IsActive = false;

            cameraController?.MoveToMainMenu(() =>
            {
                lobbyExitSign.Disable();
                playerObjects.ForEach(p => p.SetActive(false));
                foreach (GameObject t in playerLights)
                {
                    t.SetActive(false);
                }
                HideConnectionInfo();
                MenuUIController.Instance.ShowUI();
            });
        }
        
    
        #region MonoBehaviourPunCallbacks Callbacks


        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Server");
            PhotonNetwork.JoinLobby();
        }

        public void OnConnectedToServer()
        {
            PersistentData.ConnectedToServer = true;
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

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            UpdatePlayerLights();
            LobbyUIController.Instance.UpdatePlayerNames();

        }

        public override void OnJoinedRoom()
        {
            foreach (GameObject playerObject in playerObjects)
            {
                playerObject.SetActive(true);
                playerObject.GetComponent<Animator>().SetFloat(Offset, UnityEngine.Random.Range(0, 2f));
                playerObject.GetComponent<Animator>().SetFloat(Speed, UnityEngine.Random.Range(0.85f, 1.15f));
                
            }
            cameraController?.MoveToWaitingForPlayers(() =>
            {
                slenderman?.SetActive(false);
                lobbyExitSign.Enable();
                LobbyUIController.Instance.IsActive = true;
                UpdatePlayerLights();
            });
            Debug.Log($"Joined room {PhotonNetwork.CurrentRoom.Name}");
            ShowConnectionInfo($"Waiting for Players\n{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

            /*
            List<GameData.Team> freeTeams = new() {GameData.Team.RED, GameData.Team.BLUE, GameData.Team.GREEN, GameData.Team.YELLOW};
            foreach (Player p in PhotonNetwork.PlayerListOthers)
            {
                try
                {
                    GameData.Team t = (GameData.Team)p.CustomProperties["Team"];
                    freeTeams.Remove(t);
                }
                catch
                {
                    //oops? this might cause issues
                }
            }
            

            PersistentData.Team = freeTeams[(PhotonNetwork.PlayerList.Length - 1) % 4];
            */
            
            PersistentData.Team = (GameData.Team)PhotonNetwork.PlayerList.Length - 1;

            ExitGames.Client.Photon.Hashtable teamProperties = new() {["Team"] = PersistentData.Team};
            PhotonNetwork.LocalPlayer.SetCustomProperties(teamProperties);
            
            InLobby = true;
            CheckGameStart();
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            if (Equals(newPlayer, PhotonNetwork.LocalPlayer))
                return;
            UpdatePlayerLights();
            LobbyUIController.Instance.UpdatePlayerNames();
            ShowConnectionInfo($"Waiting for Players\n{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            CheckGameStart();
        }

        private void UpdatePlayerLights()
        {
            for (var i = 0; i < playerLights.Count; i++)
            {
                playerLights[i].SetActive(i < (PhotonNetwork.CurrentRoom == null ? Mathf.Infinity : PhotonNetwork.CurrentRoom.PlayerCount));
            }
        }

        private void CheckGameStart()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom && PhotonNetwork.IsMasterClient && InLobby)
            {
                StartCoroutine(StartLobby());
            }
        }

        public void ForceStart()
        {
            StartCoroutine(StartLobby(false));
        }
        
        public IEnumerator StartLobby(bool waitToStart = true)
        {
            if (!PhotonNetwork.IsMasterClient || !InLobby)
                yield break;
            if (!PersistentData.ConnectedToServer)
            {
                JoinWhenReady = true;
            }
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;


            //Just wait a bit... a bit hacky but i really dont care
            if(waitToStart)
                yield return new WaitForSeconds(8);
            
            LoadingScreenController.SendGameLoadingEvent();
            StartCoroutine(LoadMainLevel());
        }

        private IEnumerator LoadMainLevel()
        {
            yield return new WaitForSeconds(2);
            PhotonNetwork.LoadLevel(1); 
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            if (!JoinWhenReady) return;
            Connect();
            JoinWhenReady = false;
        }
        #endregion


        private void CreateRoom()
        {
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom, IsOpen = true, IsVisible = true, PlayerTtl = 0, EmptyRoomTtl = 0}, TypedLobby.Default);
        
            ShowConnectionInfo("Joining Game");
        }
    
    
        private void ShowConnectionInfo(string info)
        {
            forceStart.gameObject.SetActive(true);
        }

        private void HideConnectionInfo()
        {
            forceStart.gameObject.SetActive(false);
        }
    }
}
