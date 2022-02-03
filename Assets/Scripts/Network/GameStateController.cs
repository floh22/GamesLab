using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character;
using Controls.Abilities;
using Controls.Channeling;
using ExitGames.Client.Photon;
using GameManagement;
using GameUnit;
using JetBrains.Annotations;
using Lobby;
using NetworkedPlayer;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;
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
        public const byte StartChannelEventCode = 4;
        public const byte FinishChannelEventCode = 5;
        public const byte LoseGameEventCode = 6;
        public const byte GameTimeEventCode = 7;
        public const byte PlayerAutoAttackEventCode = 8;

        public const byte PlayerLoadedEventCode = 12;
        // public const byte MinionSpawnedEventCode = 12;

        public static UnityEvent LocalPlayerSpawnEvent = new();

        public static void SendChangeTargetEvent(GameData.Team team, GameData.Team target)
        {
            object[] content = {team, target};
            RaiseEventOptions raiseEventOptions = new() {Receivers = ReceiverGroup.All};
            PhotonNetwork.RaiseEvent(ChangeMinionTargetEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }
        
        public static void SendLoseGameEvent(GameData.Team team)
        {
            object[] content = { team}; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.Others }; 
            PhotonNetwork.RaiseEvent(LoseGameEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public static void SendGameTimeEvent(float gameTime, float currentTimer)
        {
            object[] content = { gameTime, currentTimer}; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.Others }; 
            PhotonNetwork.RaiseEvent(GameTimeEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public static void SendStartChannelEvent(GameData.Team team, int networkID)
        {
            object[] content = { team, networkID}; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.Others }; 
            PhotonNetwork.RaiseEvent(StartChannelEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public static void SendFinishChannelEvent(GameData.Team team, int networkID, int value)
        {
            object[] content = { team, networkID, value}; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.Others }; 
            PhotonNetwork.RaiseEvent(FinishChannelEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public static void SendPlayerAutoAttackEvent(GameData.Team sourceTeam)
        {
            object[] content = { sourceTeam }; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.All }; 
            PhotonNetwork.RaiseEvent(PlayerAutoAttackEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public static void SendPlayerLoadedEvent(GameData.Team sourceTeam)
        {
            object[] content = { sourceTeam }; 
            RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.All }; 
            PhotonNetwork.RaiseEvent(PlayerLoadedEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

        // public static void SendMinionSpawnedEvent(GameData.Team team, int networkID)
        // {
        //     object[] content = { team, networkID }; 
        //     RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.Others }; 
        //     PhotonNetwork.RaiseEvent(MinionSpawnedEventCode, content, raiseEventOptions, SendOptions.SendReliable);
        // }        


        [Header("Player Data")]
        [SerializeField]
        private GameObject[] playerVariantPrefabs;

        [SerializeField] private GameObject abilityPrefab;

        [SerializeField] private GameObject spawnPointHolder;


        [Header("Slender Data")] 
        [SerializeField] private GameObject slendermanPrefab;

        [SerializeField] public GameObject slendermanSpawnPosition;

        [Header("Base Data")] [SerializeField] private GameObject basePrefab;
        [SerializeField] public GameObject baseSpawnPosition;
            


        [Header("Minion Data")]
        
        [SerializeField] public MinionValues minionValues;
        [SerializeField] public GameObject minionPrefab;
        [SerializeField] public GameObject minionSpawnPointHolder;
        [SerializeField] public GameObject minionPaths;


        [Header("Game Data")] 
        public Dictionary<GameData.Team, PlayerController> Players;
        public Dictionary<GameData.Team, BaseBehavior> Bases;
        public Dictionary<GameData.Team, HashSet<Minion>> Minions;
        public HashSet<IGameUnit> GameUnits;
        public Dictionary<GameData.Team, GameData.Team> Targets;
        public Slenderman Slenderman;

        public HashSet<GameData.Team> LoadedTeams;

        
        [field: SerializeField] public float GameTime { get; set; }
        public bool IsPaused { get; set; }


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

            GameObject playerPrefab = null;

            LoadedTeams = new HashSet<GameData.Team>();

            playerPrefab = PersistentData.Team.ToString() switch
            {
                "RED" => playerVariantPrefabs[0],
                "GREEN" => playerVariantPrefabs[1],
                "BLUE" => playerVariantPrefabs[2],
                "YELLOW" => playerVariantPrefabs[3],
                _ => null
            };

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
                    GameObject pl = PhotonNetwork.Instantiate(playerPrefab.name, pos, Quaternion.identity, 0);

                    pl.transform.Find("FogOfWarVisibleRangeMesh").gameObject.SetActive(true);

                    GameObject abilityGo =
                        PhotonNetwork.Instantiate("PlayerAbilities", Vector3.zero, Quaternion.identity);
                    abilityGo.GetComponent<GameUnitFollower>().StartFollowing(pl.transform);

                    pl.AddComponent<AudioListener>();
                }
                else
                {
                    Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
                }
            }

            foreach (GameData.Team team in (GameData.Team[]) Enum.GetValues(typeof(GameData.Team)))
            {
                Minions.Add(team, new HashSet<Minion>());

                //Set default target to opposing team
                Targets.Add(team, (GameData.Team) (((int) team + 2) % 4));
            }
            
            
            //Set minion values here so all clients have them when it comes time to switch masters
            Minion.Values = minionValues;
            Minion.Splines = minionPaths;


            StartCoroutine(LoadSyncedObjects());

            if (!PhotonNetwork.IsMasterClient) return;
            try
            {
                controller = gameObject.AddComponent<MasterController>() ?? throw new NullReferenceException();
                controller.SpawnSlenderman();
                controller.SpawnBases();
            }
            catch (Exception e)
            {
                Debug.LogError("Could not create master controller. Server functionality will not work");
                Debug.Log(e.Message);
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


        private IEnumerator LoadSyncedObjects()
        {
            bool basesLoaded = false;
            bool playersLoaded = false;
            bool slenderLoaded = false;

            while (!(basesLoaded && playersLoaded && slenderLoaded))
            {
                if (!basesLoaded)
                {
                    GameObject basesO = GameObject.Find("Bases(Clone)");
                    if (basesO != null && !basesO.Equals(null))
                    {
                        Bases.Clear();
                        foreach (GameData.Team team in (GameData.Team[]) Enum.GetValues(typeof(GameData.Team)))
                        {
                            BaseBehavior bb = basesO.transform.Find(team.ToString()).GetComponent<BaseBehavior>();
                            Bases.Add(team, bb);
                            GameUnits.Add(bb);
                        }

                        if (Bases.Count == 4)
                        {
                            Debug.Log("Bases loaded");
                            basesLoaded = true;
                        }
                            
                    }
                }

                if (!playersLoaded)
                {
                    var playerObjects = GameObject.FindGameObjectsWithTag("Player");
                    
                    if (playerObjects.Length == PhotonNetwork.CurrentRoom.PlayerCount)
                    {
                        Players = playerObjects.Select(playerGo =>
                        {
                            PlayerController pc = playerGo.GetComponent<PlayerController>();
                            GameUnits.Add(pc);
                            return new KeyValuePair<GameData.Team, PlayerController>(pc.Team, pc);
                        }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        
                        Debug.Log("Players loaded");
                        playersLoaded = true;
                    }
                }

                if (!slenderLoaded)
                {
                    GameObject slenderO = GameObject.FindWithTag("Slenderman");
                    if (slenderO != null && !slenderO.Equals(null))
                    {
                        Slenderman = slenderO.GetComponent<Slenderman>();

                        Debug.Log("Slenderman loaded");
                        slenderLoaded = true;
                    }
                }
                
                //Check 10 times a second if everything has loaded
                yield return new WaitForSeconds(0.10f);
            }
            
            SendPlayerLoadedEvent(PersistentData.Team ?? throw new NullReferenceException());
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
                object[] data = (object[]) photonEvent.CustomData;

                int sourceID = (int) data[0];
                int targetID = (int) data[1];
                float damage = (float) data[2];

                // Debug.Log($"sourceID = {sourceID}");
                // Debug.Log($"targetID = {targetID}");

#nullable enable
                IGameUnit? source = null;
                IGameUnit? target = null;
#nullable disable
                foreach (IGameUnit unit in GameUnits)
                {
                    // Debug.Log($"unit.NetworkID = {unit.NetworkID}");

                    if (unit.NetworkID == sourceID)
                        source = unit;
                    if (unit.NetworkID == targetID)
                        target = unit;
                    if (source != null && target != null)
                        break;
                }

                // String sourceString = source == null ? "null" : source.ToString();
                // String targetString = target == null ? "null" : target.ToString();

                // Debug.Log($"source = {sourceString}");
                // Debug.Log($"target = {targetString}");
                // Debug.Log($"damage = {damage}");                     

                if (target == null || target.Equals(null) || source == null || source.Equals(null))
                {
                    // Debug.Log(
                    //     $"target or source null: source: {source?.NetworkID}, target: {target?.NetworkID}. sourceID: {sourceID}, targetID: {targetID}");
                    // Debug.Log(GameUnits.Select(unit => unit.NetworkID).ToList()
                    //     .Aggregate("GameUnits: ", (current, item) => current + (item + ", ")));

                    return;
                }

                if(target.ToString().StartsWith("Minion"))
                {                 
                    //I am the owner. Deal the damage. This will get synced by photon
                    if (target.OwnerID == PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        target.Health = Mathf.Max(0, target.Health - damage);   
                        target.DoDamageVisual(source, damage);                            
                    }    
                }      

                /* Start of Ellen's Attack and Damaged Animations stuff */      

                if(target.ToString().StartsWith("Ellen"))
                {          
                    target.Health = Mathf.Max(0, target.Health - damage);   
                    target.DoDamageVisual(source, damage);                      

                    String targetTeam = target.Team.ToString();
                    PlayerController ellenPlayerController = (PlayerController) target;

                    // Debug.Log($"Ellen of team {targetTeam} is taking dmg."); 

                    if (ellenPlayerController.Health <= 0f && ellenPlayerController.IsAlive)
                    {
                        if (ellenPlayerController.gameObject.GetPhotonView().IsMine)
                        {
                            // Debug.Log($"Ellen of team {targetTeam} is mine.");   
                            ellenPlayerController.Die();
                            // target.DoDamageVisual(source, damage);                            
                        }                            

                        /* Start of Ellen's Move Animation stuff */

                        Gamekit3D.PlayerController ellenGamekit3DPlayerController = ellenPlayerController.gameObject.GetComponent<Gamekit3D.PlayerController>();
                        ellenGamekit3DPlayerController.DoDieVisual();

                        /* End of Ellen's Move Animation stuff */      
                                             
                        // Debug.Log($"Ellen of team {targetTeam} died.");                        
                    }                    
                    else
                    {
                        if(ellenPlayerController.IsAlive)
                        {
                            // Gamekit3D.PlayerController ellenGamekit3DPlayerController = ellenPlayerController.gameObject.GetComponent<Gamekit3D.PlayerController>();
                            // ellenGamekit3DPlayerController.DoTakeDamageVisual();

                            MonoBehaviour damager = null;

                            if(source.ToString().StartsWith("Minion"))
                            {
                                damager = ((Minion) source);
                            }
                            else if(source.ToString().StartsWith("Ellen"))
                            {
                                damager = ((PlayerController) source);
                            }  

                            Vector3 direction = (target.Position - source.Position).normalized;

                            Gamekit3D.Damageable.DamageMessage dataMessage;
                            dataMessage.damager = damager;                         // MonoBehaviour
                            dataMessage.amount = (int) damage;                     // int
                            dataMessage.direction = direction;                     // Vector3
                            dataMessage.damageSource = source.Position;            // Vector3
                            dataMessage.throwing = false;                          // bool
                            dataMessage.stopCamera = false;                        // bool

                            Gamekit3D.Damageable ellenDamageable = ellenPlayerController.gameObject.GetComponent<Gamekit3D.Damageable>();
                            ellenDamageable.maxHitPoints = (int) ellenPlayerController.MaxHealth; // Could be set somewhere else but this is fine for now
                            ellenDamageable.currentHitPoints = (int) ellenPlayerController.Health;
                            ellenDamageable.ApplyDamage(dataMessage);

                            // Debug.Log($"Ellen of team {targetTeam} is being attacked.");   
                        } 
                    }       
                }       
                                                                                
                /* End of Ellen's Attack and Damaged Animations stuff */                 
            }

            if (eventCode == LoseGameEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                OnLose((GameData.Team)data[0]);
            }

            if (eventCode == GameTimeEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                GameTime = (float)data[0];
                UIManager.Instance.gameTimer.timeRemainingInSeconds = (float)data[1];
            }

            if (eventCode == StartChannelEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                
                GameData.Team team = (GameData.Team) data[0];
                int targetNetworkID = (int)data[1];
                PlayerController source = Players[team];
                
                Debug.Log($"{team} started channeling {targetNetworkID}");

                if (targetNetworkID == Slenderman.NetworkID)
                {
                    //Channeled slenderman
                    source.SetChannelingTo(Slenderman.gameObject.transform.position);
                    source.OnStartSlendermanChannel(Slenderman.gameObject.GetComponent<BoxCollider>().bounds.size);
                    return;
                }
                
                foreach (BaseBehavior baseStructure in Bases.Values.Where(baseStructure => baseStructure.NetworkID == targetNetworkID))
                {
                    //channeled a base
                    source.SetChannelingTo(baseStructure.innerChannelingParticleSystem.transform.position);
                    source.OnStartBaseChannel();
                    return;
                }
            }

            if (eventCode == FinishChannelEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                
                
                GameData.Team team = (GameData.Team) data[0];
                int targetNetworkID = (int)data[1];
                int value = (int)data[2];
                PlayerController source = Players[team];
                
                Debug.Log($"{team} finished channeling {targetNetworkID}");

                if (targetNetworkID == Slenderman.NetworkID)
                {
                    //Channeled slenderman
                    Slenderman.DisableChannelEffects();
                    source.DisableChannelEffects();

                    if (!PhotonNetwork.IsMasterClient)
                        return;

                    // The 999 is used in the DisableChannelEffectsNetworked function.
                    // It serves to indicate that we only want to disable channeling
                    // effects and nothing more
                    if(value != 999)
                        Slenderman.OnChanneled();

                    return;
                }
                
                foreach (BaseBehavior baseStructure in Bases.Values.Where(baseStructure => baseStructure.NetworkID == targetNetworkID))
                {
                    baseStructure.DisableChannelEffects();
                    source.DisableChannelEffects();

                    if (!PhotonNetwork.IsMasterClient)
                        return;
                        
                    // The 999 is used in the DisableChannelEffectsNetworked function.
                    // It serves to indicate that we only want to disable channeling
                    // effects and nothing more
                    if(value != 999)
                        baseStructure.Pages = value;

                    return;
                }
            }

            if (eventCode == PlayerAutoAttackEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                
                GameData.Team sourceTeam = (GameData.Team) data[0];

                PlayerController sourcePlayerController = Players[sourceTeam];

                /* Start of Ellen's Attack Animation stuff */

                PlayerInput ellenPlayerInput = sourcePlayerController.gameObject.GetComponent<PlayerInput>();
                ellenPlayerInput.DoAttack();

                /* End of Ellen's Attack Animation stuff */
            }

            if (eventCode == PlayerLoadedEventCode)
            {
                GameData.Team team = (GameData.Team)((object[])photonEvent.CustomData)[0];
                LoadedTeams.Add(team);
                Debug.Log($"{team} player ready");

                if (LoadedTeams.Count == PhotonNetwork.CurrentRoom.PlayerCount)
                {
                    if(PhotonNetwork.IsMasterClient && controller != null && !controller.Equals(null))
                    {
                        LoadingScreenController.SendGameStartingEvent();
                        controller.StartMinionSpawning(Minion.Values.InitWaveDelayInMs);
                    }
                }
            }

            // if (eventCode == MinionSpawnedEventCode)
            // {
            //     object[] data = (object[])photonEvent.CustomData;
                
            //     GameData.Team sourceTeam = (GameData.Team) data[0];
            //     int networkID = (int) data[1];
            // }
        }

        public void OnLose()
        {
            PlayerController.LocalPlayerController.OnLoseGame();
            
            SendLoseGameEvent(PlayerController.LocalPlayerController.Team);
            PhotonNetwork.Destroy(PlayerController.LocalPlayerInstance);
            
        }

        public void OnLose(GameData.Team team)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                foreach (Minion minion in Minions[team])
                {
                    PhotonNetwork.Destroy(minion.gameObject);
                }
            }
            //Display a lose message? Maybe check that the player object is destroyed, not sure
            Players.Remove(team);
            Minions.Remove(team);
            Bases.Remove(team);
            Targets.Remove(team);
            GameUnits.RemoveWhere(unit => unit.Team == team);
        }


        void SetMinionTarget(GameData.Team team, GameData.Team target)
        {
            Targets[team] = target;
            Debug.Log($"Switching {team} minion target to {target}");

            return;
            // if (!PhotonNetwork.IsMasterClient) return;

            // //For now, have all minions instantly switch agro. Maybe change this over so only future minions switch agro?
            // foreach (Minion minionBehavior in Minions[team].NotNull())
            // {
            //     minionBehavior.SetTargetTeam(target);
            // }
        }
    }
}