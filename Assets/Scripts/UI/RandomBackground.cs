using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RandomBackground : MonoBehaviour
    {

        public RandomBGValueGenerator RandomBgValueGenerator;
        public Sprite[] Backgrounds; //this is an array of type sprite
        public Image image; //this is a variable of type sprite renderer

        // Use this for initialization
        void Start ()
        {
            StartCoroutine(SetBackground());
        }

        public IEnumerator SetBackground()
        {
            yield return new WaitUntil(() => RandomBgValueGenerator.IsReady);
            image.sprite = Backgrounds[RandomBgValueGenerator.RandomValue]; 
        }
    }
}
