using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Network;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Lobby
{
    public class TutorialMenuUIController : MonoBehaviour
    {
        public static TutorialMenuUIController Instance;
        
        [SerializeField] private Button howToPlay;
        [SerializeField] private Button slenderman;
        [SerializeField] private Button bases;
        [SerializeField] private Button exitTutorial;

        void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void HowToPlay()
        {
        }

        public void Slenderman()
        {
        }

        public void Bases()
        {
        }

        public void BackToLobby()
        {
            SceneManager.LoadScene(0);
        }

    }
}