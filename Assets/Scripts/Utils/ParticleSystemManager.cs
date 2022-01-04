using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemManager : MonoBehaviour
{

    private bool[] aliveChildren;
    void Start()
    {
        aliveChildren = new bool[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            aliveChildren[i] = true;
        }
    }

    void Update()
    {
        int counter = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (!transform.GetChild(i).gameObject.GetComponent<ParticleSystem>().IsAlive())
            {
                aliveChildren[i] = false;
                counter++;
            }
        }

        if (counter == transform.childCount - 1)
        {
            Debug.Log("Getting Destroyed now" + counter);
            Destroy(gameObject);
            
        }
    }
}
