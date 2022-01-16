using System.Collections;
using UnityEngine;

namespace Character.Hero
{
    public abstract class IHeroAttacker : MonoBehaviour
    {
        private MainHeroTargeter _mainHeroTargeter;
        private IHero _hero;
        private bool _isAttacking;
        private Targetable _self;

        void Start()
        {
            _hero = GetComponentInParent<IHero>();
            _self = GetComponentInParent<Targetable>();
            _mainHeroTargeter = GetComponentInChildren<MainHeroTargeter>();
        }

        void Update()
        {
            /*if (!_hero.IsReady())
            {
                return;
            }

            if (!_mainHeroTargeter.HasTarget() || _isAttacking)
            {
                return;
            }

            if (_mainHeroTargeter.DistanceToTarget(transform.position) > _hero.AttackRange())
            {
                return;
            }

            var target = _mainHeroTargeter.GetTarget();
            switch (target.type)
            {
                case Targetable.EnemyType.Hero:
                    StartCoroutine(Attack(target));
                    break;
                case Targetable.EnemyType.Minion:
                    StartCoroutine(Attack(target));
                    break;
            }*/
        }

        private IEnumerator Attack(Targetable targetable)
        {
            _isAttacking = true;
            _hero.OnAttack();
            targetable.OnAttacked(_hero.Damage());
            // 3. wait amount of attack speed * amount of seconds
            yield return new WaitForSeconds(0.5f);
            _hero.OnRest();
            yield return new WaitForSeconds(0.5f); // TODO define with attack speed
            // 4. be ready for another attack
            _isAttacking = false;
        }
    }
}