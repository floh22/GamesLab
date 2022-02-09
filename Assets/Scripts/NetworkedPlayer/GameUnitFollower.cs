using Unity.VisualScripting;
using UnityEngine;

namespace NetworkedPlayer
{
    public class GameUnitFollower : MonoBehaviour
    {
        private Transform toFollow;

        private bool isFollowing;
        // Start is called before the first frame update
        void Start()
        {
        
        }


        public void StartFollowing(Transform t)
        {
            isFollowing = true;
            toFollow = t;
        }
        
        void LateUpdate()
        {
            if (!isFollowing)
                return;

            if (toFollow.IsDestroyed())
            {
                isFollowing = false;
                return;
            }
            gameObject.transform.position = toFollow.position;
        }
    }
}
