using UnityEngine;

namespace Character
{
    public class Targetable : MonoBehaviour
    {

        public enum EnemyType
        {
            Minion, Hero
        }

        public EnemyType type;
        private ITargetable _targetable;

        void Start()
        {
            _targetable = GetComponent<ITargetable>();
        }

        public void OnAttacked(float dmg)
        {
            _targetable.OnAttacked(dmg);
        }

    }
}
