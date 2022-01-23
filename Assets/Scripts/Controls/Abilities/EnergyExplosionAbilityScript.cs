using Character;
using NetworkedPlayer;
using UnityEngine;

namespace Controls.Abilities
{
    public class EnergyExplosionAbilityScript : MonoBehaviour
    {
        public GameObject targetCirclePrefab;
        private GameObject targetCircle;
        public GameObject explosion;
        private Vector3 targetPosition;
        float animationProgress = 0;

        void FixedUpdate()
        {    
            // This is done to move the explosion to the target location
            if (Vector3.Distance(this.gameObject.transform.position, targetPosition) < 0.5f && explosion != null)
            {
                explosion.SetActive(true);
                // Destroy the target circle when explosion starts
                Destroy(targetCircle);
            }
            else
            {
                this.gameObject.transform.position = MathParabola.Parabola(this.gameObject.transform.position, targetPosition, 0.3f, animationProgress);
                animationProgress += Time.deltaTime;
            }
        }

        public void setTargetPosition(Vector3 targetPosition_p)
        {
            targetPosition = targetPosition_p;
            // Create a targetCircle at the target location
            targetCircle = Instantiate(targetCirclePrefab, targetPosition, Quaternion.Euler(90, 0, 0));
        }
    }
}
