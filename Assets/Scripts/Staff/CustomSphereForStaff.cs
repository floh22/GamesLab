using Character;
using NetworkedPlayer;
using UnityEngine;
using GameUnit;

public class CustomSphereForStaff : MonoBehaviour
{
    public float ABILITY_DAMAGE = 40;
    private bool damageIsActivated = false;

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
                float totalAbilityDamage = ABILITY_DAMAGE;
            
                IGameUnit.SendDealDamageEvent(PlayerController.LocalPlayerController, targetIGameUnit, totalAbilityDamage);

                // To get the name of the ability need to get the grandparent object
                GameObject parentGO = this.gameObject.transform.parent.gameObject;
                Debug.Log($"Player {PlayerController.LocalPlayerController.gameObject.name} of team {PlayerController.LocalPlayerController.Team} hit {parentGO.name} on {gameObj.name} of team {targetIGameUnit.Team} and did {totalAbilityDamage} damage.");
            }  

            damageIsActivated = false;
        } 
    }       

    public void activateDamage()
    {
        damageIsActivated = true;
    }
}
