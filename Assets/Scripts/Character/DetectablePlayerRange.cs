using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NetworkedPlayer;
using Character;
using GameUnit;

public class DetectablePlayerRange : MonoBehaviourPunCallbacks
{
    public void OnTriggerEnter(Collider other)
    {       
        // we dont do anything if we are not the local player.
        if (!photonView.IsMine)
        {
            return;
        }

        PlayerController player = this.transform.parent.GetComponent<PlayerController>();
        
        //page code

        if (!player.HasPage && other.CompareTag("Page"))
        {
            other.gameObject.GetPhotonView().TransferOwnership(PhotonNetwork.LocalPlayer);
            Page page = other.GetComponent<Page>();

            page.Follow(transform);
            
            
            player.HasPage = true;
            player.currentPage = page;
            player.isChannelingObjective = false;
            // Disable the channeling effect
            player.channelParticleSystem.SetActive(false);
            player.ringsParticleSystem.SetActive(false);
            Debug.Log($"Page has been picked up by player of {player.Team} team");
        }        

        if (other.gameObject == this.transform.parent.gameObject)
            return;

        var target = other.GetComponent<IGameUnit>();

        if(target is PlayerController || target is Minion)
        {
            // Debug.Log($"target = {target}");
            
            player.AddTarget(target);

            // Debug.Log("I am triggered. Target added.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // we dont do anything if we are not the local player.
        if (!photonView.IsMine)
        {
            return;
        }
        

        var target = other.GetComponent<IGameUnit>();

        PlayerController player = this.transform.parent.GetComponent<PlayerController>();        

        if(target is PlayerController || target is Minion)
        {
            // Debug.Log($"target = {target}");
            
            player.DeleteTarget(target);

            // Debug.Log("I am triggered. Target deleted.");
        }        
    }
}
