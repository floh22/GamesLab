using Photon.Pun;
using TMPro;
using UnityEngine;

namespace GameManagement
{
    public class Timer : MonoBehaviour
    {
        public float timeRemainingInSeconds = GameData.SecondsPerRound;
        public TextMeshProUGUI timeRemainingComponent;

        private int minutes;
        private int seconds;
        private string niceTime;
        
        void Update()
        {
            if (timeRemainingInSeconds <= 0) return;
            if(PhotonNetwork.IsMasterClient)
                timeRemainingInSeconds -= Mathf.Max(0, Time.deltaTime);
            
            minutes = Mathf.FloorToInt(timeRemainingInSeconds / 60F);
            seconds = Mathf.FloorToInt(timeRemainingInSeconds - minutes * 60);
            niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);

            timeRemainingComponent.text = niceTime;
        }
    }
}