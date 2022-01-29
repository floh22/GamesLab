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
    }


    void Update()
    {
        // Prevent control is connected to Photon and represent the localPlayer
        if (photonView.IsMine == false && PhotonNetwork.IsConnected)
        {
            return;
        }       

        m_Movement.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
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
        Debug.Log($"photonView.IsMine = {photonView.IsMine}, PhotonNetwork.IsConnected = {PhotonNetwork.IsConnected}");

        String characterTeam = gameObject.GetComponent<NetworkedPlayer.PlayerController>().Team.ToString();
        String localPlayerTeam = NetworkedPlayer.PlayerController.LocalPlayerController.Team.ToString();  
              
        Debug.Log($"characterTeam = {characterTeam}, localPlayerTeam = {localPlayerTeam}");

        if (photonView.IsMine == false && PhotonNetwork.IsConnected)
        {
            return;
        }    

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