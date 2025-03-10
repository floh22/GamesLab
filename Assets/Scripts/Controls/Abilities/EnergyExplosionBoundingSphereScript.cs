using Character;
using NetworkedPlayer;
using UnityEngine;
using GameUnit;

namespace Controls.Abilities
{
    public class EnergyExplosionBoundingSphereScript : MonoBehaviour
    {
        public float ABILITY_DAMAGE = 20;
        private bool damageIsActivated = false;

        //If your GameObject starts to collide with another GameObject with a Collider
        void OnTriggerEnter(Collider collider)
        {
            if(damageIsActivated)
            {
                GameObject gameObj = collider.gameObject;
                IGameUnit targetIGameUnit = gameObj.GetComponent<IGameUnit>();

                // Ignore units without GameUnit component
                // and of the same team
                if (targetIGameUnit != null && targetIGameUnit.Team != PlayerController.LocalPlayerController.Team && !(targetIGameUnit is BaseBehavior))
                {
                    float damageMultiplier =
                        PlayerController.LocalPlayerController.DamageMultiplierAbility2 * (targetIGameUnit.Type.Equals(GameUnitType.Minion)
                            ? PlayerController.LocalPlayerController.DamageMultiplierMinion
                            : 1);

                    float totalAbilityDamage = ABILITY_DAMAGE * damageMultiplier;
                
                    IGameUnit.SendDealDamageEvent(PlayerController.LocalPlayerController, targetIGameUnit, totalAbilityDamage);

                    // To get the name of the ability need to get the grandparent object
                    GameObject grandParentGO = this.gameObject.transform.parent.gameObject.transform.parent.gameObject;
                    Debug.Log($"Player {PlayerController.LocalPlayerController.gameObject.name} of team {PlayerController.LocalPlayerController.Team} threw {grandParentGO.name} on {gameObj.name} of team {targetIGameUnit.Team} and did {totalAbilityDamage} damage.");
                }  
            } 
        }   

        public void activateDamage()
        {
            damageIsActivated = true;
        }
    }
}
