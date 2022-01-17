using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character;
using ExitGames.Client.Photon;
using GameManagement;
using GameUnit;
using JetBrains.Annotations;
using NetworkedPlayer;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Network
{
    public class GameStateController : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public static GameStateController Instance;

        [CanBeNull] private MasterController controller;

        public const byte ChangeMinionTargetEventCode = 1;
        public const byte DamageGameUnitEventCode = 3;
        public const byte ChannelSlenderManEventCode = 4;

        public static UnityEvent LocalPlayerSpawnEvent = new();

        public static void SendChangeTargetEvent(GameData.Team team, GameData.Team target)
        {
            object[] content = {team, target};
            RaiseEventOptions raiseEventOptions = new() {Receivers = ReceiverGroup.Others};
            PhotonNetwork.RaiseEvent(ChangeMinionTargetEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }


        [Header("Player Data")] [SerializeField]
        private GameObject playerPrefab;

        [SerializeField] private GameObject abilityPrefab;

        [SerializeField] private GameObject spawnPointHolder;

        [Header("Minion Data")] [SerializeField]
        private MinionValues minionValues;

        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private GameObject minionSpawnPointHolder;
        [SerializeField] private GameObject minionPaths;


        public Dictionary<GameData.Team, PlayerController> Players;
        public Dictionary<GameData.Team, BaseBehavior> Bases;
        public Dictionary<GameData.Team, HashSet<Minion>> Minions;
        public HashSet<IGameUnit> GameUnits;

        public Dictionary<GameData.Team, GameData.Team> Targets;


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
            Minions = new Dictionary<GameData.Team, HashSet<Minion>>();
            Targets = new Dictionary<GameData.Team, GameData.Team>();
            GameUnits = new HashSet<IGameUnit>();

            if (playerPrefab == null)
            {
                // #Tip Never assume public properties of Components are filled up properly, always check and inform the developer of it.
                Debug.LogError(
                    "<Color=Red><b>Missing</b></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'",
                    this);
            }
            else
            {
                if (PlayerController.LocalPlayerInstance == null)
                {
                    Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
                    Vector3 pos = spawnPointHolder.transform.Find(PersistentData.Team.ToString()).transform.position;

                    // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                    GameObject pl = PhotonNetwork.Instantiate(this.playerPrefab.name, pos, Quaternion.identity, 0);

                    pl.transform.Find("FogOfWarVisibleRangeMesh").gameObject.SetActive(true);

                    GameObject abilityGo =
                        PhotonNetwork.Instantiate("PlayerAbilities", Vector3.zero, Quaternion.identity);
                    abilityGo.GetComponent<GameUnitFollower>().StartFollowing(pl.transform);
                }
                else
                {
                    Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
                }
            }

            GameObject baseHolder = GameObject.Find("Bases");

            foreach (GameData.Team team in (GameData.Team[]) Enum.GetValues(typeof(GameData.Team)))
            {
                Minions.Add(team, new HashSet<Minion>());

                //Set default target to opposing team
                Targets.Add(team, (GameData.Team) (((int) team + 2) % 4));

                if (baseHolder == null || baseHolder.Equals(null)) continue;
                BaseBehavior bb = baseHolder.transform.Find(team.ToString()).GetComponent<BaseBehavior>();
                Bases.Add(team, bb);
                GameUnits.Add(bb);
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

        private void Update()
        {
            // "back" button of phone equals "Escape". quit app if that's pressed
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitApplication();
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

            //Add them to gameUnits
            Debug.Log($"{Players.Count} Players found");
            int preSize = GameUnits.Count;
            foreach (PlayerController player in Players.Values)
            {
                int size = GameUnits.Count;
                GameUnits.Add(player);
                if (size + 1 != GameUnits.Count)
                {
                    Debug.LogError("Could not add player to GameUnit list");
                }
            }

            if (GameUnits.Count != preSize + Players.Count)
            {
                Debug.LogError("Something went wrong adding players to GameUnit list");
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


        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            if (eventCode == ChangeMinionTargetEventCode)
            {
                object[] data = (object[]) photonEvent.CustomData;

                GameData.Team team = (GameData.Team) data[0];
                GameData.Team target = (GameData.Team) data[1];

                SetMinionTarget(team, target);
            }

            if (eventCode == DamageGameUnitEventCode)
            {
                Debug.Log("Damage Event");
                object[] data = (object[]) photonEvent.CustomData;

                int sourceID = (int) data[0];
                int targetID = (int) data[1];
                float damage = (float) data[2];

                IGameUnit? source = null;
                IGameUnit? target = null;

                foreach (IGameUnit unit in GameUnits)
                {
                    if (unit.NetworkID == sourceID)
                        source = unit;
                    if (unit.NetworkID == targetID)
                        target = unit;
                    if (source != null && target != null)
                        break;
                }

                if (target == null || source == null)
                {
                    Debug.Log(
                        $"target or source null: source: {source?.NetworkID}, target: {target?.NetworkID}. sourceID: {sourceID}, targetID: {targetID}");
                    Debug.Log(GameUnits.Select(unit => unit.NetworkID).ToList()
                        .Aggregate("GameUnits: ", (current, item) => current + (item + ", ")));

                    return;
                }

                Debug.Log("showing damage dealt");
                target.DoDamageVisual(source, damage);

                //I am the owner. Deal the damage. This will get synced by photon
                if (target.OwnerID == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Debug.Log("I should take damage now");
                    target.Health = Mathf.Max(0, target.Health - damage);
                }
            }
        }


        void SetMinionTarget(GameData.Team team, GameData.Team target)
        {
            Targets[team] = target;

            if (!PhotonNetwork.IsMasterClient) return;

            //For now, have all minions instantly switch agro. Maybe change this over so only future minions switch agro?
            foreach (Minion minionBehavior in Minions[team].NotNull())
            {
                minionBehavior.SetTargetTeam(target);
            }
        }
    }
}