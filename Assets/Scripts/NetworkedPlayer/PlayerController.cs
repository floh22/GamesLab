using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using Controls.Channeling;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.StructWrapping;
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
        [FormerlySerializedAs("SlenderBuffDuration")] [SerializeField] public float slenderBuffDuration = 45;


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

        private float damageMultiplierMinion;

        [property: SerializeField]
        public float DamageMultiplierMinion
        {
            get { return ReturnMultiplierWithRespectToSlenderBuff(damageMultiplierMinion); }
            set { damageMultiplierMinion = value; }
        }

        private float damageMultiplierAbility1;

        [property: SerializeField]
        public float DamageMultiplierAbility1
        {
            get { return ReturnMultiplierWithRespectToSlenderBuff(damageMultiplierAbility1); }
            set { damageMultiplierAbility1 = value; }
        }

        private float damageMultiplierAbility2;

        [property: SerializeField]
        public float DamageMultiplierAbility2
        {
            get { return ReturnMultiplierWithRespectToSlenderBuff(damageMultiplierAbility2); }
            set { damageMultiplierAbility2 = value; }
        }

        [field: SerializeField] public int UpgradesMinion { get; set; }
        [field: SerializeField] public int UpgradesAbility1 { get; set; }
        [field: SerializeField] public int UpgradesAbility2 { get; set; }

        [field: SerializeField] public int UnspentLevelUps { get; set; }

        #endregion

        [field: SerializeField] public float DeathTimerMax { get; set; } = 15;
        [field: SerializeField] public float DeathTimerCurrently { get; set; } = 0;

        private  GameObject deadPlayerObject;

        #region Public Fields

        [FormerlySerializedAs("DamageText")] [SerializeField] public GameObject damageText;

        [FormerlySerializedAs("DeadPlayerPrefab")] [FormerlySerializedAs("DeadPlayer")] [SerializeField]
        public GameObject deadPlayerPrefab;

        public AudioSource audioSource;
        public AudioClip LevelUpAudioClip;
        public AudioClip SpawnAudioClip;

        [FormerlySerializedAs("SlenderBuffPrefab")] [SerializeField] public GameObject slenderBuffPrefab;
        [FormerlySerializedAs("PagePrefab")] [SerializeField] public GameObject pagePrefab;
        private Page currentPage;


        [SerializeField] public bool HasPage { get; set; }

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

        [SerializeField] private GameObject playerUiPrefab;

        private PlayerUI playerUI;

        [FormerlySerializedAs("ChannelParticleSystem")] [SerializeField] public GameObject channelParticleSystem;
        [FormerlySerializedAs("RingsParticleSystem")] public GameObject ringsParticleSystem;

        private bool isChannelingObjective;
        private Vector3 channelingTo = Vector3.positiveInfinity;

        private IGameUnit self;

        private bool isAutoattackOn;

        private bool isAttacking;

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
                
                GameStateController.LocalPlayerSpawnEvent.Invoke();
            }

            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        public void Start()
        {
            AttachtedObjectInstance = gameObject;
            cameraController = gameObject.GetComponent<CameraController>();

            CurrentlyAttackedBy = new HashSet<IGameUnit>();

            isAutoattackOn = true;
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
                    audioSource = GetComponent<AudioSource>();
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

            // Asssign Layer to player depending on team
            this.gameObject.layer = LayerMask.NameToLayer(this.Team.ToString() + "Units");

            // canvas = GameObject.Find("Canvas");
            // actionButtonsGroupGo = canvas.transform.Find("Ingame_UI").gameObject.transform.Find("Action Buttons Group").gameObject;
        }


        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// Process Inputs if local player.
        /// Show and hide the beams
        /// Watch for end of game, when local player health is 0.
        /// </summary>
        public void Update()
        {
            // if (this.Health <= 0f && IsAlive)
            // {
            //     Die();
            // }

            // we only process Inputs and check health if we are the local player
            if (photonView.IsMine)
            {
                //Make sure to allways check == null and .Equals(null). This is because unity overwrites the Equals method for gameobjects, so we have to check both
                if (!(CurrentAttackTarget == null || CurrentAttackTarget.Equals(null) || isAttacking))
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

                //This should in theory not be needed anymore since pages are now fully networked
                /*
                if (HasPage && CurrentPage == null)
                {
                    SpawnPage();
                }

                if (!HasPage && CurrentPage != null)
                {
                    DestroyPage();
                }
                */
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            // we dont do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }
            
            
            //page code

            if (!HasPage && other.CompareTag("Page"))
            {
                other.gameObject.GetPhotonView().TransferOwnership(PhotonNetwork.LocalPlayer);
                Page page = other.GetComponent<Page>();

                page.Follow(transform);
                
                
                HasPage = true;
                currentPage = page;
                isChannelingObjective = false;
                // Disable the channeling effect
                channelParticleSystem.SetActive(false);
                ringsParticleSystem.SetActive(false);
                Debug.Log($"Page has been picked up by player of {Team} team");
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

        public void AutoAttackOn()
        {
            isAutoattackOn = true;
        }

        public void AutoAttackOff()
        {
            isAutoattackOn = false;
        }

        public void DoDamageVisual(IGameUnit unit, float damage)
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Uncomment next line sometime
            // CurrentlyAttackedBy.Add(unit);

            DamageIndicator indicator = Instantiate(damageText, transform.position, Quaternion.identity)
                .GetComponent<DamageIndicator>();
            indicator.SetDamageText(damage);
        }


        public void Die()
        {
            IsAlive = false;

            if (HasPage)
            {
                DropPageOnTheGround();
                HasPage = false;
            }

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

            GameObject playerUiGo = playerUI.gameObject;
            playerUiGo.SetActive(false);
            // actionButtonsGroupGo.SetActive(false); 

            UIManager.Instance.ShowDeathIndicatorCountdown(DeathTimerMax);         
                                            
            TakeAwayCameraFromPlayer();
        }

        private void TakeAwayCameraFromPlayer()
        {
            // Stop following alive character
            cameraController.OnStopFollowing();

            //create dead character
            Vector3 position = transform.position;
            deadPlayerObject = Instantiate(deadPlayerPrefab, position, Quaternion.identity);
            CameraController deadCameraController = deadPlayerObject.GetComponent<CameraController>();
            //follow dead character
            deadCameraController.OnStartFollowing();
        }        

        public void PutCameraBackOnPlayer()
        {
            CameraController deadCameraController = deadPlayerObject.GetComponent<CameraController>();

            //Start following player again
            deadCameraController.OnStopFollowing();
            cameraController.OnStartFollowing();         
            
            // Destroy dead player
            Destroy(deadPlayerObject);
        }

        public void RespawnEnded()
        {
            GameObject playerUiGo = playerUI.gameObject;
            playerUiGo.SetActive(true);
            // actionButtonsGroupGo.SetActive(true);  
            
            if (!photonView.IsMine)
            {
                return;
            }
            photonView.RPC("playSpawnAudio", RpcTarget.All, this.NetworkID);
            
            //Reset stats
            IsAlive = true;
            this.Health = this.MaxHealth;

        }

        public void DieEnded()
        {
            IsAlive = false;
        }

        public void OnStartSlendermanChannel(Vector3 slendermanSize)
        {
            // Enable channeling effects when channeling Slenderman on the channeler.
            // The channeling effect on Slenderman will be activated in the OnCollision.cs script
            // when the particles from the channeler hit Slenderman.

            this.channelParticleSystem.SetActive(true);

            float hSliderValueR = 255.0F / 255;
            float hSliderValueG = 0.0F / 255;
            float hSliderValueB = 221.0F / 255;
            float hSliderValueA = 255.0F / 255;   
            Color color = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);

            ParticleSystem channelParticleSystem = this.channelParticleSystem.GetComponent<ParticleSystem>();
            var channelParticleSystemMain = channelParticleSystem.main;
            channelParticleSystemMain.startColor = color;

            // Set the force that will change the particles direcion
            var fo = channelParticleSystem.forceOverLifetime;
            fo.enabled = true;

            fo.x = new ParticleSystem.MinMaxCurve(channelingTo.x - transform.position.x);
            fo.y = new ParticleSystem.MinMaxCurve(-channelingTo.y + transform.position.y + (slendermanSize.y / 2));
            fo.z = new ParticleSystem.MinMaxCurve(channelingTo.z - transform.position.z);            

            /* Start of Rings Channeling Effect Stuff */
            this.ringsParticleSystem.SetActive(true);
            ParticleSystem ringsParticleSystem = this.ringsParticleSystem.GetComponent<ParticleSystem>();
            ringsParticleSystem.Play(true);       

            ParticleSystem embers = this.ringsParticleSystem.transform.Find("Embers").gameObject.GetComponent<ParticleSystem>();
            ParticleSystem smoke = this.ringsParticleSystem.transform.Find("Smoke").gameObject.GetComponent<ParticleSystem>();
            var embersMain = embers.main;
            var smokeMain = smoke.main;
            embersMain.startColor = color;
            smokeMain.startColor = color;
            
            Gradient gradient;
            GradientColorKey[] colorKey;
            GradientAlphaKey[] alphaKey;
            gradient = new Gradient();
            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            colorKey = new GradientColorKey[2];
            colorKey[0].color = color;
            colorKey[0].time = 0.0f;
            colorKey[1].color = color;
            colorKey[1].time = 1.0f;
            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 0.0f;
            alphaKey[1].time = 1.0f;
            gradient.SetKeys(colorKey, alphaKey);       

            ParticleSystem.MainModule ringsParticleSystemMain = ringsParticleSystem.main;
            ringsParticleSystemMain.startColor = new ParticleSystem.MinMaxGradient(gradient);
            /* End of Rings Channeling Effect Stuff */
        }        

        public void OnStartBaseChannel()
        {
            // Enable channeling effects on the channeler when channeling a base.
            // The channeling effect the base will be activated in the OnCollision.cs script
            // when the particles from the channeler hit it.

            this.channelParticleSystem.SetActive(true);
            
            // Set this in the player themselves
            ParticleSystem channelParticleSystem = this.channelParticleSystem.GetComponent<ParticleSystem>();

            // Set the force that will change the particles direcion
            var fo = channelParticleSystem.forceOverLifetime;
            fo.enabled = true;

            fo.x = new ParticleSystem.MinMaxCurve(channelingTo.x - transform.position.x);
            fo.y = new ParticleSystem.MinMaxCurve(-channelingTo.y + transform.position.y);
            fo.z = new ParticleSystem.MinMaxCurve(channelingTo.z - transform.position.z);

            // Set particles color
            float hSliderValueR = 0.0f;
            float hSliderValueG = 0.0f;
            float hSliderValueB = 0.0f;
            float hSliderValueA = 1.0f;


            if(Team.ToString() == "RED")
            {
                // // Set particles color
                // hSliderValueR = 174.0F / 255;
                // hSliderValueG = 6.0F / 255;
                // hSliderValueB = 6.0F / 255;
                // hSliderValueA = 255.0F / 255;

                // Set particles color
                hSliderValueR = 1;
                hSliderValueG = 0;
                hSliderValueB = 0;
                hSliderValueA = 1;                    
            }
            else if (Team.ToString() == "YELLOW")
            {
                // // Set particles color
                // hSliderValueR = 171.0F / 255;
                // hSliderValueG = 173.0F / 255;
                // hSliderValueB = 6.0F / 255;
                // hSliderValueA = 255.0F / 255;

                // Set particles color
                hSliderValueR = 1;
                hSliderValueG = 1;
                hSliderValueB = 0;
                hSliderValueA = 1;                     
            }
            else if (Team.ToString() == "GREEN")
            {
                // // Set particles color
                // hSliderValueR = 7.0F / 255;
                // hSliderValueG = 173.0F / 255;
                // hSliderValueB = 16.0F / 255;
                // hSliderValueA = 255.0F / 255;

                // Set particles color
                hSliderValueR = 0;
                hSliderValueG = 1;
                hSliderValueB = 0;
                hSliderValueA = 1;                    
            }
            else if (Team.ToString() == "BLUE")
            {
                // // Set particles color
                // hSliderValueR = 7.0F / 255;
                // hSliderValueG = 58.0F / 255;
                // hSliderValueB = 173.0F / 255;
                // hSliderValueA = 255.0F / 255;

                // Set particles color
                hSliderValueR = 0;
                hSliderValueG = 1;
                hSliderValueB = 1;
                hSliderValueA = 1;                      
            }

            Color color = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);
            var channelParticleSystemMain = channelParticleSystem.main;
            channelParticleSystemMain.startColor = color;

            /* Start of Rings Channeling Effect Stuff */
            this.ringsParticleSystem.SetActive(true);

            ParticleSystem ringsParticleSystem = this.ringsParticleSystem.GetComponent<ParticleSystem>();
            ringsParticleSystem.Play(true);       

            ParticleSystem embers = this.ringsParticleSystem.transform.Find("Embers").gameObject.GetComponent<ParticleSystem>();
            ParticleSystem smoke = this.ringsParticleSystem.transform.Find("Smoke").gameObject.GetComponent<ParticleSystem>();
            var embersMain = embers.main;
            var smokeMain = smoke.main;
            embersMain.startColor = color;
            smokeMain.startColor = color;
            
            Gradient gradient;
            GradientColorKey[] colorKey;
            GradientAlphaKey[] alphaKey;
            gradient = new Gradient();
            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            colorKey = new GradientColorKey[2];
            colorKey[0].color = color;
            colorKey[0].time = 0.0f;
            colorKey[1].color = color;
            colorKey[1].time = 1.0f;
            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 0.0f;
            alphaKey[1].time = 1.0f;
            gradient.SetKeys(colorKey, alphaKey);       

            ParticleSystem.MainModule ringsParticleSystemMain = ringsParticleSystem.main;
            ringsParticleSystemMain.startColor = new ParticleSystem.MinMaxGradient(gradient);
            /* End of Rings Channeling Effect Stuff */
        }

        public void SetChannelingTo(Vector3 channelingToP)
        {
            channelingTo = channelingToP;
        }

        public void DisableChannelEffects()
        {
            channelParticleSystem.SetActive(false);
            ringsParticleSystem.SetActive(false);
            isChannelingObjective = false;
        }
        
        public void OnChannelObjective(Vector3 objectivePosition, int networkId)
        {
            isChannelingObjective = true;
            channelingTo = objectivePosition;
            GameStateController.SendStartChannelEvent(Team, networkId);
        }

        // This function disables channeling effects for other players (so not the local player)
        // and does nothing more than that. The 999 indicates that we only want to disable channeling
        // effects and nothing more.
        public void DisableChannelEffectsNetworked(int networkId)
        {
            GameStateController.SendFinishChannelEvent(Team, networkId, 999);
        }

        public void OnChannelingFinishedAndReceiveSlendermanBuff(int networkId)
        {
            OnReceiveSlendermanBuff();
            GameStateController.SendFinishChannelEvent(Team, networkId, 0);
        }

        public void OnChannelingFinishedAndPickUpPage(int networkId, int pages)
        {
            SpawnPage();
            GameStateController.SendFinishChannelEvent(Team, networkId, pages);
        }

        public void OnChannelingFinishedAndDropPage(int networkId, int pages)
        {
            DestroyPage();
            HasPage = false;
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
            channelParticleSystem.SetActive(false);
            ringsParticleSystem.SetActive(false);
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
                // TODO sync damage as well. Cause we have dmg increased during Slenderman buff.
                // Local player, send data
                stream.SendNext(this.NetworkID);
                stream.SendNext(this.Health);
                stream.SendNext(this.Team);
                stream.SendNext(this.Level);
                stream.SendNext(this.isChannelingObjective);
                stream.SendNext(this.channelingTo);
                stream.SendNext(this.HasPage);
            }
            else
            {
                // Network player, receive data
                this.NetworkID = (int) stream.ReceiveNext();
                this.Health = (float) stream.ReceiveNext();
                this.Team = (GameData.Team) stream.ReceiveNext();
                this.Level = (int) stream.ReceiveNext();
                this.isChannelingObjective = (bool) stream.ReceiveNext();
                this.channelingTo = (Vector3) stream.ReceiveNext();
                this.HasPage = (bool) stream.ReceiveNext();
            }
        }

        public void AddExperience(int amount)
        {
            if (Level >= MaxLevel)
            {
                return;
            }

            Experience += amount;
            if (Experience < ExperienceToReachNextLevel) return;
            
            Level++;
            UnspentLevelUps++;
            Experience -= ExperienceToReachNextLevel;
            ExperienceToReachNextLevel += ExperienceBetweenLevels;
            this.PlayLevelUpAudio();
            StartCoroutine(UIManager.Instance.ShowLevelUpLabel());
        }

        public void AddExperienceBySource(bool byMinion)
        {
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
        
        [PunRPC]
        public void playSpawnAudio(int networkID)
        {
            Debug.Log("Here"+networkID+"|"+this.NetworkID);
            Debug.Log("Here"+this.Team);
            if (this.NetworkID != networkID)
            {
                return;
            }
            audioSource.clip = SpawnAudioClip;
            audioSource.Play();
        }

        private void PlayLevelUpAudio()
        {
            audioSource.clip = LevelUpAudioClip;
            audioSource.Play();
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
            AutoObjectParenter.SendParentEvent(AutoObjectParenter.ParentEventTarget.SLENDERMAN, gameObject);
            UIManager.Instance.ShowSlenderBuffCountdown(slenderBuffDuration);
            yield return new WaitForSeconds(slenderBuffDuration);
            PhotonNetwork.Destroy(effect);
        }

        public void OnLoseGame()
        {
        }
        

        public void SacrificePage()
        {
            DestroyPage();
            HasPage = false;
            isChannelingObjective = false;
            Debug.Log($"Page has been sacrifised by player of {Team} team");
        }

        private void DropPageOnTheGround()
        {
            //Cant drop a page that doesnt exist
            if (currentPage == null) return;
            
            currentPage.Drop();
            Debug.Log($"Page has been dropped on the ground by player of {Team} team");
        }

        #endregion

        #region Utils

        private void SpawnPage()
        {
            Vector3 position = transform.position;
            GameObject pageObject = PhotonNetwork.Instantiate("Page", position, Quaternion.identity);
            currentPage = pageObject.GetComponent<Page>();
            currentPage.Follow(this.transform);
            HasPage = true;
        }

        private void DestroyPage()
        {
            PhotonNetwork.Destroy(currentPage.gameObject);
            HasPage = false;
        }

        #endregion

        #region Coroutines

        private IEnumerator Attack(float damage)
        {
            if (isChannelingObjective || !isAutoattackOn)
            {
                yield break;
            }

            isAttacking = true;
            // OnAttacking();
            CurrentAttackTarget.AddAttacker(self);
            GameStateController.SendPlayerAutoAttackEvent(Team);
            ((IGameUnit) this).SendDealDamageEvent(CurrentAttackTarget, damage);
            float pauseInSeconds = 1f * AttackSpeed;
            yield return new WaitForSeconds(pauseInSeconds / 2);
            // OnRest();
            yield return new WaitForSeconds(pauseInSeconds / 2);
            isAttacking = false;
        }

        // public IEnumerator Respawn(Action nextFunc)
        // {
        //     //wait out death timer
        //     DeathTimerCurrently = DeathTimerMax;

        //     while (DeathTimerCurrently > 0)
        //     {
        //         DeathTimerCurrently = Mathf.Max(0, DeathTimerCurrently - 0.1f);
        //         yield return new WaitForSeconds(0.1f);
        //     }

        //     nextFunc.Invoke();
        // }

        #endregion
    }
}