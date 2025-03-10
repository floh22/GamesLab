using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using GameManagement;
using Network;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{

    public static LobbyUIController Instance;

    private bool isActive;
    public bool IsActive
    {
        get => isActive;

        set
        {
            if (value == isActive)
                return;
            isActive = value;
            if (changeStateRoutine != null)
                StopCoroutine(changeStateRoutine);
            changeStateRoutine = StartCoroutine(isActive ? ShowUI() : HideUI());
        }
    }

    [Header("Lobby UI Data")] 
    [SerializeField] private GameObject playerNameDisplayCanvasObject;
    [SerializeField] private TMP_Text[] playerNameDisplays;
    [SerializeField] private int[] paddingValuesPerPlayer;
    [SerializeField] private RectMask2D playerNameMask;
    [SerializeField] private Transform dividerTransform;
    
    
    
    private Coroutine playerNameDisplayRoutine;
    private Coroutine changeStateRoutine;


    public bool cameraMoveFinished = false;
    public bool updatePlayerNamesOnMoveFinish = false;


    // Start is called before the first frame update
    void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void UpdatePlayerNames()
    {
        int counter = 0;
        foreach(var player in  PhotonNetwork.CurrentRoom.Players)
        {
            GameData.Team t = (GameData.Team)player.Value.CustomProperties["Team"];
            GameObject playerObject = LauncherController.Instance.playerObjectsByTeam[t];


            Vector3 pastPos = playerObject.transform.position;
            playerObject.transform.position =
                LauncherController.Instance.playerPositions.Values.ToList()[counter];
            playerNameDisplays[counter].text = PhotonNetwork.CurrentRoom.Players.Values.ToList()[counter].NickName;


            GameObject o = LauncherController.Instance.playerObjects.FirstOrDefault(p => p != playerObject &&
                Vector3.Distance(p.transform.position, playerObject.transform.position) < 0.1f);

            if (o != null && !o.Equals(null))
            {
                o.transform.position = pastPos;
            }

            counter++;
        }
        
        if(playerNameDisplayRoutine != null)
            StopCoroutine(playerNameDisplayRoutine);
        playerNameDisplayRoutine = StartCoroutine(UpdatePlayerNameMasks());
    }

    private IEnumerator UpdatePlayerNameMasks()
    {
        float smoothTime = 0.3f;
        float smoothTimeChange = 0.0025f;
        float displayRoutineVelocity = 0;

        bool doMove = true;
        while (doMove)
        {
            int padding = paddingValuesPerPlayer[0];
            try
            {
                padding = paddingValuesPerPlayer[PhotonNetwork.CurrentRoom.PlayerCount];
            }
            catch (NullReferenceException)
            {
                //Player left lobby while animating
                playerNameMask.padding = new Vector4(0, 0, padding, 0);
                yield break;
            }

            float newPadding = Mathf.SmoothDamp(playerNameMask.padding.z,
                padding, ref displayRoutineVelocity, smoothTime);

            smoothTime -= smoothTimeChange;
                
            playerNameMask.padding = new Vector4(0, 0,  newPadding, 0);

            yield return null;
            
            doMove = Math.Abs(playerNameMask.padding.z - padding) > 0.01f;
        }
    }

    private IEnumerator ShowUI()
    {
        float smoothTime = 0.3f;
        float smoothTimeChange = 0.0025f;
        float changeVel = 1;
        while (Math.Abs(dividerTransform.localScale.x - 1) > 0.01)
        {
            float newScale = Mathf.SmoothDamp(dividerTransform.localScale.x,
                1, ref changeVel, smoothTime);

            smoothTime -= smoothTimeChange;

            dividerTransform.localScale = new Vector3(newScale, 1, 1);

            yield return null;
        }

        cameraMoveFinished = true;
        
        yield return new WaitForSeconds(0.1f);
        
        if(updatePlayerNamesOnMoveFinish)
            UpdatePlayerNames();
    }


    private IEnumerator HideUI()
    {
        playerNameMask.padding = new Vector4(0, 0, 1920, 0);
        dividerTransform.localScale = new Vector3(1.12f, 1, 1);
        cameraMoveFinished = false;
        updatePlayerNamesOnMoveFinish = false;
        yield return null; 
        
    }
    
}
