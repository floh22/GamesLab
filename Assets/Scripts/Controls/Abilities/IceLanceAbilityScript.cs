using Character;
using NetworkedPlayer;
using UnityEngine;

namespace Controls.Abilities
{
    public class IceLanceAbilityScript : MonoBehaviour
    {
        // This sphere is the bound to where the projectiles can go.
        // This is coupled with the trigger in the ParticleSystem
        // with a sphere in one of the slots and the Exit (from sphere)
        // option set to Kill (the particle).
        // This sphere should also have a SphereCollider component.
        public Transform boundingSphereTransform;

        [SerializeField] private Collider damageCollider;
        // This function is used to scale the bounding sphere to a max radius
        // that projectiles can go to before getting destroyed.
        // Keep in mind that the ParticleSystem automatically destroys the
        // projectiles for us using the sphere.
        public void setMaxRadius(float radius)
        {
            boundingSphereTransform.localScale = new Vector3(radius, radius, radius);
        }

        void OnParticleCollision(GameObject gameObj)
        {
            IGameUnit targetIGameUnit = gameObj.GetComponent<IGameUnit>();

            //Ignore units without GameUnit component
            if (targetIGameUnit != null)
            {
                if(PlayerController.LocalPlayerController.Team != targetIGameUnit.Team)
                {
                    float damageMultiplier =
                        PlayerController.LocalPlayerController.DamageMultiplierAbility2 * (targetIGameUnit.Type.Equals(GameUnitType.Minion)
                            ? PlayerController.LocalPlayerController.DamageMultiplierMinion
                            : 1);

                    float abilityDamage = 20 * damageMultiplier;
                
                    targetIGameUnit.DoDamageVisual(PlayerController.LocalPlayerController, abilityDamage);
                
                    IGameUnit.SendDealDamageEvent(PlayerController.LocalPlayerController, targetIGameUnit, abilityDamage);

                    Debug.Log($"Player {PlayerController.LocalPlayerController.gameObject.name} of team {PlayerController.LocalPlayerController.Team} threw {this.gameObject.name} on {gameObj.name} of team {targetIGameUnit.Team}.");
                }
            }
        }

        public void ActivateDaamge()
        {
            damageCollider.enabled = true;
        }
    }
}
