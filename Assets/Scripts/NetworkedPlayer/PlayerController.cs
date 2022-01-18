using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using Controls.Channeling;
using ExitGames.Client.Photon;
using GameManagement;
using Network;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Utils;

namespace NetworkedPlayer
{
    public class PlayerController : MonoBehaviourPunCallbacks, IGameUnit
    {
        #region StaticFields

        public static GameObject LocalPlayerInstance;
        public static PlayerController LocalPlayerController;
        #endregion

        #region IGameUnit

        public int NetworkID { get; set; }

        public int OwnerID => photonView.OwnerActorNr;

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
        public bool IsChannelingObjective => isChannelingObjective;
        public bool IsVisible { get; set; }
        [SerializeField] public bool HasSlenderBuff { get; set; }
        [SerializeField] public float SlenderBuffDuration = 45;
        

        public IGameUnit CurrentAttackTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; }

        #endregion

        #region Level

        [field: SerializeField] public int Level { get; set; }
        [field: SerializeField] public int MaxLevel { get; set; }
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

        [field: SerializeField] public int UnspentLevelUps { get; set; }

        #endregion
        
        [field: SerializeField] public float DeathTimerMax { get; set; } = 15;
        [field: SerializeField] public float DeathTimerCurrently { get; set; } = 0;

        #region Public Fields

        [SerializeField] public GameObject DamageText;

        [FormerlySerializedAs("DeadPlayer")] [SerializeField]
        public GameObject DeadPlayerPrefab;
        [SerializeField] public GameObject SlenderBuffPrefab;

        public bool HasPage
        {
            get => hasPage;
            set
            {
                if (hasPage != value)
                {
                    if (value)
                        PickUpPage();
                    else
                        DropPage();
                }

                hasPage = value;
            }
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

        #endregion

        #region Private Fields
        
        private CameraController cameraController;

        [SerializeField] private bool hasPage;

        [SerializeField] private GameObject playerUiPrefab;

        private PlayerUI playerUI;

        [SerializeField]
        public GameObject ChannelParticleSystem;
        
        private bool isChannelingObjective;
        private Vector3 channelingTo = Vector3.positiveInfinity;
        
        private IGameUnit self;
        
        private bool isAttacking;
        
        private bool isAttacked;
        
        private Page page;

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        public void Awake()
        {
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
            
            GameStateController.LocalPlayerSpawnEvent.Invoke();
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        public void Start()
        {
            AttachtedObjectInstance = gameObject;
            cameraController = gameObject.GetComponent<CameraController>();
           
            page = GetComponentInChildren<Page>();

            CurrentlyAttackedBy = new HashSet<IGameUnit>();

            //TODO temp
            MaxHealth = PlayerValues.MaxHealth;
            Health = MaxHealth;
            MoveSpeed = PlayerValues.MoveSpeed;
            AttackDamage = PlayerValues.AttackDamage;
            AttackSpeed = PlayerValues.AttackSpeed;
            AttackRange = PlayerValues.AttackRange;
            Level = 1;
            MaxLevel = 13;
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

            self = gameObject.GetComponent<IGameUnit>();

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

            if (photonView.IsMine)
            {
                NetworkID = gameObject.GetInstanceID();
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
                if (this.Health <= 0f && IsAlive)
                {
                    Die();
                }
                
                if (!(CurrentAttackTarget == null || isAttacking))
                {
                    if (Vector3.Distance(CurrentAttackTarget.Position, Position) > AttackRange)
                    {
                        return;
                    }

                    switch (CurrentAttackTarget.Type)
                    {
                        case GameUnitType.Player:
                            StartCoroutine(Attack(AttackDamage));
                            break;
                        case GameUnitType.Minion:
                            StartCoroutine(Attack(AttackDamage * PlayerValues.AttackDamageMinionsMultiplier));
                            break;
                    }
                }

                
            }

            if (HasPage && !page.IsActive)
            {
                ShowPage();
            }
            if (!HasPage && page.IsActive)
            {
                HidePage();
            }

            if (this.ChannelParticleSystem != null && this.isChannelingObjective != this.ChannelParticleSystem.activeInHierarchy)
            {
                this.ChannelParticleSystem.SetActive(this.isChannelingObjective);
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            // we dont do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }

            if (other.gameObject == gameObject)
                return;

            var target = other.GetComponent<IGameUnit>();

            if (target == self)
            {
                return;
            }

            if (target?.Team == self?.Team)
            {
                return;
            }

            if (CurrentAttackTarget != null)
            {
                return;
            }

            CurrentAttackTarget = target;
        }

        private void OnTriggerExit(Collider other)
        {
            // we dont do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }
            
            var target = other.GetComponent<IGameUnit>();
            if (target == null)
            {
                return;
            }
            
            if (target == self)
            {
                return;
            }

            if (target == CurrentAttackTarget || CurrentAttackTarget == null)
            {
                CurrentAttackTarget = null;
            }

        }

        #endregion

        #region Public API

        public void DoDamageVisual(IGameUnit unit, float damage)
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Uncomment next line sometime
            // CurrentlyAttackedBy.Add(unit);

            DamageIndicator indicator = Instantiate(DamageText, transform.position, Quaternion.identity)
                .GetComponent<DamageIndicator>();
            indicator.SetDamageText(damage);
        }
        

        public void Die()
        {
            IsAlive = false;
            
            DropPage();

            //remove attackers
            foreach (IGameUnit gameUnit in CurrentlyAttackedBy)
            {
                if (gameUnit.Type == GameUnitType.Player && Vector3.Distance(gameUnit.Position, Position) <
                    IGameUnit.DistanceForExperienceOnDeath)
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
                Destroy(deadPlayerObject);        }));
            
        }

