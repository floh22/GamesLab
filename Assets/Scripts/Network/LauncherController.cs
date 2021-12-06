using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using Utils;

public class LauncherController : MonoBehaviourPunCallbacks
{
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

    [SerializeField]
    private Button joinButton;

    [SerializeField] private TMP_InputField playerNameField;

    [SerializeField] private TMP_Text connectionInfo;

    #endregion


    #region Private Fields

    private List<RoomInfo> RoomList = new();
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
        if (PhotonNetwork.IsConnected)
        {
            if (RoomList.Count == 0)
            {
                CreateRoom();
                return;
            }

            try
            {
                PhotonNetwork.JoinRoom(
                    (RoomList.FirstOrDefault(r =>
                         (bool)(r.CustomProperties["GameRunning"] ?? throw new ArgumentNullException()) == false) ??
                     throw new NoAvailableRoomFoundException()).Name);
            }
            catch (NoAvailableRoomFoundException noRoomFound)
            {
                CreateRoom();
            }
            catch (ArgumentNullException e)
            {
                
            }
          
        }
        else
        {
            // #Critical, we must first and foremost connect to Photon Online Server.
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = GameVersion.ToString();
        }
    }
    
    #region MonoBehaviourPunCallbacks Callbacks


    public override void OnConnectedToMaster()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
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
        Debug.Log($"Joined room {PhotonNetwork.CurrentRoom.Name}");
        ShowConnectionInfo($"Waiting for Players\n{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        PhotonNetwork.LoadLevel(0);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        ShowConnectionInfo($"Waiting for Players\n{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperty("GameRunning", true);
            PhotonNetwork.LoadLevel(0);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        RoomList = roomList;
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        PhotonNetwork.CurrentRoom.SetCustomProperty("GameRunning", false);
    }

    #endregion


    private void CreateRoom()
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom }, TypedLobby.Default);
        
        ShowConnectionInfo("Joining Game");
    }
    
    
    private void ShowConnectionInfo(string info)
    {
        joinButton.gameObject.SetActive(false);
        playerNameField.gameObject.SetActive(false);
        connectionInfo.gameObject.SetActive(true);
        connectionInfo.SetText(info);
    }

    private void HideConnectionInfo()
    {
        joinButton.gameObject.SetActive(true);
        playerNameField.gameObject.SetActive(true);
        connectionInfo.gameObject.SetActive(false);
    }

}