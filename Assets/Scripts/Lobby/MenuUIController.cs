using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Network;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Lobby
{
    public class MenuUIController : MonoBehaviour
    {
        public static MenuUIController Instance;
        
        [SerializeField] private HoverMenuButton findLobby;
        [SerializeField] private HoverMenuButton showTutorial;
        [SerializeField] private HoverMenuButton exitGame;
        [SerializeField] private HoverMenuButton serverStatus;

        [SerializeField] private HoverMenuInputField menuNameInput;


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

            menuButtons = new List<HoverMenuButton> { findLobby, showTutorial, exitGame, serverStatus, menuNameInput};

            ShowUI(0.5f);
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void OnButtonClicked(HoverMenuButton button)
        {
            foreach (HoverMenuButton hoverMenuButton in menuButtons.Except(new []{button}))
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
        
        public void FindLobby()
        {
            if (PhotonNetwork.NickName == "")
            {
                menuNameInput.ShowWarning = true;
            }
            
            OnButtonClicked(findLobby);

            LauncherController.Instance.Connect();
        }

        public void ShowTutorial()
        {
            OnButtonClicked(showTutorial);
            StartCoroutine(ActionAfterDelay(0.5f, () => SceneManager.LoadScene(2)));
        }

        public void Exit()
        {
            OnButtonClicked(exitGame);
            StartCoroutine(ActionAfterDelay(0.5f, Application.Quit));
        }

        public void OnPlayerNameChange()
        {
            if (menuNameInput.ShowWarning && PhotonNetwork.NickName != "")
            {
                menuNameInput.ShowWarning = false;
            }
        }

        private IEnumerator ActionAfterDelay(float delay, Action a)
        {
            yield return new WaitForSeconds(delay);
            a.Invoke();
        }
    }
}
