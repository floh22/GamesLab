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
        
        
        [SerializeField] private HoverMenuButton howToPlayButton;
        [SerializeField] private HoverMenuButton slendermanButton;
        [SerializeField] private HoverMenuButton basesButton;
        [SerializeField] private HoverMenuButton exitButton;


        [SerializeField] private float buttonClickDelay = 0.2f;

        private List<HoverMenuButton> menuButtons;
        // Start is called before the first frame update
        void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            menuButtons = new List<HoverMenuButton> { howToPlayButton, slendermanButton, basesButton, exitButton};

            ShowUI(0.5f);
        }
        
        public void OnButtonClicked(HoverMenuButton button)
        {
            foreach (HoverMenuButton hoverMenuButton in (button == exitButton ? menuButtons.Except(new[] { button }).ToList() : menuButtons.Except(new[] { button, exitButton }).ToList()))
            {
                hoverMenuButton.Click();
            }

            StartCoroutine(ActionAfterDelay(buttonClickDelay, button.Click));
        }

        public void ShowUI(float delay = 0)
        {
            StartCoroutine(ShowUIStaggered(delay));
        }
        
        private IEnumerator ShowUIStaggered(float delay = 0)
        {
            if (delay != 0)
                yield return new WaitForSeconds(delay);
            foreach (HoverMenuButton hoverMenuButton in menuButtons)
            {
                hoverMenuButton.Show();
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private IEnumerator ActionAfterDelay(float delay, Action a)
        {
            yield return new WaitForSeconds(delay);
            a.Invoke();
        }

        public void HowToPlay()
        {
            OnButtonClicked(howToPlayButton);
            
            StartCoroutine(ActionAfterDelay(buttonClickDelay, () =>
            {
                howToPlayView.SetActive(true);
                slendermanView.SetActive(false);
                basesView.SetActive(false);
            }));
        }

        public void Slenderman()
        {
            OnButtonClicked(slendermanButton);
            StartCoroutine(ActionAfterDelay(buttonClickDelay, () =>
            {
                howToPlayView.SetActive(false);
                slendermanView.SetActive(true);
                basesView.SetActive(false);
            }));
        }

        public void Bases()
        {
            OnButtonClicked(basesButton);
            StartCoroutine(ActionAfterDelay(buttonClickDelay, () =>
            {
                howToPlayView.SetActive(false);
                slendermanView.SetActive(false);
                basesView.SetActive(true);
            }));
        }

        public void BackToLobby()
        {
            if (howToPlayView.activeSelf || slendermanView.activeSelf || basesView.activeSelf)
            {
                ShowUI();
                howToPlayView.SetActive(false);
                slendermanView.SetActive(false);
                basesView.SetActive(false);
            }
            else
            {
                OnButtonClicked(exitButton);

                StartCoroutine(ActionAfterDelay(buttonClickDelay * 1.5f, () => SceneManager.LoadScene(0)));
            }
        }

    }
}