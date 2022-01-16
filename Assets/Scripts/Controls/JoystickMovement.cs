using System;
using Character;
using Character.MainHero;
using Photon.Pun;
using UnityEngine;

namespace Controls
{
    public class JoystickMovement : MonoBehaviourPun
    {

        private IGameUnit hero;
        public Joystick joystick;

        // units moved per second holding down move input
        public float moveSpeed = MainHeroValues.MoveSpeed;

        private void Start()
        {
            joystick = GameObject.FindWithTag("Joystick").GetComponent<FixedJoystick>();
            hero = gameObject.GetComponent<IGameUnit>();
            moveSpeed = hero.MoveSpeed;
        }

        private void Update()
        {
            // Prevent control is connected to Photon and represent the localPlayer
            if(photonView.IsMine == false && PhotonNetwork.IsConnected )
            {
                return;
            }
            
            Move(
                joystick.Vertical,
                joystick.Horizontal
            );
        }

        private void Move(float vertical, float horizontal)
        {
            if (vertical < 0)
            {
                transform.Translate(Vector3.back * Math.Abs(vertical) * moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.Translate(Vector3.forward * Math.Abs(vertical) * moveSpeed * Time.deltaTime);
            }

            if (horizontal > 0)
            {
                transform.Translate(Vector3.right * Math.Abs(horizontal) * moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.Translate(Vector3.left * Math.Abs(horizontal) * moveSpeed* Time.deltaTime);
            }
        }
    }
}