using System.Collections;
using System.Collections.Generic;
using GameManagement;
using Photon.Pun;
using UnityEngine;
using Utils;

namespace Character.SecondaryHero
{
    public class SecondaryHero: MonoBehaviour, IGameUnit
    {
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            throw new System.NotImplementedException();
        }

        public int NetworkID { get; set; }
        public GameData.Team Team { get; set; }
        [field: SerializeField] public GameUnitType Type { get; } = GameUnitType.Player;
        public Vector3 Position => transform.position;
        [field: SerializeField] public float MaxHealth { get; set; } = SecondaryHeroValues.MaxHealth;
        [field: SerializeField] public float Health { get; set; } = SecondaryHeroValues.MaxHealth;
        [field: SerializeField] public float MoveSpeed { get; set; } = SecondaryHeroValues.MoveSpeed;
        public float RotationSpeed { get; set; }
        [field: SerializeField] public float AttackDamage { get; set; } = SecondaryHeroValues.AttackDamage;
        [field: SerializeField] public float AttackSpeed { get; set; } = SecondaryHeroValues.AttackSpeed;
        [field: SerializeField] public float AttackRange { get; set; } = SecondaryHeroValues.AttackRange;
        public IGameUnit CurrentAttackTarget { get; set; }
        public HashSet<IGameUnit> CurrentlyAttackedBy { get; set; } = new();
        private SecondaryHeroHealthBar healthBar;
        private bool isAttacked;
        private new MeshRenderer renderer;

        void Start()
        {
            renderer = GetComponent<MeshRenderer>();
            healthBar = GetComponentInChildren<SecondaryHeroHealthBar>();
            healthBar.SetName(Type.ToString());
            healthBar.SetHP(MaxHealth);
            Health = MaxHealth;
        }
        
        public bool IsDestroyed()
        {
            throw new System.NotImplementedException();
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