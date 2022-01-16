using UnityEngine;

namespace CharacterV2
{
    public abstract class ITargetable : MonoBehaviour
    {
        public abstract void OnAttack();
        public abstract void OnAttacked(float dmg);
        public abstract void OnRest();
    }
}