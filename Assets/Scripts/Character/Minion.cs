using System.Collections;
using Character.Stats;
using UnityEngine;
using Utils;

namespace Character
{
    public class Minion : ITargetable
    {
        private Targetable _targetable;
        private Attacker _attacker;
        private MinionStats _stats;
        private MeshRenderer _renderer;
        private bool _ready;
        private bool _isAttacked;

        public float AttackRange() => _stats.attackRange;

        public bool IsReady() => _ready;

        void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
            _stats = GetComponentInChildren<MinionStats>();
            _targetable = GetComponentInChildren<Targetable>();
            _ready = true;
        }

        public override void OnAttack()
        {
            _renderer.material.color = Color.blue;
        }
        
        public override void OnAttacked()
        {
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