        public void OnChannelObjective(Vector3 objectivePosition, int networkId)
        {
            isChannelingObjective = true;
            channelingTo = objectivePosition;
            GameStateController.SendStartChannelEvent(Team, networkId);
        }

        public void OnChannelingFinishedAndReceiveSlendermanBuff(int networkId)
        {
            OnReceiveSlendermanBuff();
            GameStateController.SendFinishChannelEvent(Team, networkId, 0);
        }
        
        public void OnChannelingFinishedAndPickUpPage(int networkId, int pages)
        {
            PickUpPage();
            GameStateController.SendFinishChannelEvent(Team, networkId, pages);
        }
        
        public void OnChannelingFinishedAndDropPage(int networkId, int pages)
        {
            DropPage();
            GameStateController.SendFinishChannelEvent(Team, networkId, pages);
        }
        
        public void InterruptChanneling()
        {
            if (!isChannelingObjective)
            {
                return;
            }
            isChannelingObjective = false;
            channelingTo = Vector3.positiveInfinity;

            // Disable the channeling effect
            ChannelParticleSystem.SetActive(false);
            Debug.Log($"Player's channeling from team {Team} has been interrupted");
        }

        public void OnReceiveSlendermanBuff()
        {
            isChannelingObjective = false;
            StartCoroutine(SlenderBuffCoroutine());
            Debug.Log($"Player from team {Team} received the buff");
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // TODO sync damage as well
                // Local player, send data
                stream.SendNext(this.NetworkID);
                stream.SendNext(this.Health);
                stream.SendNext(this.Team);
                stream.SendNext(this.Level);
                stream.SendNext(this.isChannelingObjective);
                stream.SendNext(this.channelingTo);
                stream.SendNext(this.hasPage);
            }
            else
            {
                // Network player, receive data
                this.NetworkID = (int)stream.ReceiveNext();
                this.Health = (float) stream.ReceiveNext();
                this.Team = (GameData.Team) stream.ReceiveNext();
                this.Level = (int)stream.ReceiveNext();
                this.isChannelingObjective = (bool)stream.ReceiveNext();
                this.channelingTo = (Vector3)stream.ReceiveNext();
                this.hasPage = (bool) stream.ReceiveNext();
            }
        }

        public void AddExperience(int amount)
        {
            if (Level >= MaxLevel)
            {
                return;
            }
            Experience += amount;
            if (Experience >= ExperienceToReachNextLevel)
            {
                Level++;
                UnspentLevelUps++;
                Experience -= ExperienceToReachNextLevel;
                ExperienceToReachNextLevel += ExperienceBetweenLevels;
                StartCoroutine(UIManager.Instance.ShowLevelUpLabel());
            }
        }

        public void AddExperienceBySource(bool byMinion)
        {
            Debug.Log(byMinion);
            AddExperience(byMinion ? GainedExperienceByMinion : GainedExperienceByPlayer);
        }

        public void UpdateMultiplier(int whatToUpdate)
        {
            if (UnspentLevelUps <= 0)
            {
                return;
            }

            UnspentLevelUps--;
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

        private float ReturnMultiplierWithRespectToSlenderBuff(float mulitplier)
        {
            return HasSlenderBuff ? mulitplier * 2 : mulitplier;
        }

        IEnumerator SlenderBuffCoroutine()
        {
            Vector3 position = transform.position;
            HasSlenderBuff = true;
            GameObject effect = PhotonNetwork.Instantiate("SlenderBuffVisual", position, Quaternion.identity);
            // GameObject effect = Instantiate(SlenderBuffPrefab, position, Quaternion.identity);
            AutoObjectParenter.SendParentEvent(gameObject);
            UIManager.Instance.ShowSlenderBuffCountdown(SlenderBuffDuration);
            yield return new WaitForSeconds(SlenderBuffDuration);
            PhotonNetwork.Destroy(effect);
        }

        public void OnLoseGame()
        {
        }
        
        public void PickUpPage()
        {
            ShowPage();
            hasPage = true;
            isChannelingObjective = false;
            // Disable the channeling effect
            ChannelParticleSystem.SetActive(false);
            Debug.Log($"Page has been picked up by player of {Team} team");
        }
        
        public void SacrifisePage()
        {
            HidePage();
            hasPage = false;
            isChannelingObjective = false;
            Debug.Log($"Page has been sacrifised by player of {Team} team");
        }

        public void DropPage()
        {
            HidePage();
            hasPage = false;
            Debug.Log($"Page has been dropped by player of {Team} team");
        }

        #endregion

        #region Utils
        
        private void ShowPage()
        {
            page.TurnOn();
        }
		
        private void HidePage()
        {
            page.TurnOff();
        }

        #endregion

        #region Coroutines
        
        private IEnumerator Attack(float damage)
        {
            if (isChannelingObjective)
            {
                yield break;
            }
            isAttacking = true;
            // OnAttacking();
            CurrentAttackTarget.AddAttacker(self);
            ((IGameUnit)this).SendDealDamageEvent(CurrentAttackTarget, damage);
            float pauseInSeconds = 1f * AttackSpeed;
            yield return new WaitForSeconds(pauseInSeconds / 2);
            // OnRest();
            yield return new WaitForSeconds(pauseInSeconds / 2);
            isAttacking = false;
        }
        
        private IEnumerator Attacked()
        {
            isAttacked = true;
            // OnAttacked();
            yield return new WaitForSeconds(GameConstants.AttackedAnimationDuration);
            // OnRest();
            isAttacked = false;
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

        #endregion
    }
}