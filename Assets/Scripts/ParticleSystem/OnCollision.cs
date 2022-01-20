using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkedPlayer;
public class OnCollision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnParticleCollision(GameObject gameObj)
    {        
        if (gameObj.name == "RED" || gameObj.name == "GREEN" || gameObj.name == "YELLOW" || gameObj.name == "BLUE" || gameObj.name.StartsWith("Slender"))
        {
            PlayerController currentPlayer = PlayerController.LocalPlayerController;

            if (gameObj.transform.Find("InnerChannelingParticleSystem").gameObject.activeSelf == false)
            {
                Debug.Log($"Player {currentPlayer.gameObject.name} of team {currentPlayer.Team} threw {this.gameObject.name} on {gameObj.name}.");
                gameObj.transform.Find("InnerChannelingParticleSystem").gameObject.SetActive(true);
            }
        }
    }
}
