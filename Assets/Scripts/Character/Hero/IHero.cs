using System;
using System.Collections;
using Character.Stats;
using UnityEngine;
using Utils;

namespace Character.Hero
{
    public abstract class IHero: ITargetable
    {
        private HeroStats _stats;
        private MeshRenderer _renderer;
        private bool _ready;
        private bool _isAttacked;

        public float AttackRange() => _stats.attackRange;

        public float Damage() => _stats.damage;

        public bool IsReady() => _ready;

        void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
            _stats = GetComponentInChildren<HeroStats>();
            _ready = true;
            CheckObjects();
        }

        public void OnMouseDown()
        {
            throw new NotImplementedException();
        }

        public override void OnAttack()
        {
            _renderer.material.color = Color.blue;
        }
        
        public override void OnAttacked(float dmg)
        {
            _stats.OnAttacked(dmg);
            if (_isAttacked)
            {
                return;
            }
            StartCoroutine(AttackedCoroutine());
        }
        
        public override void OnRest()
        {
            _renderer.material.color = Color.grey;
        }

        private void CheckObjects()
        {
            ValidationUtils.RequireNonNull(_renderer);
            ValidationUtils.RequireNonNull(_stats);
        }
        
        private IEnumerator AttackedCoroutine()
        {
            _isAttacked = true;
            var material = _renderer.material;
            material.color = Color.red;
            yield return new WaitForSeconds(AttackConstants.AttackedAnimationDuration);
            material.color = Color.grey;
            _isAttacked = false;
        }
    }
}