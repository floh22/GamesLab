using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scipts
{
    public class JoystickMovement : MonoBehaviour
    {

        public Joystick joystick;

        // units moved per second holding down move input
        public float moveSpeed = 2;

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
                transform.Translate(Vector3.left * Math.Abs(horizontal) * moveSpeed* Time.fixedDeltaTime);
            }
        }
    }
}