using System;
using Photon.Pun;
using UnityEngine;

namespace NetworkedPlayer
{
	public class PlayerAnimatorController : MonoBehaviourPun
	{
		#region Private Fields
		
		[SerializeField]
		private Joystick joystick;
    
		[SerializeField]
		private float directionDampTime = 0.25f;
		Animator animator;
		private static readonly int Jump = Animator.StringToHash("Jump");
		private static readonly int Direction = Animator.StringToHash("Direction");
		private static readonly int Speed = Animator.StringToHash("Speed");

		#endregion
    
		#region MonoBehaviour CallBacks
    
		/// <summary>
		/// MonoBehaviour method called on GameObject by Unity during initialization phase.
		/// </summary>
		private void Start () 
		{
			animator = GetComponent<Animator>();
			joystick = GameObject.FindWithTag("Joystick").GetComponent<FixedJoystick>();
		}
    	        
		/// <summary>
		/// MonoBehaviour method called on GameObject by Unity on every frame.
		/// </summary>
		private void Update () 
		{
    
			// Prevent control is connected to Photon and represent the localPlayer
			if( photonView.IsMine == false && PhotonNetwork.IsConnected )
			{
				return;
			}
    
			// failSafe is missing Animator component on GameObject
			if (!animator)
			{
				return;
			}
    
			// deal with Jumping
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);			
    
			// only allow jumping if we are running.
			if (stateInfo.IsName("Base Layer.Run"))
			{
				// When using trigger parameter
				if (Input.GetButtonDown("Fire2")) animator.SetTrigger(Jump); 
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
			animator.SetFloat( Speed, h*h+v*v );
			animator.SetFloat( Direction, h, directionDampTime, Time.deltaTime );
			
		}
    
		#endregion
	}
}
