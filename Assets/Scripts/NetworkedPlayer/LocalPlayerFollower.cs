using System.Collections;
using System.Collections.Generic;
using NetworkedPlayer;
using UnityEngine;

public class LocalPlayerFollower : MonoBehaviour
{
    private Transform toFollow;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (PlayerController.LocalPlayerInstance == null || PlayerController.LocalPlayerInstance.Equals(null))
        {
            return;
        }
        
        if (toFollow == null || toFollow.Equals(null) )
        {
            toFollow = PlayerController.LocalPlayerInstance.transform;
        }

        gameObject.transform.position = toFollow.position;
    }
}
