using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;

namespace Gamekit3D
{
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        private void Awake()
        {
            //we make sure the checkpoint is part of the Checkpoint layer, which is set to interact ONLY with the player layer.
            gameObject.layer = LayerMask.NameToLayer("Checkpoint");

            /* Start of unofficial code */

            GameObject spawnPointHolder = GameObject.Find("SpawnPoints");

            if(spawnPointHolder != null)
            {
                string currentTeam = PersistentData.Team.ToString();
                Vector3 pos = spawnPointHolder.transform.Find(PersistentData.Team.ToString()).transform.position;
                transform.position = pos + Vector3.up;
            }
            else
                Debug.Log("spawnPointHolder == null in Gamekit3D.Checkpoint.Awake()");            

            /* End of unofficial code */
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController controller = other.GetComponent<PlayerController>();

            if (controller == null)
                return;

            // Debug.Log($"Checkpoint Set, Collider = {other}");

            controller.SetCheckpoint(this);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue * 0.75f;
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawRay(transform.position, transform.forward * 2);
        }
    }
}
