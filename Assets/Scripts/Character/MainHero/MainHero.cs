using System.Collections;
using System.Collections.Generic;
using GameManagement;
using Photon.Pun;
using UnityEngine;
using Utils;

namespace Character.MainHero
{
    public class MainHero: MonoBehaviour, IGameUnit
    {

        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        [field: SerializeField] public GameUnitType Type { get; } = GameUnitType.MainPlayer;
        public Vector3 Position => transform.position;

        [field: SerializeField] public float MaxHealth { get; set; } = MainHeroValues.MaxHealth;
        [field: SerializeField] public float Health { get; set; } = MainHeroValues.MaxHealth;
        [field: SerializeField] public float MoveSpeed { get; set; } = MainHeroValues.MoveSpeed;
        public float RotationSpeed { get; set; }
        [field: SerializeField] public float AttackDamage { get; set; } = MainHeroValues.AttackDamage;
        [field: SerializeField] public float AttackSpeed { get; set; } = MainHeroValues.AttackSpeed;
        [field: SerializeField] public float AttackRange { get; set; } = MainHeroValues.AttackRange;
        [field: SerializeField] public IGameUnit CurrentAttackTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; }
        private new Transform transform;
        private MainHeroHealthBar healthBar;
        private IGameUnit self;
        private bool isAttacking;
        private bool isAttacked;
        private new MeshRenderer renderer;

        void Start()
        {
            renderer = GetComponent<MeshRenderer>();
            transform = GetComponent<Transform>();
            self = GetComponent<IGameUnit>();
            healthBar = GetComponentInChildren<MainHeroHealthBar>();
            healthBar.SetName(Type.ToString());
            healthBar.SetHP(MaxHealth);
            Health = MaxHealth;
        }

        void Update()
        {
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
        
        public void Damage(IGameUnit unit, float damage)
        {
            Health -= damage;
            healthBar.SetHP(Health);
            if (isAttacked)
            {
                return;
            }
            StartCoroutine(Attacked());
        }

        public bool IsDestroyed()
        {
            throw new System.NotImplementedException();
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            throw new System.NotImplementedException();
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<IGameUnit>();
            if (target == null)
            {
                return;
            }

            if (target == self)
            {
                return;
            }

            if (CurrentAttackTarget != null)
            {
                return;
            }

            CurrentAttackTarget = target;
            Debug.Log("OnTriggerEnter: " + target.Type);
        }
        
        private void OnTriggerExit(Collider other)
        {
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
        
        private IEnumerator Attack()
        {
            isAttacking = true;
            OnAttacking();
            CurrentAttackTarget.AddAttacker(self);
            CurrentAttackTarget.Damage(self, AttackDamage);
            float pauseInSeconds = 1f * AttackSpeed;
            yield return new WaitForSeconds(pauseInSeconds / 2);
            OnRest();
            yield return new WaitForSeconds(pauseInSeconds / 2);
            isAttacking = false;
        }
        
        private IEnumerator Attacked()
        {
            isAttacked = true;
            OnAttacked();
            yield return new WaitForSeconds(AttackConstants.AttackedAnimationDuration);
            OnRest();
            isAttacked = false;
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
    }
}