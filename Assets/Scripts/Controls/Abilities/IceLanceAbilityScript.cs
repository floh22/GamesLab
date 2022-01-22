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

        public float ABILITY_DAMAGE = 20;

        private float damageMultiplier;

        private int i = 0;

        public void setMaxRadius(float radius)
        {
            boundingSphereTransform.localScale = new Vector3(radius, radius, radius);
        }

        void OnParticleCollision(GameObject gameObj)
        {
            IGameUnit targetIGameUnit = gameObj.GetComponent<IGameUnit>();

            // Ignore units without GameUnit component
            if (targetIGameUnit != null)
            {
                damageMultiplier =
                    PlayerController.LocalPlayerController.DamageMultiplierAbility2 * (targetIGameUnit.Type.Equals(GameUnitType.Minion)
                        ? PlayerController.LocalPlayerController.DamageMultiplierMinion
                        : 1);

                float totalAbilityDamage = ABILITY_DAMAGE * damageMultiplier;
            
                targetIGameUnit.DoDamageVisual(PlayerController.LocalPlayerController, totalAbilityDamage);
            
                IGameUnit.SendDealDamageEvent(PlayerController.LocalPlayerController, targetIGameUnit, totalAbilityDamage);

                Debug.Log($"Player {PlayerController.LocalPlayerController.gameObject.name} of team {PlayerController.LocalPlayerController.Team} threw {this.gameObject.name} on {gameObj.name} of team {targetIGameUnit.Team} and did {totalAbilityDamage} damage.");
            }
        }

        public void determineNumberOfShots()
        {
            // Increase the number of ice lance abilities fired depending on skill "level"
            ParticleSystem ps = this.gameObject.GetComponent<ParticleSystem>();
            var emission = ps.emission;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);

            if(PlayerController.LocalPlayerController.UpgradesAbility2+1 > bursts[0].cycleCount)
            {
                bursts[0].cycleCount = PlayerController.LocalPlayerController.UpgradesAbility2 + 1;
                emission.SetBursts(bursts, 1);
            }
        }

        public void determineUnitsToAvoid()
        {
            // All layers except the current team layer.
            int layerMask =~ LayerMask.GetMask(PlayerController.LocalPlayerController.Team.ToString() + "Units");

            ParticleSystem ps = this.gameObject.GetComponent<ParticleSystem>();
            var collision = ps.collision;
            collision.collidesWith = layerMask;
        }

        public void ActivateDaamge()
        {
            damageCollider.enabled = true;
        }
    }
}
