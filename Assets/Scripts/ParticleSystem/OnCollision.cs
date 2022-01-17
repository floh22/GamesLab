using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCollision : MonoBehaviour
{
    public ParticleSystem part;
    public List<ParticleCollisionEvent> collisionEvents;

    // Start is called before the first frame update
    void Start()
    {
        part = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnParticleCollision(GameObject gameObj)
    {
        Debug.Log($"Collision with {gameObj.name} detected.");

        if (gameObj.name == "RED" || gameObj.name == "GREEN" || gameObj.name == "YELLOW" || gameObj.name == "BLUE" || gameObj.name == "Slender")
        {            
            gameObj.transform.Find("InnerChannelingParticleSystem").gameObject.SetActive(true);
        }

        //int numCollisionEvents = part.GetCollisionEvents(gameObj, collisionEvents);
    }
}
