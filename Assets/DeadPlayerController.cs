using System;
using System.Collections;
using System.Collections.Generic;
using NetworkedPlayer;
using UnityEngine;

public class DeadPlayerController : MonoBehaviour
{
    [SerializeField]
    private Joystick joystick;
    
    private CharacterController controller;
    
    // Start is called before the first frame update
    void Start()
    {
        joystick = GameObject.FindWithTag("Joystick").GetComponent<FixedJoystick>();
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        float h = Math.Clamp(Input.GetAxis("Horizontal") + joystick.Horizontal, -1, 1);
        float v = Math.Clamp(Input.GetAxis("Vertical") + joystick.Vertical, -1, 1);
        Vector3 move = new Vector3(h, 0, v).normalized;
        controller.Move(move * Time.deltaTime * (h*h+v*v) * 5);
    }
}
