using System;
using System.Collections;
using JetBrains.Annotations;
using Network;
using NetworkedPlayer;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameManagement
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        
        #region Standard

        [CanBeNull] private MasterController controller;
        private PlayerController _playerController;

        #endregion

        #region BasicUI
        
        public GameObject PauseMenuUI;
        public GameObject IngameUI;
        public Image MinionSwitchButtonImage;

        private GameObject PagesContainer;
        private Image[] PagesImages;

        [SerializeField] private GameObject AutoAttackOnImage;
        [SerializeField] private GameObject AutoAttackOffImage;

        #endregion

        #region Level

        [SerializeField] private TextMeshProUGUI LevelLabel;
        [SerializeField] private TextMeshProUGUI LevelUpLabel;
        private GameObject[] levelUpButtons;
        private Image Circular_Meter_1;
        private Image Circular_Meter_2;
        private Image Circular_Meter_3;
        
        #endregion
        
        
        public Timer GameTimer;

        #region Scoreboard

        [SerializeField] private TextMeshProUGUI Player_Pages_Label_1;
        [SerializeField] private TextMeshProUGUI Player_Pages_Label_2;
        [SerializeField] private TextMeshProUGUI Player_Pages_Label_3;
        [SerializeField] private TextMeshProUGUI Player_Level_Label_1;
        [SerializeField] private TextMeshProUGUI Player_Level_Label_2;
        [SerializeField] private TextMeshProUGUI Player_Level_Label_3;

        #endregion

        void Start()
        {
            
            if (Instance == null)
            {
                Instance = this;
            }

            PauseMenuUI = GameObject.Find("PauseMenu_UI");
            if (PauseMenuUI != null && !PauseMenuUI.Equals(null))
                PauseMenuUI.SetActive(false);
            IngameUI = GameObject.Find("Ingame_UI");

            MinionSwitchButtonImage = GameObject.Find("Player_Color_Sprite").GetComponent<Image>();

            AutoAttackOnImage = GameObject.Find("ON_Sprite");
            AutoAttackOffImage = GameObject.Find("OFF_Sprite");

            LevelLabel = GameObject.Find("Level_Text").GetComponent<TextMeshProUGUI>();
            LevelUpLabel = GameObject.Find("LevelUp_Label").GetComponent<TextMeshProUGUI>();
            LevelUpLabel.enabled = false;
            levelUpButtons = GameObject.FindGameObjectsWithTag("LevelUpButton");
            Circular_Meter_1 = GameObject.Find("Circular_Meter_1").GetComponent<Image>();
            Circular_Meter_2 = GameObject.Find("Circular_Meter_2").GetComponent<Image>();
            Circular_Meter_3 = GameObject.Find("Circular_Meter_3").GetComponent<Image>();

            Player_Pages_Label_1 = GameObject.Find("Player_Pages_Number_1").GetComponent<TextMeshProUGUI>();
            Player_Pages_Label_2 = GameObject.Find("Player_Pages_Number_2").GetComponent<TextMeshProUGUI>();
            Player_Pages_Label_3 = GameObject.Find("Player_Pages_Number_3").GetComponent<TextMeshProUGUI>();

            Player_Level_Label_1 = GameObject.Find("Player_Level_1").GetComponent<TextMeshProUGUI>();
            Player_Level_Label_2 = GameObject.Find("Player_Level_2").GetComponent<TextMeshProUGUI>();
            Player_Level_Label_3 = GameObject.Find("Player_Level_3").GetComponent<TextMeshProUGUI>();

            UpdateCircularMeters();
            SetVisibilityOfLevelUpButtons(false);

            _playerController = PlayerController.LocalPlayerController;

            SetMinionTarget(GetNextPlayerClockwise(PersistentData.Team ?? GameData.Team.RED, true));

            PagesContainer = GameObject.Find("Pages Container");
            PagesImages = PagesContainer.GetComponentsInChildren<Image>();

            SetAutoAttack(GameData.Instance.AutoAttack);

            SetInitPages();
        }

        // Update is called once per frame
        void Update()
        {
            if (_playerController != null)
            {
                LevelLabel.text = _playerController.Level.ToString();
            }
        }

        public void TogglePauseMenu()
        {
            if (GameData.Instance.GameIsPaused)
            {
                PauseMenuUI.SetActive(false);
                IngameUI.SetActive(true);
                GameData.Instance.GameIsPaused = false;
            }
            else
            {
                PauseMenuUI.SetActive(true);
                IngameUI.SetActive(false);
                GameData.Instance.GameIsPaused = true;
            }
        }

        public void SetMinionTarget(GameData.Team target)
        {
            GameData.Instance.SelectedMinionTarget = target;
            MinionSwitchButtonImage.color = GetColor(target);
        }

        public void SetMinionsToNextTarget()
        {
            SetMinionTarget(GetNextPlayerClockwise(GameData.Instance.SelectedMinionTarget, true));
        }

        public void SetAutoAttack(bool state)
        {
            GameData.Instance.AutoAttack = state;
            if (state)
            {
                AutoAttackOnImage.SetActive(true);
                AutoAttackOffImage.SetActive(false);
            }
            else
            {
                AutoAttackOnImage.SetActive(false);
                AutoAttackOffImage.SetActive(true);
            }
        }

        public void SwitchAutoAttack()
        {
            SetAutoAttack(!GameData.Instance.AutoAttack);
        }
        
        public void SetPages(int count)
        {
            while (count != GameData.Instance.NumberOfPages)
            {
                if (count > GameData.Instance.NumberOfPages)
                {
                    IncreasePages();
                }
                else
                {
                    DecreasePages();
                }
            }
        }

        public void IncreasePages()
        {
            if (GameData.Instance.NumberOfPages < 10)
            {
                PagesImages[GameData.Instance.NumberOfPages].enabled = true;
                GameData.Instance.NumberOfPages++;
            }
        }

        public void DecreasePages()
        {
            if (GameData.Instance.NumberOfPages - 1 >= 0)
            {
                GameData.Instance.NumberOfPages--;
                PagesImages[GameData.Instance.NumberOfPages].enabled = false;
            }
        }

        void SetInitPages()
        {
            for (int i = 9; i >= GameData.Instance.NumberOfPages; i--)
            {
                PagesImages[i].enabled = false;
            }
        }

        GameData.Team GetNextPlayerClockwise(GameData.Team team, bool CountOwnPlayer)
        {
            int temp = (((int) team - 1) + 4) % 4;
            if (!CountOwnPlayer)
            {
                return (GameData.Team) temp;
            }
            else
            {
                if ((GameData.Team) temp == (PersistentData.Team ?? GameData.Team.RED))
                {
                    temp = (((int) temp - 1) + 4) % 4;
                }
            }

            return (GameData.Team) temp;
        }

        Color GetColor(GameData.Team team)
        {
            float[] color = GameData.PlayerColors[(int) team];
            return new Color(color[0], color[1], color[2], color[3]);
        }

        public IEnumerator ShowLevelUpLabel()
        {
            LevelUpLabel.enabled = true;
            SetVisibilityOfLevelUpButtons(true);
            yield return new WaitForSeconds(2);
            LevelUpLabel.enabled = false;
        }

        private void SetVisibilityOfLevelUpButtons(bool visibility)
        {
            if (_playerController == null || _playerController.Equals(null))
            {
                _playerController = PlayerController.LocalPlayerController;
                
                if (_playerController == null || _playerController.Equals(null))
                    return;
            }

            foreach (GameObject button in levelUpButtons)
            {
                //Don't judge this code it works and I didn't have the time to make it better :P
                if (button.name == "Uprade_Button_1" && _playerController.UpgradesAbility1 < 4 || !visibility)
                {
                    button.SetActive(visibility);
                }

                if (button.name == "Uprade_Button_2" && _playerController.UpgradesAbility2 < 4 || !visibility)
                {
                    button.SetActive(visibility);
                }

                if (button.name == "Uprade_Button_3" && _playerController.UpgradesMinion < 4 || !visibility)
                {
                    button.SetActive(visibility);
                }
            }
        }

        private void UpdateCircularMeters()
        {
            if (_playerController == null || _playerController.Equals(null))
            {
                _playerController = PlayerController.LocalPlayerController;

                if (_playerController == null || _playerController.Equals(null))
                    return;
            }

            Circular_Meter_1.fillAmount = _playerController.UpgradesAbility1 * 0.25f;
            Circular_Meter_2.fillAmount = _playerController.UpgradesAbility2 * 0.25f;
            Circular_Meter_3.fillAmount = _playerController.UpgradesMinion * 0.25f;
        }

        public void UpdateButtonClicked(int which)
        {
            if (_playerController == null || _playerController.Equals(null))
            {
                _playerController = PlayerController.LocalPlayerInstance.GetComponent<PlayerController>();
            }

            _playerController.UpdateMultiplier(which);
            UpdateCircularMeters();
            SetVisibilityOfLevelUpButtons(false);
        }

        public void UIHELPERMETHODAddExperience(int experience)
        {
            if (_playerController == null || _playerController.Equals(null))
            {
                _playerController = PlayerController.LocalPlayerController;
                
                if (_playerController == null || _playerController.Equals(null))
                    return;
            }

            _playerController.AddExperience(experience);
        }
    }
}