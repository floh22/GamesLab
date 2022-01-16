using System;
using CharacterV2;
using CharacterV2.MainHero;
using UnityEngine;

namespace Scipts
{
    public class KeyboardMovement : MonoBehaviour
    {
        private const string VerticalAxis = "Vertical";
        private const string HorizontalAxis = "Horizontal";

        // units moved per second holding down move input
        public float moveSpeed = MainHeroValues.MoveSpeed;

        // Update is called once per frame
        private void Update()
        {
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