using System;
using Character;
using Character.MainHero;
using Photon.Pun;
using UnityEngine;

namespace Controls
{
    public class KeyboardMovement : MonoBehaviourPun
    {
        private const string VerticalAxis = "Vertical";
        private const string HorizontalAxis = "Horizontal";

        private IGameUnit hero;
        // units moved per second holding down move input
        public float moveSpeed = MainHeroValues.MoveSpeed;
        
        private void Start()
        {
            hero = gameObject.GetComponent<IGameUnit>();
            moveSpeed = hero.MoveSpeed;
        }

        // Update is called once per frame
        private void Update()
        {
            // Prevent control is connected to Photon and represent the localPlayer
            if(photonView.IsMine == false && PhotonNetwork.IsConnected )
            {
                return;
            }

            Move(
                Input.GetAxis(VerticalAxis),
                Input.GetAxis(HorizontalAxis)
            );
        }

        private void Move(float vertical, float horizontal)
        {
            if (vertical < 0)
            {
                transform.Translate(Vector3.back * Math.Abs(vertical) * moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                transform.Translate(Vector3.forward * Math.Abs(vertical) * moveSpeed * Time.fixedDeltaTime);
            }

            if (horizontal > 0)
            {
                transform.Translate(Vector3.right * Math.Abs(horizontal) * moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                transform.Translate(Vector3.left * Math.Abs(horizontal) * moveSpeed * Time.fixedDeltaTime);
            }
        }
    }
}