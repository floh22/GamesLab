using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameManagement;
using GameUnit;
using JetBrains.Annotations;
using NetworkedPlayer;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    public class GameStateController : MonoBehaviourPunCallbacks
    {
        
        public static GameStateController Instance;
        
        [CanBeNull] private MasterController controller;
        
        
        [Header("Player Data")]
        [SerializeField]
        private GameObject playerPrefab;

        [SerializeField] private GameObject abilityPrefab;

        [SerializeField] private GameObject spawnPointHolder;
        
        
        [Header("Minion Data")]
        
        [SerializeField] private MinionValues minionValues;
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private GameObject minionSpawnPointHolder;
        [SerializeField] private GameObject minionPaths;


        public Dictionary<GameData.Team, PlayerController> Players;
        public Dictionary<GameData.Team, BaseBehavior> Bases;


        private bool hasLeft = false;
        
        #region Photon Callbacks


        /// <summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            SceneManager.LoadScene(0);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);
        }

        #endregion


        #region Public Methods


        public void LeaveRoom()
        {
            if (hasLeft)
                return;
            hasLeft = true;

            if (PhotonNetwork.IsMasterClient)
            {
                Player nextMasterClient = PhotonNetwork.MasterClient.GetNext();
                PhotonNetwork.SetMasterClient(nextMasterClient);
                PhotonView photonView = PhotonView.Get(this);
                photonView.RPC("NewMasterClient", RpcTarget.Others, nextMasterClient.ActorNumber);
            }
            
            PhotonNetwork.LeaveRoom();
        }

        [PunRPC]
        public void NewMasterClient(int newMasterActorNumber)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterActorNumber)
            {
                Debug.Log("Now functioning as master client");
            }
        }


        #endregion
        
        void Start()
        {
            Instance = this;

            // in case we started this demo with the wrong scene being active, simply load the menu scene
            if (!PhotonNetwork.IsConnected)
            {
                SceneManager.LoadScene(0);

                return;
            }

            Players = new Dictionary<GameData.Team, PlayerController>();
            Bases = new Dictionary<GameData.Team, BaseBehavior>();

            if (playerPrefab == null) { // #Tip Never assume public properties of Components are filled up properly, always check and inform the developer of it.

                Debug.LogError("<Color=Red><b>Missing</b></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
            } else {


                if (PlayerController.LocalPlayerInstance==null)
                {
                    Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
                    Vector3 pos = spawnPointHolder.transform.Find(PersistentData.Team.ToString()).transform.position;

                    // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                    GameObject pl = PhotonNetwork.Instantiate(this.playerPrefab.name, pos, Quaternion.identity, 0);
                    
                    pl.transform.Find("FogOfWarVisibleRangeMesh").gameObject.SetActive(true);

                    GameObject.Instantiate(abilityPrefab);
                }
                else{

                    Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
                }


            }
            
            GameObject baseHolder = GameObject.Find("Bases");

            foreach (GameData.Team team in (GameData.Team[])Enum.GetValues(typeof(GameData.Team)))
            {
                if(baseHolder != null && !baseHolder.Equals(null))
                    Bases.Add(team, baseHolder.transform.Find(team.ToString()).GetComponent<BaseBehavior>());
            }


            StartCoroutine(InitPlayersThisRound());
            
            if (!PhotonNetwork.IsMasterClient) return;
            try
            {
                controller = gameObject.AddComponent<MasterController>() ?? throw new NullReferenceException();
                controller.Init(minionValues, minionPrefab, minionSpawnPointHolder, minionPaths);

                controller.StartMinionSpawning(10000);
            }
            catch
            {
                Debug.LogError("Could not create master controller. Server functionality will not work");
            }

        }


        private IEnumerator InitPlayersThisRound()
        {
            //Wait 2 seconds to init players to let everyone join
            yield return new WaitForSeconds(2);

            Players = GameObject.FindGameObjectsWithTag("Player").Select(playerGo =>
            {
                PlayerController pc = playerGo.GetComponent<PlayerController>();
                return new KeyValuePair<GameData.Team, PlayerController>(pc.Team, pc);
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private void Update()
        {
            // "back" button of phone equals "Escape". quit app if that's pressed
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitApplication();
            }
        }
        
        public void QuitApplication()
        {
            Application.Quit();
        }

        public Vector3 GetPlayerSpawnPoint(GameData.Team team)
        {
            return spawnPointHolder.transform.Find(team.ToString()).transform.position;
        }

        
    }
}
