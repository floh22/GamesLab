using TMPro;
using UnityEngine;

namespace GameManagement
{
    public class Timer : MonoBehaviour
    {
        public float timeRemainingInSeconds = 300;
        public TextMeshProUGUI timeRemainingComponent;

        private int minutes;
        private int seconds;
        private string niceTime;

        void Start()
        {
        }
        void Update()
        {        
            if (timeRemainingInSeconds > 0)
            {
                timeRemainingInSeconds -= Time.deltaTime;
            
                minutes = Mathf.FloorToInt(timeRemainingInSeconds / 60F);
                seconds = Mathf.FloorToInt(timeRemainingInSeconds - minutes * 60);
                niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);

                timeRemainingComponent.text = niceTime;
            }
            else
            {
                // Debug.Log("Time has run out!");
            }
        }
    }
}