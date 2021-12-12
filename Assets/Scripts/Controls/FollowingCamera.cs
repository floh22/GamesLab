using UnityEngine;

namespace Scipts
{
    public class FollowingCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;
        public float smoothFactor;

        private void FixedUpdate()
        {
            Follow();
        }

        void Follow()
        {
            Vector3 targetPosition = target.position + offset;
            Vector3 smoothPosition = Vector3.Lerp(transform.position, targetPosition, smoothFactor * Time.deltaTime) ;
            transform.position = smoothPosition;
        }
    }
}