using UnityEngine;

namespace Controls
{
    public class AbilityProjectile2 : AbilityProjectile
    {
        void FixedUpdate()
        {
            if (_alive)
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, Time.deltaTime * 6);
                if (Vector3.Distance(transform.position, _targetPosition) < 1f)
                { 
                    TerminateParticle();
                }
            }
        }

        public void TerminateParticle()
        {
            abilityPrefab.SetActive(true);
            Instantiate(abilityPrefab, _targetPosition, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}