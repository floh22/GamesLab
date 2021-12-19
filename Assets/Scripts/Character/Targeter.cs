using Scipts;
using UnityEngine;

namespace Character
{
    public class Targeter : MonoBehaviour
    {

        // TODO You should be able change the target by clicking on another hero
        public Targetable target;
        public float targetRange;
        private BoxCollider _boxCollider;

        public void SetTargetRange(float targetRange)
        {
            this.targetRange = targetRange;
            _boxCollider.size = new Vector3(targetRange, 1, targetRange);
        }

        public bool HasTarget()
        {
            return target != null;
        }

        public Targetable GetTarget()
        {
            return target;
        }

        public float DistanceToTarget(Vector3 position)
        {
            return Vector3.Distance(transform.position, position);
        }

        public void Start()
        {
            _boxCollider = GetComponent<BoxCollider>();
            SetTargetRange(targetRange);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var targetable = other.GetComponent<Targetable>();
            if (targetable == null)
            {
                return;
            }

            if (target != null)
            {
                return;
            }

            target = targetable;
            Debug.Log("OnTriggerEnter: " + targetable.type);
        }

        private void OnTriggerExit(Collider other)
        {
            var targetable = other.GetComponent<Targetable>();
            if (targetable == null)
            {
                return;
            }

            if (targetable == target)
            {
                target = null;
            }

            Debug.Log("OnTriggerExit: " + targetable.type);
        }
    }
}