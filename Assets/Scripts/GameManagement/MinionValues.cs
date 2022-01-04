using UnityEngine;
using UnityEngine.Serialization;

namespace GameManagement
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MinionManager", order = 1)]
    public class MinionValues : ScriptableObject
    {
        public int WaveSize;
        public float WaveDelayInMs;
        public float MinionOffsetInMs;
        
        
        public float MinionHealth;
        [FormerlySerializedAs("MinionAttacKSpeed")] public float MinionAttackSpeed;
        public float MinionAttackDamage;
        public float MinionMoveSpeed;
        public float MinionAgroRadius;
        public float MinionLeashRadius;
        public float MinionAttackRange;
    }
}
