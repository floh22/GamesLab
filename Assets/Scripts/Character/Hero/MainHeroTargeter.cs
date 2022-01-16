using UnityEngine;

namespace Character.Hero
{
    public class MainHeroTargeter : MonoBehaviour
    {

        // TODO You should be able change the target by clicking on another hero
        public IGameUnit firstPriorityTarget;
        public IGameUnit target;
        public float targetRange;
        private BoxCollider _boxCollider;
        private IGameUnit _self;

        public void SetTargetRange(float targetRange)
        {
            _self = GetComponentInParent<IGameUnit>();
            this.targetRange = targetRange;
            _boxCollider.size = new Vector3(targetRange, 1, targetRange);
        }
        
        public bool HasTarget()
        {
            return target != null;
        }

        public IGameUnit GetTarget()
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
            var targetable = other.GetComponent<IGameUnit>();
            if (targetable == null)
            {
                return;
            }

            if (targetable == _self)
            {
                return;
            }

            if (target != null)
            {
                return;
            }

            target = targetable;
            Debug.Log("OnTriggerEnter: " + targetable.Type);
        }

        private void OnTriggerExit(Collider other)
        {
            var targetable = other.GetComponent<IGameUnit>();
            if (targetable == null)
            {
                return;
            }
            
            if (targetable == _self)
            {
                return;
            }

            if (targetable == target)
            {
                target = null;
            }

            Debug.Log("OnTriggerExit: " + targetable.Type);
        }
    }
}