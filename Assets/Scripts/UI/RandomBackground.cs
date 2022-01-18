using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomBackground : MonoBehaviour 
{

    public Sprite[] Backgrounds; //this is an array of type sprite
    public Image image; //this is a variable of type sprite renderer

    // Use this for initialization
    void Start () 
    {

        image.sprite = Backgrounds[Random.Range(0, Backgrounds.Length)]; 
        /*this will change the current sprite of the sprite renderer to a random sprite that was chosen 
        randomly from the array of backgrounds */
    }

    // Update is called once per frame
    void Update () 
    {

    }
}
