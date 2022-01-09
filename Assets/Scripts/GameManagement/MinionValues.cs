using UnityEngine;
using UnityEngine.Serialization;

namespace GameManagement
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MinionManager", order = 1)]
    public class MinionValues : ScriptableObject
    {
        [Header("Wave Data")]
        public int WaveSize;
        public int WaveDelayInMs;
        public int MinionOffsetInMs;

        [Space]
        [Header("Unit Stats")] 
        public float UpdateRateInS;
        public float MinionHealth;
        public float MinionAttackSpeed;
        public float MinionAttackDamage;
        public float MinionMoveSpeed;
        public float MinionAgroRadius;
        public float MinionLeashRadius;
        public float MinionAttackRange;
    }
}
