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
        if (gameObj.name == "RED" || gameObj.name == "GREEN" || gameObj.name == "YELLOW" || gameObj.name == "BLUE")
        {
            PlayerController currentPlayer = PlayerController.LocalPlayerController;

            if (gameObj.transform.Find("InnerChannelingParticleSystem").gameObject.activeSelf == false)
            {
                Debug.Log($"Player {currentPlayer.gameObject.name} of team {currentPlayer.Team} threw {this.gameObject.name} on {gameObj.name}.");
                
                GameObject innerChannelingParticleSystem = gameObj.transform.Find("InnerChannelingParticleSystem").gameObject;
                innerChannelingParticleSystem.SetActive(true);

                // Set this in the player themselves
                ParticleSystem channelParticleSystem = innerChannelingParticleSystem.GetComponent<ParticleSystem>();

                // Set particles color
                float hSliderValueR = 0.0f;
                float hSliderValueG = 0.0f;
                float hSliderValueB = 0.0f;
                float hSliderValueA = 1.0f;

                PlayerController caster = this.gameObject.transform.parent.gameObject.GetComponent<PlayerController>();

                if(caster.Team.ToString() == "RED")
                {
                    hSliderValueR = 1;
                    hSliderValueG = 0;
                    hSliderValueB = 0;
                    hSliderValueA = 1;                    
                }
                else if (caster.Team.ToString() == "YELLOW")
                {
                    hSliderValueR = 1;
                    hSliderValueG = 1;
                    hSliderValueB = 0;
                    hSliderValueA = 1;                     
                }
                else if (caster.Team.ToString() == "GREEN")
                {
                    hSliderValueR = 0;
                    hSliderValueG = 1;
                    hSliderValueB = 0;
                    hSliderValueA = 1;                    
                }
                else if (caster.Team.ToString() == "BLUE")
                {
                    hSliderValueR = 0;
                    hSliderValueG = 1;
                    hSliderValueB = 1;
                    hSliderValueA = 1;                      
                }

                Color color = new Color(hSliderValueR, hSliderValueG, hSliderValueB, hSliderValueA);
                var channelParticleSystemMain = channelParticleSystem.main;
                channelParticleSystemMain.startColor = color;
            }
        }
        else if(gameObj.name.StartsWith("Slender"))
        {
            PlayerController currentPlayer = PlayerController.LocalPlayerController;

            if (gameObj.transform.Find("InnerChannelingParticleSystem").gameObject.activeSelf == false)
            {
                Debug.Log($"Player {currentPlayer.gameObject.name} of team {currentPlayer.Team} threw {this.gameObject.name} on {gameObj.name}.");
                
                GameObject innerChannelingParticleSystem = gameObj.transform.Find("InnerChannelingParticleSystem").gameObject;
                innerChannelingParticleSystem.SetActive(true);
            }
        }
    }
}
