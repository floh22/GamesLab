using System.Collections;
using Character.Stats;
using UnityEngine;
using Utils;

namespace Character
{
    public class Hero : ITargetable
    {

        private Targeter _targeter;
        private Attacker _attacker;
        private HeroStats _stats;
        private MeshRenderer _renderer;
        private bool _ready;

        public float AttackRange() => _stats.attackRange;

        public bool IsReady() => _ready;

        void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
            _stats = GetComponentInChildren<HeroStats>();
            _targeter = GetComponentInChildren<Targeter>();
            _attacker = GetComponentInChildren<Attacker>();
            _attacker.SetTargeter(_targeter);
            _ready = true;
            CheckObjects();
        }

        public override void OnAttack()
        {
            _renderer.material.color = Color.blue;
        }
        
        public override void OnAttacked()
        {
            _renderer.material.color = Color.red;
        }
        
        public override void OnRest()
        {
            _renderer.material.color = Color.grey;
        }

        private void CheckObjects()
        {
            ValidationUtils.RequireNonNull(_renderer);
            ValidationUtils.RequireNonNull(_stats);
            ValidationUtils.RequireNonNull(_targeter);
            ValidationUtils.RequireNonNull(_attacker);
        }

    }
}
