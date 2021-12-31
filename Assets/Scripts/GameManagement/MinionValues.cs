using UnityEngine;

namespace GameManagement
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MinionManager", order = 1)]
    public class MinionValues : ScriptableObject
    {
        public int WaveSize;
        public float WaveDelayInMs;
        public float MinionOffsetInMs;
        
        
        public float MinionHealth;
        public float MinionAttacKSpeed;
        public float MinionAttackDamage;
        public float MinionMoveSpeed;
        public float MinionAgroRadius;
        public float MinionLeashRadius;
    }
}
