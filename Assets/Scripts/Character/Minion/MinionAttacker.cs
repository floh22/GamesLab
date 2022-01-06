using System.Collections;
using UnityEngine;

namespace Character.Minion
{
    public class MinionAttacker : MonoBehaviour
    {
        private Targeter _targeter;
        private Character.Minion.Minion _minion;
        private bool _isAttacking;
        private Targetable _self;

        void Start()
        {
            _minion = GetComponentInParent<Character.Minion.Minion>();
            _self = GetComponentInParent<Targetable>();
            _targeter = GetComponentInChildren<Targeter>();
        }

        void Update()
        {
            if (!_minion.IsReady())
            {
                return;
            }

            if (!_targeter.HasTarget() || _isAttacking)
            {
                return;
            }

            if (_targeter.DistanceToTarget(transform.position) > _minion.AttackRange())
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

        private IEnumerator Attack(Targetable targetable)
        {
            _isAttacking = true;
            _minion.OnAttack();
            targetable.OnAttacked(_minion.Damage());
            // 3. wait amount of attack speed * amount of seconds
            yield return new WaitForSeconds(0.5f);
            _minion.OnRest();
            yield return new WaitForSeconds(0.5f); // TODO define with attack speed
            // 4. be ready for another attack
            _isAttacking = false;
        }
    }
}