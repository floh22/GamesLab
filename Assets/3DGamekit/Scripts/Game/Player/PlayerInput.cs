using UnityEngine;
using System;
using System.Collections;
using Gamekit3D;
using Photon.Pun;
using NetworkedPlayer;
public class PlayerInput : MonoBehaviourPunCallbacks
{
    public static PlayerInput Instance
    {
        get { return s_Instance; }
    }

    protected static PlayerInput s_Instance;

    [HideInInspector]
    public bool playerControllerInputBlocked;

    protected Vector2 m_Movement;
    protected Vector2 m_Camera;
    protected bool m_Jump;
    protected bool m_Attack;
    protected bool m_Pause;
    protected bool m_ExternalInputBlocked;

    private Joystick joystick;

    public Vector2 MoveInput
    {
        get
        {
            if(playerControllerInputBlocked || m_ExternalInputBlocked)
                return Vector2.zero;
            return m_Movement;
        }
    }

    public Vector2 CameraInput
    {
        get
        {
            if(playerControllerInputBlocked || m_ExternalInputBlocked)
                return Vector2.zero;
            return m_Camera;
        }
    }

    public bool JumpInput
    {
        get { return m_Jump && !playerControllerInputBlocked && !m_ExternalInputBlocked; }
    }

    public bool Attack
    {
        get { return m_Attack && !playerControllerInputBlocked && !m_ExternalInputBlocked; }
    }

    public bool Pause
    {
        get { return m_Pause; }
    }

    WaitForSeconds m_AttackInputWait;
    Coroutine m_AttackWaitCoroutine;

    const float k_AttackInputDuration = 0.03f;

    void Awake()
    {
        // Debug.Log($"LocalPlayerController.Team = {NetworkedPlayer.PlayerController.LocalPlayerController.Team}");

        /*
            photonView.IsMine will be true if the instance is controlled by the 'client' application, meaning this 
            instance represents the physical person playing on this computer within this application. So if it is 
            false, we don't want to do anything and solely rely on the PhotonView component to synchronize the 
            transform and animator components we've setup earlier.

            But, why having then to enforce PhotonNetwork.IsConnected == true in our if statement? eh eh :) because 
            during development, we may want to test this prefab without being connected. In a dummy scene for 
            example, just to create and validate code that is not related to networking features per se. And so 
            with this additional expression, we will allow input to be used if we are not connected. It's a very 
            simple trick and will greatly improve your workflow during development.        
        */
        if (photonView.IsMine == false && PhotonNetwork.IsConnected)
        {
            return;
        }        

        // String characterTeam = gameObject.GetComponent<NetworkedPlayer.PlayerController>().Team.ToString();
        // String localPlayerTeam = NetworkedPlayer.PlayerController.LocalPlayerController.Team.ToString();

        

        // // If the character instance'S team is different from the local player team don't proceed.
        // if (gameObject.GetComponent<NetworkedPlayer.PlayerController>().Team != NetworkedPlayer.PlayerController.LocalPlayerController.Team)
        // {
        //     return;
        // }        

        m_AttackInputWait = new WaitForSeconds(k_AttackInputDuration);

        if (s_Instance == null)
            s_Instance = this;
        else if (s_Instance != this)
            throw new UnityException("There cannot be more than one PlayerInput script.  The instances are " + s_Instance.name + " and " + name + ".");

        joystick = GameObject.FindWithTag("Joystick").GetComponent<FixedJoystick>(); // Unofficial code
    }


    void Update()
    {
        /* Start of unofficial code */

        // Prevent control is connected to Photon and represent the localPlayer
        if (photonView.IsMine == false && PhotonNetwork.IsConnected)
        {
            return;
        }       

        float h = Math.Clamp(Input.GetAxis("Horizontal") + joystick.Horizontal, -1, 1);
        float v = Math.Clamp(Input.GetAxis("Vertical") + joystick.Vertical, -1, 1); 

        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0
            || joystick.Horizontal != 0 || joystick.Vertical != 0)
        {
            NetworkedPlayer.PlayerController.LocalPlayerController.InterruptChanneling();
        }        
        
        /* End of unofficial code */       

        m_Movement.Set(h, v);
        // m_Camera.Set(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        m_Jump = Input.GetButton("Jump");

        // if (Input.GetButtonDown("Fire1"))
        // {
        //     if (m_AttackWaitCoroutine != null)
        //         StopCoroutine(m_AttackWaitCoroutine);

        //     m_AttackWaitCoroutine = StartCoroutine(AttackWait());
        // }

        // m_Pause = Input.GetButtonDown ("Pause");
    }

    IEnumerator AttackWait()
    {
        m_Attack = true;

        yield return m_AttackInputWait;

        m_Attack = false;
    }

    public bool HaveControl()
    {
        return !m_ExternalInputBlocked;
    }

    public void ReleaseControl()
    {
        m_ExternalInputBlocked = true;
    }

    public void GainControl()
    {
        m_ExternalInputBlocked = false;
    }

    /* Start of non-official code */

    public void DoAttack()
    {
        if (m_AttackWaitCoroutine != null)
            StopCoroutine(m_AttackWaitCoroutine);

        m_AttackWaitCoroutine = StartCoroutine(AttackWait());
    }

    public void DoMove(float x, float y)
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected)
        {
            return;
        }    

        m_Movement.Set(x, y);
    }    

    /* End of non-official code */
}