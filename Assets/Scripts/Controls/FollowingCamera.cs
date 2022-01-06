using UnityEngine;

namespace Controls
{
    public class FollowingCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;

        private void LateUpdate()
        {
            Follow();
        }

        void Follow()
        {
            transform.position = target.position + offset;
        }
    }
}