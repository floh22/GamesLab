using Character.Stats;
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
        public IStats stats;

        void Start()
        {
            stats = GetComponent<IStats>();
        }

    }
}
