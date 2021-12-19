using System.Collections;
using Character.Stats;
using Scipts;
using UnityEngine;

namespace Character
{
    public class Attacker : MonoBehaviour
    {
        private Targeter _targeter;
        private HeroStats _stats;
        private bool _isAttacking;

        void Start()
        {
            _targeter = GetComponent<Targeter>();
            _stats = GetComponent<HeroStats>();
        }

        void Update()
        {
            if (!_isAttacking && _targeter != null && _targeter.HasTarget())
            {
                var target = _targeter.GetTarget();
                var distance = Vector3.Distance(target.transform.position, this.transform.position);
                if (distance <= _stats.attackRange)
                {
                    _isAttacking = true;
                    // TODO start coroutine to attack,
                    // TODO set false by the end of coroutine duration depends on attack speed 
                    switch (target.type)
                    {
                        case Targetable.EnemyType.Hero:
                            StartCoroutine(AttackHero(target));
                            break;
                        case Targetable.EnemyType.Minion:
                            StartCoroutine(AttackMinion(target));
                            break;
                    }
                }
            }
        }

        private IEnumerator AttackHero(Targetable target)
        {
            // TODO 1. animate an attack
            // 2. take hp from target
            // 3. wait amount of attack speed * amount of seconds
            // 4. be ready for another attack
            _isAttacking = true;
            yield return new WaitForSeconds(1);
            _isAttacking = false;
        }

        private IEnumerator AttackMinion(Targetable target)
        {
            // TODO 1. animate an attack
            // 2. take hp from target
            // 3. wait amount of attack speed * amount of seconds
            // 4. be ready for another attack
            _isAttacking = true;
            yield return new WaitForSeconds(1);
            _isAttacking = false;
        }
    }
}