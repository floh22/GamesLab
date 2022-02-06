using Character;
using NetworkedPlayer;
using UnityEngine;
using GameUnit;

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
        public float ABILITY_DAMAGE = 20;
        public AudioSource AudioSource;


        private bool damageIsActivated = false;

        /* Start of Debug stuff
        public GameObject targetCirclePrefab;
        private GameObject targetCircle;        
        private Vector3 targetPosition;   
        public GameObject targetArrowPrefab;
        private GameObject targetArrow;
        End of Debug stuff */         

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
            if (damageIsActivated)
            {
                IGameUnit targetIGameUnit = gameObj.GetComponent<IGameUnit>();

                // Ignore units without GameUnit component
                if (targetIGameUnit != null && !(targetIGameUnit is BaseBehavior))
                {
                    AudioSource.Play();

                    float damageMultiplier =
                        PlayerController.LocalPlayerController.DamageMultiplierAbility2 * (targetIGameUnit.Type.Equals(GameUnitType.Minion)
                            ? PlayerController.LocalPlayerController.DamageMultiplierMinion
                            : 1);

                    float totalAbilityDamage = ABILITY_DAMAGE * damageMultiplier;
                
                    IGameUnit.SendDealDamageEvent(PlayerController.LocalPlayerController, targetIGameUnit, totalAbilityDamage);

                    Debug.Log($"Player {PlayerController.LocalPlayerController.gameObject.name} of team {PlayerController.LocalPlayerController.Team} threw {this.gameObject.name} on {gameObj.name} of team {targetIGameUnit.Team} and did {totalAbilityDamage} damage.");
                }
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
            // This is done to make particles collide with everything except units of the same team
            int layerMask =~ LayerMask.GetMask(PlayerController.LocalPlayerController.Team.ToString() + "Units");

            ParticleSystem ps = this.gameObject.GetComponent<ParticleSystem>();
            var collision = ps.collision;
            collision.collidesWith = layerMask;
        }

        // void FixedUpdate()
        // {
        //     // This is done to move the particle system with the player without rotating it.
        //     // Useful for when multiple shots are fired while moving
        //     Vector3 forward = PlayerController.LocalPlayerController.gameObject.transform.forward;
        //     this.gameObject.transform.position = PlayerController.LocalPlayerController.gameObject.transform.position + new Vector3(forward.x * 0.05f, 2, forward.z * 0.05f);            
        // }

        public void activateDamage()
        {
            damageIsActivated = true;
        }

        /* Start of Debug stuff
        public void setTargetPosition(Vector3 targetPosition_p, Vector3 sourcePosition_p)
        {
            Vector3 sourcePosition = new Vector3(sourcePosition_p.x, 0.1f, sourcePosition_p.z);
            targetPosition = new Vector3(targetPosition_p.x, 0.1f, targetPosition_p.z);

            // Create a targetCircle at the target location
            targetCircle = Instantiate(targetCirclePrefab, targetPosition, Quaternion.Euler(90, 0, 0));
            targetArrow = Instantiate(targetArrowPrefab, sourcePosition, Quaternion.Euler(0, 0, 0));

            targetArrow.transform.LookAt(targetPosition);
            targetArrow.transform.Rotate(90.0f, 0.0f, 0.0f);
        }     
        End of Debug stuff */       
    }
}
