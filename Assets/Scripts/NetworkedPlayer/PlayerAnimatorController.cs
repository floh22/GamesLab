using System;
using Photon.Pun;
using UnityEngine;

namespace NetworkedPlayer
{
    public class PlayerAnimatorController : MonoBehaviourPun
    {
        #region Private Fields

        [SerializeField] private Joystick joystick;

        private PlayerController player;

        Animator animator;
        private static readonly int Speed = Animator.StringToHash("ForwardSpeed");

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        private void Start()
        {
            animator = GetComponent<Animator>();
            joystick = GameObject.FindWithTag("Joystick").GetComponent<FixedJoystick>();
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// </summary>
        private void Update()
        {
            // Prevent control is connected to Photon and represent the localPlayer
            if (photonView.IsMine == false && PhotonNetwork.IsConnected)
            {
                return;
            }

            // failSafe is missing Animator component on GameObject
            if (!animator)
            {
                return;
            }

            if (player == null || player.Equals(null))
            {
                player = PlayerController.LocalPlayerController;
            }

            if (player == null)
                return;

            if (!player.IsAlive)
            {
                transform.position = new Vector3(0, -10, 0);
                return;
            }

            // deal with movement
            float h = Math.Clamp(Input.GetAxis("Horizontal") + joystick.Horizontal, -1, 1);
            float v = Math.Clamp(Input.GetAxis("Vertical") + joystick.Vertical, -1, 1);

            // prevent negative Speed.

            /*
            if( v < 0 )
            {
                v = 0;
            }
            */

            // set the Animator Parameters
            animator.SetFloat(Speed, h * h + v * v);
            //animator.SetFloat( Direction, h, directionDampTime, Time.deltaTime * 10);

            if (h == 0 && v == 0)
            {
                return;
            }

            if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0
                || joystick.Horizontal != 0 || joystick.Vertical != 0)
            {
                player.InterruptChanneling();
            }

            float heading = Mathf.Atan2(h, v);
            transform.rotation = Quaternion.Euler(0, heading * Mathf.Rad2Deg, 0);

            /* Start of Ellen's  Attack Animation stuff */

            PlayerInput ellenPlayerInput = this.gameObject.GetComponent<PlayerInput>();
            ellenPlayerInput.DoMove(h, v);

            /* End of Ellen's Attack Animation stuff */
        }

        #endregion
    }
}