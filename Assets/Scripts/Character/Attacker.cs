using Character;
using UnityEngine;

namespace Scipts
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
            if (_targeter != null && _targeter.HasTarget())
            {
                var target = _targeter.GetTarget();
                var distance = Vector3.Distance(target.transform.position, this.transform.position);
                if (distance <= _stats.attackRange)
                {
                    _isAttacking = true;
                    // TODO start coroutine to attack, set false by the end of coroutine duration depends on attack speed 
                }
            }
        }
        
        
    }
}
