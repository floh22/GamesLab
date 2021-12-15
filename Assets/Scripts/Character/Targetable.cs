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

    }
}
