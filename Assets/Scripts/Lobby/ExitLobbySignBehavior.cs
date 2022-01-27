using Lobby;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ExitLobbySignBehavior : MonoBehaviour
{

    [SerializeField] private LauncherController launcherController;
    [SerializeField] private LobbyCameraController lobbyCameraController;
    [SerializeField] private Camera lobbyCamera;
    [SerializeField] private Vector3 backupPosition;
    
    
    [Header("Exit Sign")] 
    [SerializeField] private Light signLight;
    [SerializeField] private TMP_Text signText;
    [SerializeField] private Color disabledColor;
    [SerializeField] private Color enabledColor;

    private void Start()
    {
        Vector3 pos = transform.position + Vector3.left * 2; //Left most part of sign
        Vector3 inCameraPos = lobbyCamera.WorldToViewportPoint(pos);
        
        Debug.Log(inCameraPos);

        if (inCameraPos.x < 0)
        {
            
            //transform.position = backupPosition;
        }

        lobbyCamera.enabled = false;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0) || Camera.main == null || lobbyCameraController.isMoving) return;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out var hit, 100, LayerMask.GetMask("GameObject"))) return;
        
        //just reuse the respawn gameTag, no need for extra tags
        if(hit.collider.CompareTag("Respawn")) {
            launcherController.Leave();                       
        }
    }


    public void Enable()
    {
        signLight.enabled = true;
        signText.color = enabledColor;
    }

    public void Disable()
    {
        signLight.enabled = false;
        signText.color = disabledColor;
    }
}
