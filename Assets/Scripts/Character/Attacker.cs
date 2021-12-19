using System.Collections;
using UnityEngine;

namespace Character
{
    public class Attacker : MonoBehaviour
    {
        private Targeter _targeter;
        private Hero _hero;
        private bool _isAttacking;

        void Start()
        {
            _hero = GetComponentInParent<Hero>();
        }

        void Update()
        {
            if (!_hero.IsReady())
            {
                return;
            }

            if (!_targeter.HasTarget() || _isAttacking)
            {
                return;
            }

            if (_targeter.DistanceToTarget(transform.position) > _hero.AttackRange())
            {
                return;
            }

            var target = _targeter.GetTarget();
            switch (target.type)
            {
                case Targetable.EnemyType.Hero:
                    StartCoroutine(Attack(target));
                    break;
                case Targetable.EnemyType.Minion:
                    StartCoroutine(Attack(target));
                    break;
            }
        }

        public void SetTargeter(Targeter targeter)
        {
            _targeter = targeter;
        }

        private IEnumerator Attack(Targetable targetable)
        {
            _isAttacking = true;
            // TODO 1. animate an attack
            _hero.OnAttack();
            // 2. take hp from target
            // 3. wait amount of attack speed * amount of seconds
            // 4. be ready for another attack
            _isAttacking = true;
            targetable.OnAttacked();
            yield return new WaitForSeconds(0.5f);
            _hero.OnRest();
            // pause
            yield return new WaitForSeconds(0.5f); // TODO define with attack speed
            _isAttacking = false;
        }
    }
}