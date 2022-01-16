using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using ExitGames.Client.Photon.StructWrapping;
using GameManagement;
using Network;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace NetworkedPlayer
{
    public class PlayerController : MonoBehaviourPunCallbacks, IGameUnit
    {
        #region StaticFields
        
        public static GameObject LocalPlayerInstance;
        public static PlayerController LocalPlayerController;
        
        #endregion
        
        [SerializeField] public GameObject DamageText;
        [FormerlySerializedAs("DeadPlayer")] [SerializeField] public GameObject DeadPlayerPrefab;

        private CameraController cameraController;

        [SerializeField] private bool hasPage;
        
        public bool HasPage
        {
            get => hasPage;
            set
            {
                if (hasPage != value)
                {
                    if(value)
                        PickUpPage();
                    else
                        DropPage();
                }
                hasPage = value;
            }
        }

        #region IGameUnit
        public int NetworkID { get; set; }
        [field: SerializeField] public GameData.Team Team { get; set; }

        public GameUnitType Type => GameUnitType.Player;
        public float MaxHealth { get; set; }
        [field: SerializeField] public float Health { get; set; }
        public float MoveSpeed { get; set; }
        public float RotationSpeed { get; set; }
        public float AttackDamage { get; set; }
        public float AttackSpeed { get; set; }
        public float AttackRange { get; set; }
        public bool IsAlive { get; set; } = true;
        public bool IsVisible { get; set; }

        public IGameUnit CurrentAttackTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; }
        
        #endregion
        
        #region Level
        [field: SerializeField] public int Level { get; set; }
        [field: SerializeField] public int Experience { get; set; }
        [field: SerializeField] public int ExperienceToReachNextLevel { get; set; }
        public int ExperienceBetweenLevels { get; set; }
        public int GainedExperienceByMinion { get; set; }
        public int GainedExperienceByPlayer { get; set; }
        [field: SerializeField] public float DamageMultiplierMinion { get; set; }
        [field: SerializeField] public float DamageMultiplierAbility1 { get; set; }
        [field: SerializeField] public float DamageMultiplierAbility2 { get; set; }
        
        #endregion
        
        public void Damage(IGameUnit unit, float damage)
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Uncomment next line sometime
            // CurrentlyAttackedBy.Add(unit);
            
            Health -= damage;
            
            DamageIndicator indicator = Instantiate(DamageText, transform.position, Quaternion.identity)
                .GetComponent<DamageIndicator>();
            indicator.SetDamageText(damage);
        }

        public bool IsDestroyed()
        {
            return !gameObject;
        }

        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        #region Private Fields

        [SerializeField] private GameObject playerUiPrefab;

        [FormerlySerializedAs("beams")] [SerializeField]
        private GameObject channelPrefab;

        private bool isChanneling;

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        public void Awake()
        {
            if (this.channelPrefab == null)
            {
                Debug.LogError("<Color=Red><b>Missing</b></Color> Beams Reference.", this);
            }
            else
            {
                this.channelPrefab.SetActive(false);
            }


            // in GameStateController we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
            if (photonView.IsMine)
            {
                LocalPlayerInstance = gameObject;
                LocalPlayerController = this;
                Team = PersistentData.Team ?? throw new NullReferenceException();
                this.transform.rotation = Quaternion.LookRotation(Vector3.zero);
            }

            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        public void Start()
        {
            cameraController = gameObject.GetComponent<CameraController>();
            NetworkID = gameObject.GetInstanceID();
            CurrentlyAttackedBy = new HashSet<IGameUnit>();


            //TODO temp
            Health = 100;
            MaxHealth = 100;
            Level = 0;
            Experience = 0;
            ExperienceToReachNextLevel = 200;
            ExperienceBetweenLevels = 100;
            GainedExperienceByMinion = 50;
            GainedExperienceByPlayer = 100;
            DamageMultiplierMinion = 1f;
            DamageMultiplierAbility1 = 1f;
            DamageMultiplierAbility2 = 1f;
            

            if (cameraController != null)
            {
                if (photonView.IsMine)
                {
                    cameraController.OnStartFollowing();
                }
            }
            else
            {
                Debug.LogError("<Color=Red><b>Missing</b></Color> CameraController Component on player Prefab.", this);
            }

            // Create the UI
            if (this.playerUiPrefab != null)
            {
                GameObject uiGo = Instantiate(this.playerUiPrefab);
                uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
            else
            {
                Debug.LogWarning("<Color=Red><b>Missing</b></Color> PlayerUiPrefab reference on player Prefab.", this);
            }
        }


        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// Process Inputs if local player.
        /// Show and hide the beams
        /// Watch for end of game, when local player health is 0.
        /// </summary>
        public void Update()
        {
            // we only process Inputs and check health if we are the local player
            if (photonView.IsMine)
            {
                this.ProcessInputs();

                if (this.Health <= 0f && IsAlive)
                {
                    Die();
                }
            }

            if (this.channelPrefab != null && this.isChanneling != this.channelPrefab.activeInHierarchy)
            {
                this.channelPrefab.SetActive(this.isChanneling);
            }
        }
        
        public void OnTriggerEnter(Collider other)
        {
            // we dont do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }


            // We are only interested in Beams. Beam weapon for now since its a bit simpler than ammo to sync over network
            // we should be using tags, but im lazy
            if (!other.name.Contains("Beam"))
            {
                return;
            }

            // we slowly affect health when beam is constantly hitting us, so player has to move to prevent death.
            this.Health -= 0.1f;
        }

        public void OnTriggerStay(Collider other)
        {
            // we dont do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }

            // We are only interested in Beams. Beam weapon for now since its a bit simpler than ammo to sync over network
            // we should be using tags, but im lazy
            if (!other.name.Contains("Beam"))
            {
                return;
            }

            // we slowly affect health when beam is constantly hitting us, so player has to move to prevent death.
            this.Health -= 0.1f * Time.deltaTime;
        }
        

        #endregion

        private void PickUpPage()
        {
            
        }

        private void DropPage()
        {
            
        }
        
        public void Die()
        {
            IsAlive = false;
            //Stop following alive character
            cameraController.OnStopFollowing();
            
            //create dead character
            var position = transform.position;
            GameObject deadPlayerObject = Instantiate(DeadPlayerPrefab, position, Quaternion.identity);
            DeadPlayerController deadPlayerController = deadPlayerObject.GetComponent<DeadPlayerController>();
            CameraController deadCameraController = deadPlayerObject.GetComponent<CameraController>();
            
            //follow dead character
            deadCameraController.OnStartFollowing();
            
            
            StartCoroutine(Respawn(() => { 
                //Respawn at position
                position = GameStateController.Instance.GetPlayerSpawnPoint(Team) + Vector3.up;
                Terrain activeTerrain = Terrain.activeTerrain;
                position = new Vector3( position.x, activeTerrain.SampleHeight(GameStateController.Instance.GetPlayerSpawnPoint(Team)) + activeTerrain.transform.position.y, position.y);
                transform.position = position;
            
                //Reset stats
                this.Health = this.MaxHealth;
                IsAlive = true;
            
                //Switch cameras
                deadCameraController.OnStopFollowing();
                cameraController.OnStartFollowing();
            
                Destroy(deadPlayerObject);
        }));
            
        }


        public IEnumerator Respawn(Action nextFunc)
        {
            //wait out death timer
            yield return new WaitForSeconds(10);
            nextFunc.Invoke();
        }

        private void ProcessInputs()
        {
            if (Input.GetButtonDown("Fire1"))
            {
                // we don't want to fire when we interact with UI buttons, and since all EventSystem GameObjects are UI, ignore input when over UI
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                if (!this.isChanneling)
                {
                    this.isChanneling = true;
                }
            }

            if (!Input.GetButtonUp("Fire1")) return;
            if (this.isChanneling)
            {
                this.isChanneling = false;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Local player, send data
                stream.SendNext(this.isChanneling);
                stream.SendNext(this.Health);
                stream.SendNext(this.Team);
            }
            else
            {
                // Network player, receive data
                this.isChanneling = (bool) stream.ReceiveNext();
                this.Health = (float) stream.ReceiveNext();
                this.Team = (GameData.Team) stream.ReceiveNext();
            }
        }

        public void AddExperience(int amount)
        {
            Experience += amount;
            if (Experience >= ExperienceToReachNextLevel)
            {
                Level++;
                Experience -= ExperienceToReachNextLevel;
                ExperienceToReachNextLevel += ExperienceBetweenLevels;
                StartCoroutine(GameObject.Find("UIManager").GetComponent<UIManager>().ShowLevelUpLabel());
            }
        }

        public void UpdateMultiplier(int whatToUpdate)
        {
            switch (whatToUpdate)
            {
                case 1:
                    DamageMultiplierMinion += 0.1f;
                    break;
                case 2:
                    DamageMultiplierAbility1 += 0.2f;
                    break;
                case 3:
                    DamageMultiplierAbility2 += 0.2f;
                    break;
            }
        }

        public void OnLoseGame()
        {
            
        }
    }
}