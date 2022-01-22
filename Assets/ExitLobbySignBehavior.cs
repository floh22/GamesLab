using System.Collections;
using System.Collections.Generic;
using Lobby;
using Network;
using UnityEngine;

public class ExitLobbySignBehavior : MonoBehaviour
{

    [SerializeField] private LauncherController launcherController;
    [SerializeField] private LobbyCameraController lobbyCameraController;

    // Update is called once per frame
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
}
