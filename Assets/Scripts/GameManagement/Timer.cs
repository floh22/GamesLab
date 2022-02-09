using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace GameManagement
{
    public class Timer : MonoBehaviour
    {
        public float timeRemainingInSeconds = GameData.SecondsPerRound;
        public TextMeshProUGUI timeRemainingComponent;

        public List<GameObject> visuals = new List<GameObject>();

        private int minutes;
        private int seconds;
        private string niceTime;
        
        void Update()
        {
            if (timeRemainingInSeconds <= 0) return;
            if(PhotonNetwork.IsMasterClient)
                timeRemainingInSeconds = Mathf.Max(0, timeRemainingInSeconds - Time.deltaTime);
            
            minutes = Mathf.FloorToInt(timeRemainingInSeconds / 60F);
            seconds = Mathf.FloorToInt(timeRemainingInSeconds - minutes * 60);
            niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);

            timeRemainingComponent.text = niceTime;
        }

        public void SetInactive()
        {
            timeRemainingComponent.enabled = false;
            foreach (GameObject visual in visuals)
            {
                visual.SetActive(false);
            }
        }
    }
}