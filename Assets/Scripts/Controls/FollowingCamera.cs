using UnityEngine;

namespace Scipts
{
    public class FollowingCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;

        private void Update()
        {
            Follow();
        }

        void Follow()
        {
            transform.position = target.position + offset;
        }
    }
}