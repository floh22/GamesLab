using Character;
using NetworkedPlayer;
using UnityEngine;

namespace Controls.Abilities
{
    public class IceLanceAbilityScript : MonoBehaviour
    {
        // This sphere is the bound to where the projectiles can go.
        // This is coupled with the a trigger in the ParticleSystem
        // with a sphere in one of the slots and the Exit (from sphere)
        // option set to Kill (the particle).
        // This sphere should also have a SphereCollider component.
        public Transform boundingSphereTransform;

        [SerializeField] private Collider damageCollider;
        // This function is used to scale the bounding sphere to a max radius
        // that there projectiles can go to before getting destroyed.
        // Keep in mind that the ParticleSystem automatically destroys the
        // projectiles for us using the sphere.
        public void setMaxRadius(float radius)
        {
            boundingSphereTransform.localScale = new Vector3(radius, radius, radius);
        }

        void OnParticleCollision(GameObject gameObj)
        {

            IGameUnit unit = gameObj.GetComponent<IGameUnit>();
            IGameUnit castFrom = PlayerController.LocalPlayerController;

            //Ignore units without GameUnit component
            if (unit == null || unit.Equals(null) || castFrom.Team == unit.Team)
            {
                return;
            }

            float damageMultiplier =
                PlayerController.LocalPlayerController.DamageMultiplierAbility2 * (unit.Type.Equals(GameUnitType.Minion)
                    ? PlayerController.LocalPlayerController.DamageMultiplierMinion
                    : 1);

            float abilityDamage = 20 * damageMultiplier;
        
            unit.DoDamageVisual(castFrom, abilityDamage);
        
            IGameUnit.SendDealDamageEvent(castFrom, unit, abilityDamage);

            return;
        
        
            if (gameObj.name.StartsWith("Minion"))
            {
                PlayerController currentPlayer = PlayerController.LocalPlayerController;
                GameUnit.Minion minionScript = gameObj.GetComponent<GameUnit.Minion>();    

                Debug.Log($"Player {currentPlayer.gameObject.name} of team {currentPlayer.Team} threw {this.gameObject.name} on {gameObj.name} of team {minionScript.Team}.");

                minionScript.DoDamageVisual(currentPlayer, 999);
            }

            if (gameObj.name.StartsWith("Temp Character"))
            {
                PlayerController currentPlayer = PlayerController.LocalPlayerController;
                PlayerController targetPlayerScript = gameObj.GetComponent<PlayerController>();    

                Debug.Log($"Player {currentPlayer.gameObject.name} of team {currentPlayer.Team} threw {this.gameObject.name} on {gameObj.name} of team {targetPlayerScript.Team}.");

                targetPlayerScript.DoDamageVisual(currentPlayer, 999);
            }

            // if (gameObj.name.StartsWith("Slender"))
            // {
            //     PlayerController currentPlayer = PlayerController.LocalPlayerController;

            //     if (gameObj.transform.Find("InnerChannelingParticleSystem").gameObject.activeSelf == false)
            //     {
            //         Debug.Log($"Player {currentPlayer.gameObject.name} of team {currentPlayer.Team} threw {this.gameObject.name} on {gameObj.name}.");
            //         gameObj.transform.Find("InnerChannelingParticleSystem").gameObject.SetActive(true);
            //     }
            // }
        }

        public void ActivateDaamge()
        {
            damageCollider.enabled = true;
        }
    }
}
