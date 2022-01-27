using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        for (int playerPos = 0; playerPos < PhotonNetwork.CurrentRoom.PlayerCount; playerPos++)
        {
            playerNameDisplays[playerPos].text = PhotonNetwork.CurrentRoom.Players.Values.ToList()[playerPos].NickName;
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
        while (Math.Abs(playerNameMask.padding.z - paddingValuesPerPlayer[PhotonNetwork.CurrentRoom.PlayerCount]) > 0.01f)
        {
            float newPadding = Mathf.SmoothDamp(playerNameMask.padding.z,
                paddingValuesPerPlayer[PhotonNetwork.CurrentRoom.PlayerCount], ref displayRoutineVelocity, smoothTime);

            smoothTime -= smoothTimeChange;
                
            playerNameMask.padding = new Vector4(0, 0,  newPadding, 0);

            yield return null;
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
        yield return new WaitForSeconds(0.1f);
        
        UpdatePlayerNames();
    }


    private IEnumerator HideUI()
    {
        playerNameMask.padding = new Vector4(0, 0, 1920, 0);
        dividerTransform.localScale = new Vector3(1.12f, 1, 1);
        yield return null; 
        
    }
    
}
