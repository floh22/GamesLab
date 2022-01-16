using System;
using System.Collections;
using System.Collections.Generic;
using GameManagement;
using Network;
using NetworkedPlayer;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

namespace Character.MainHero
{
    public class MainHero: MonoBehaviourPunCallbacks, IGameUnit
    {

        #region StaticFields

        public static GameObject LocalHeroInstance;

        #endregion

        private CameraController cameraController;

        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        [field: SerializeField] public GameUnitType Type { get; } = GameUnitType.Player;
        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public GameObject AttachtedObjectInstance { get; set; }

        [field: SerializeField] public float MaxHealth { get; set; }
        [field: SerializeField] public float Health { get; set; }
        [field: SerializeField] public float MoveSpeed { get; set; }
        public float RotationSpeed { get; set; }
        [field: SerializeField] public float AttackDamage { get; set; }
        [field: SerializeField] public float AttackSpeed { get; set; }
        [field: SerializeField] public float AttackRange { get; set; }
        public bool IsAlive { get; set; }
        public bool IsVisible { get; set; }
        [field: SerializeField] public IGameUnit CurrentAttackTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; }

        #region Private Fields
        
        [SerializeField] private GameObject playerUiPrefab;
        private new Transform transform;
        private MainHeroHealthBar healthBar;
        private IGameUnit self;
        private bool isAttacking;
        private bool isAttacked;
        private new MeshRenderer renderer;
        
        #endregion

        #region MonoBehaviour CallBacks

        public void Awake()
        {
            // in GameStateController we keep track of the localPlayer instance
            // to prevent instantiation when levels are synchronized
            if (photonView.IsMine)
            {
                LocalHeroInstance = gameObject;
                Team = PersistentData.Team ?? throw new NullReferenceException();
            }
            // we flag as don't destroy on load so that instance
            // survives level synchronization,
            // thus giving a seamless experience when levels load.
            DontDestroyOnLoad(gameObject);
        }
        
        void Start()
        {
            cameraController = gameObject.GetComponent<CameraController>();

            NetworkID = gameObject.GetInstanceID();

            CurrentlyAttackedBy = new HashSet<IGameUnit>();

            MaxHealth = MainHeroValues.MaxHealth;
            Health = MaxHealth;
            MoveSpeed = MainHeroValues.MoveSpeed;
            AttackDamage = MainHeroValues.AttackDamage;
            AttackSpeed = MainHeroValues.AttackSpeed;
            AttackRange = MainHeroValues.AttackRange;
            
            renderer = GetComponent<MeshRenderer>();
            transform = GetComponent<Transform>();
            self = gameObject.GetComponent<IGameUnit>();
            healthBar = GetComponentInChildren<MainHeroHealthBar>();
            healthBar.SetName(Type.ToString());
            healthBar.SetHP(MaxHealth);
            Health = MaxHealth;

            if (cameraController != null)
            {
                // TODO Remove? Because MainHero is always owns photonView
                if (photonView.IsMine)
                {
                    Debug.Log("OnStartFollowing");
                    cameraController.OnStartFollowing();
                }
            }
            else
            {
                Debug.LogError("<Color=Red><b>Missing</b></Color> CameraWork Component on MainHero Prefab.", this);
            }

            if (playerUiPrefab != null)
            {
                GameObject uiGameObject = Instantiate(playerUiPrefab);
                uiGameObject.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
            else
            {
                Debug.LogWarning("<Color=Red><b>Missing</b></Color> PlayerUiPrefab reference on player Prefab.", this);
            }
            
#if UNITY_5_4_OR_NEWER
            // Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
#endif
        }

        public override void OnDisable()
        {
            base.OnDisable();
            
#if UNITY_5_4_OR_NEWER
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
#endif
        }
        
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode loadingMode)
        {
            this.CalledOnLevelWasLoaded(scene.buildIndex);
        }
        
        private void CalledOnLevelWasLoaded(int level)
        {
            if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
            {
                transform.position = new Vector3(0f, 5f, 0f);
            }

            GameObject uiGo = Instantiate(playerUiPrefab);
            uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
        }

        void Update()
        {
            if (photonView.IsMine)
            {
                if (Health <= 0f)
                {
                    Die();
                }
            }
            
            if (CurrentAttackTarget == null || isAttacking)
            {
                return;
            }

            if (Vector3.Distance(CurrentAttackTarget.Position, Position) > AttackRange)
            {
                Debug.Log($"CATP = {CurrentAttackTarget.Position} > P = {Position}");
                Debug.Log($"Distance = {Vector3.Distance(CurrentAttackTarget.Position, Position)} > Attack Range = {AttackRange}");
                return;
            }

            switch (CurrentAttackTarget.Type)
            {
                case GameUnitType.Player:
                    StartCoroutine(Attack());
                    break;
                case GameUnitType.Minion:
                    // TODO implement
                    break;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // we dont do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }
            Debug.Log("OnTriggerEnter: Photon view is mine");
            
            var target = other.GetComponent<IGameUnit>();
            if (target == null)
            {
                return;
            }
            Debug.Log("OnTriggerEnter: Target is not null");

            if (target == self)
            {
                return;
            }
            Debug.Log("OnTriggerEnter: Target is not self");

            if (CurrentAttackTarget != null)
            {
                return;
            }
            Debug.Log("OnTriggerEnter: Have no current attack target");

            CurrentAttackTarget = target;
            Debug.Log("OnTriggerEnter: " + target.Type);
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

            if (target == CurrentAttackTarget)
            {
                CurrentAttackTarget = null;
            }

            Debug.Log("OnTriggerExit: " + target.Type);
        }

        #endregion

        #region Photon

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Local player, send data
                stream.SendNext(Health);
                stream.SendNext(Team);
            }
            else
            {
                // Network player, receive data
                Health = (float) stream.ReceiveNext();
                Team = (GameData.Team) stream.ReceiveNext();
            }
        }

        #endregion
        
        #region Utils

        public bool IsDestroyed()
        {
            return !gameObject;
        }
        
        private void Die()
        {
            cameraController.OnStopFollowing();
            StartCoroutine(Respawn());
        }
        
        public void Damage(IGameUnit unit, float damage)
        {
            CurrentlyAttackedBy.Add(unit);
            
            Health -= damage;
            healthBar.SetHP(Health);
            if (isAttacked)
            {
                return;
            }
            StartCoroutine(Attacked());
        }

        private void OnAttacking()
        {
            renderer.material.color = Color.green;
        }
        
        private void OnRest()
        {
            renderer.material.color = Color.white;
        }
        
        private void OnAttacked()
        {
            renderer.material.color = Color.red;
        }

        #endregion

        #region Coroutines

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(GameConstants.RespawnTime);
            Health = MaxHealth;
        }
        
        private IEnumerator Attack()
        {
            isAttacking = true;
            OnAttacking();
            CurrentAttackTarget.AddAttacker(self);
            CurrentAttackTarget.Damage(self, AttackDamage);
            Debug.Log("Damaged my target");
            float pauseInSeconds = 1f * AttackSpeed;
            yield return new WaitForSeconds(pauseInSeconds / 2);
            OnRest();
            yield return new WaitForSeconds(pauseInSeconds / 2);
            isAttacking = false;
            Debug.Log("Done attacking my target");
        }
        
        private IEnumerator Attacked()
        {
            isAttacked = true;
            OnAttacked();
            yield return new WaitForSeconds(GameConstants.AttackedAnimationDuration);
            OnRest();
            isAttacked = false;
        }


        #endregion
        
    }
}