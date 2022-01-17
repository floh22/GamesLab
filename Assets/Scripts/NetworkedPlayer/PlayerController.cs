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
        [SerializeField] public GameObject SlenderBuffPrefab;
        
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
        //TODO: Probably merge this with LocalPlayerInstance but I didnt want to break anything so I left it as is for now
        public GameObject AttachtedObjectInstance { get; set; } 

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
        [SerializeField] public bool HasSlenderBuff { get; set; }
        [SerializeField] public float SlenderBuffDuration = 45;
        

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

        private float _damageMultiplierMinion;
        [field: SerializeField] public float DamageMultiplierMinion
        {
            get
            {
                return ReturnMultiplierWithRespectToSlenderBuff(_damageMultiplierMinion);
            }
            set
            {
                _damageMultiplierMinion = value;
            }
        }

        private float _damageMultiplierAbility1;
        [field: SerializeField] public float DamageMultiplierAbility1 
        {
            get
            {
                return ReturnMultiplierWithRespectToSlenderBuff(_damageMultiplierAbility1);
            }
            set
            {
                _damageMultiplierAbility1 = value;
            }
        }

        private float _damageMultiplierAbility2;
        [field: SerializeField] public float DamageMultiplierAbility2 
        {
            get
            {
                return ReturnMultiplierWithRespectToSlenderBuff(_damageMultiplierAbility2);
            }
            set
            {
                _damageMultiplierAbility2 = value;
            }
        }

        [field: SerializeField] public int UpgradesMinion { get; set; }
        [field: SerializeField] public int UpgradesAbility1 { get; set; }
        [field: SerializeField] public int UpgradesAbility2 { get; set; }

        #endregion

        [field: SerializeField] public float DeathTimerMax { get; set; } = 15;
        [field: SerializeField] public float DeathTimerCurrently { get; set; } = 0;

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

        private PlayerUI playerUI;

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
            Level = 1;
            Experience = 0;
            ExperienceToReachNextLevel = 200;
            ExperienceBetweenLevels = 100;
            GainedExperienceByMinion = 50;
            GainedExperienceByPlayer = 100;
            DamageMultiplierMinion = 1f;
            DamageMultiplierAbility1 = 1f;
            DamageMultiplierAbility2 = 1f;
            UpgradesMinion = 0;
            UpgradesAbility1 = 0;
            UpgradesAbility2 = 0;

            CurrentlyAttackedBy = new HashSet<IGameUnit>();

            // Debug.Log($"{photonView.Owner.NickName} is on team: {Team.ToString()}");

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
                playerUI = uiGo.GetComponent<PlayerUI>();
                playerUI.SetTarget(this);
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


        public IEnumerator Respawn()
        {
            yield return new WaitForSeconds(10);
            this.Health = this.MaxHealth;
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
            
            //remove attackers
            foreach (IGameUnit gameUnit in CurrentlyAttackedBy)
            {
                if (gameUnit.Type == GameUnitType.Player && Vector3.Distance(gameUnit.Position, Position) < IGameUnit.DistanceForExperienceOnDeath)
                {
                    gameUnit.AttachtedObjectInstance.GetComponent<PlayerController>().AddExperienceBySource(false);
                }

                gameUnit.TargetDied(this);
            }
            
            
            //Stop following alive character
            cameraController.OnStopFollowing();
            CharacterController controller = GetComponent<CharacterController>();
            controller.enabled = false;
            GameObject playerUiGo = playerUI.gameObject;
            playerUiGo.SetActive(false);
            
            //create dead character
            Vector3 position = transform.position;
            GameObject deadPlayerObject = Instantiate(DeadPlayerPrefab, position, Quaternion.identity);
            CameraController deadCameraController = deadPlayerObject.GetComponent<CameraController>();
            
            //follow dead character
            deadCameraController.OnStartFollowing();

            transform.position = new Vector3(0, -10, 0);
            
            GetComponent<Rigidbody>().position = new Vector3(0, -10, 0);
            
            
            StartCoroutine(Respawn(() => {
                IsAlive = true;
                
                
                //Get Player spawn point
                position = GameStateController.Instance.GetPlayerSpawnPoint(Team) + Vector3.up ;
                transform.position = position;

                //Reset stats
                this.Health = this.MaxHealth;
                
                controller.enabled = true;
                playerUiGo.SetActive(true);
                
                //Start following player again
                deadCameraController.OnStopFollowing();
                cameraController.OnStartFollowing();
                
                //Destroy dead player
                Destroy(deadPlayerObject);
        }));
            
        }


        public IEnumerator Respawn(Action nextFunc)
        {
            //wait out death timer
            DeathTimerCurrently = DeathTimerMax;

            while (DeathTimerCurrently > 0)
            {
                DeathTimerCurrently = Mathf.Max(0, DeathTimerCurrently - 0.1f);
                yield return new WaitForSeconds(0.1f);
            }
            
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
                stream.SendNext(this.Level);
            }
            else
            {
                // Network player, receive data
                this.isChanneling = (bool) stream.ReceiveNext();
                this.Health = (float) stream.ReceiveNext();
                this.Team = (GameData.Team) stream.ReceiveNext();
                this.Level = (int)stream.ReceiveNext();
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

        public void AddExperienceBySource(bool byMinion)
        {
            AddExperience(byMinion ? GainedExperienceByMinion : GainedExperienceByPlayer);
        }

        public void UpdateMultiplier(int whatToUpdate)
        {
            switch (whatToUpdate)
            {
                case 1:
                    if (UpgradesMinion < 4)
                    {
                        DamageMultiplierMinion += 0.1f;
                        UpgradesMinion++;
                    }

                    break;
                case 2:
                    if (UpgradesAbility1 < 4)
                    {
                        DamageMultiplierAbility1 += 0.2f;
                        UpgradesAbility1++;
                    }

                    break;
                case 3:
                    if (UpgradesAbility2 < 4)
                    {
                        DamageMultiplierAbility2 += 0.2f;
                        UpgradesAbility2++;
                    }

                    break;
            }
        }

        public void ActivateSlenderBuff()
        {
            StartCoroutine(SlenderBuffCoroutine());
        }

        private float ReturnMultiplierWithRespectToSlenderBuff(float mulitplier)
        {
            return HasSlenderBuff ? mulitplier * 2 : mulitplier;
        }

        IEnumerator SlenderBuffCoroutine()
        {
            Vector3 position = transform.position;
            position.y = 0.1f;
            HasSlenderBuff = true;
            GameObject effect = Instantiate(SlenderBuffPrefab, position, Quaternion.identity);
            effect.transform.SetParent(gameObject.transform);
            GameObject.Find("UIManager").GetComponent<UIManager>().ShowSlenderBuffCountdown(SlenderBuffDuration);
            yield return new WaitForSeconds(SlenderBuffDuration);
            Destroy(effect);
        }

        public void OnLoseGame()
        {
            
        }
    }
}