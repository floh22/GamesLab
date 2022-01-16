using System;
using Character.MainHero;
using UnityEngine;

namespace Controls
{
    public class JoystickMovement : MonoBehaviour
    {

        public Joystick joystick;

        // units moved per second holding down move input
        public float moveSpeed = MainHeroValues.MoveSpeed;

        // Update is called once per frame
        private void Update()
        {
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