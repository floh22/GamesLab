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
        
        [SerializeField] private GameObject howToPlayView;
        [SerializeField] private GameObject slendermanView;
        [SerializeField] private GameObject basesView;

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
            howToPlayView.SetActive(true);
            slendermanView.SetActive(false);
            basesView.SetActive(false);
        }

        public void Slenderman()
        {
            howToPlayView.SetActive(false);
            slendermanView.SetActive(true);
            basesView.SetActive(false);
        }

        public void Bases()
        {
            howToPlayView.SetActive(false);
            slendermanView.SetActive(false);
            basesView.SetActive(true);
        }

        public void BackToLobby()
        {
            SceneManager.LoadScene(0);
        }

    }
}