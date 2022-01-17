using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character;
using GameUnit;
using JetBrains.Annotations;
using Network;
using NetworkedPlayer;
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

        [SerializeField] private Image SlenderImage;
        private bool IsSlenderCooldown;
        private float SlenderCooldownDuration;

        private Image HealthbarImage;
        [SerializeField] private Image OwnPlayerLevelBackgroundImage; 

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

        public HashSet<GameData.Team> ScoreboardEntries { get; set; }
        [SerializeField] private TextMeshProUGUI[] Player_Pages_Labels;
        [SerializeField] private TextMeshProUGUI[] Player_Level_Labels;
        [SerializeField] private Image[] Player_Sprite_Images;
        [SerializeField] private Image[] Player_Pages_Images;
        [SerializeField] private Image[] Player_Level_Background_Images;
        private int _currentlyDisplayedPlaser;

        #endregion

        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            ScoreboardEntries = new HashSet<GameData.Team>();
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

            UpdateCircularMeters();
            SetVisibilityOfLevelUpButtons(false);

            _playerController = PlayerController.LocalPlayerController;

            PagesContainer = GameObject.Find("Pages Container");
            PagesImages = PagesContainer.GetComponentsInChildren<Image>();

            SetAutoAttack(GameData.Instance.AutoAttack);

            SlenderImage = GameObject.Find("SlenderImage").GetComponent<Image>();
            HealthbarImage = GameObject.Find("Healthbar_InnerBar").GetComponent<Image>();

            GameStateController.LocalPlayerSpawnEvent.AddListener(SetupUI);
        }

        // Update is called once per frame
        void Update()
        {
            _playerController = PlayerController.LocalPlayerController;
            if (_playerController != null)
            {
                LevelLabel.text = _playerController.Level.ToString();
                HealthbarImage.fillAmount = _playerController.Health / 100;
            }

            if (IsSlenderCooldown)
            {
                SlenderImage.fillAmount -= 1 / SlenderCooldownDuration * Time.deltaTime;
                if (SlenderImage.fillAmount <= 0)
                {
                    IsSlenderCooldown = false;
                    SlenderImage.enabled = false;
                }
            }

            UpdateScoreboard();
        }

        void SetupUI()
        {
            _playerController = PlayerController.LocalPlayerController;

            SetPages(PlayerValues.PagesAmount);
            SetVisibilityOfLevelUpButtons(false);
            UpdateCircularMeters();
            OwnPlayerLevelBackgroundImage.color = GetColor(_playerController.Team);
            SetMinionTarget(GetNextPlayerClockwise(PersistentData.Team ?? GameData.Team.RED, true));

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
            if (_playerController == null || _playerController.Equals(null))
            {
                _playerController = PlayerController.LocalPlayerInstance.GetComponent<PlayerController>();
            }
            GameData.Instance.SelectedMinionTarget = target;
            GameStateController.SendChangeTargetEvent(_playerController.Team, target);
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

        //HELPERMETHODDONTUSE
        public void IncreasePages()
        {
            if (GameData.Instance.NumberOfPages < 10)
            {
                BaseBehavior currentBase = GameStateController.Instance.Bases.FirstOrDefault(x => x.Key == PersistentData.Team).Value;
                currentBase.Pages--;
                SetPages(currentBase.Pages);
            }
        }

        //HELPERMETHODDONTUSE
        public void DecreasePages()
        {
            if (GameData.Instance.NumberOfPages - 1 >= 0)
            {
                BaseBehavior currentBase = GameStateController.Instance.Bases.FirstOrDefault(x => x.Key == PersistentData.Team).Value;
                currentBase.Pages--;
                SetPages(currentBase.Pages);

            }
        }

        public void SetPages()
        {
            SetPages(GetPagesForTeam(PersistentData.Team ?? GameData.Team.RED));
        }

        public void SetPages(int count)
        {
            int counter = 0;
            for (int i = 0; i < count; i++)
            {
                PagesImages[i].enabled = true;
                counter++;
            }

            for (int i = counter; i < 10; i++)
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

        void SetVisibilityOfLevelUpButtons(bool visibility)
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

        void UpdateCircularMeters()
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

        public void ShowSlenderBuffCountdown(float duration)
        {
            SlenderImage.enabled = true;
            SlenderImage.fillAmount = 1;
            SlenderCooldownDuration = duration;
            IsSlenderCooldown = true;
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

        public void UIHELPERMETHOD2()
        {
            if (_playerController == null || _playerController.Equals(null))
            {
                _playerController = PlayerController.LocalPlayerController;

                if (_playerController == null || _playerController.Equals(null))
                    return;
            }

            _playerController.OnReceiveSlendermanBuff();
        }

        void UpdateScoreboard()
        {
            if (GameStateController.Instance == null || GameStateController.Instance.Players == null)
            {
                return;
            }
            Dictionary<GameData.Team, PlayerController> players = GameStateController.Instance.Players;

            int counter = 0;
            foreach (KeyValuePair<GameData.Team, PlayerController> entry in players)
            {
                if (!ScoreboardEntries.Contains(entry.Key))
                {
                    if (entry.Key != (PersistentData.Team ?? GameData.Team.RED))
                    {
                        ScoreboardEntries.Add(entry.Value.Team);
                        DisplayRowInScoreboard(counter, true);
                        UpdateRowInScoreboard(counter, entry.Value.Team);
                    }
                }
                else
                {
                    UpdateRowInScoreboard(counter, entry.Value.Team);
                }
                counter++;
            }
            
            for (int i = counter; i < 3; i++)
            {
                DisplayRowInScoreboard(i, false);
            }
        }

        void DisplayRowInScoreboard(int i, bool value)
        {
            Player_Sprite_Images[i].enabled = value;
            Player_Pages_Labels[i].enabled = value;
            Player_Level_Labels[i].enabled = value;
            Player_Pages_Images[i].enabled = value;
            Player_Level_Background_Images[i].enabled = value;
        }

        void UpdateRowInScoreboard(int i, GameData.Team team)
        {
            PlayerController currentPlayer = GameStateController.Instance.Players.FirstOrDefault(x => x.Key == team).Value;
            BaseBehavior currentBase = GameStateController.Instance.Bases.FirstOrDefault(x => x.Key == team).Value;
            Player_Level_Labels[i].text = currentPlayer.Level.ToString();
            Player_Pages_Labels[i].text = "x" + currentBase.Pages.ToString();
            Player_Sprite_Images[i].color = GetColor(team);
        }

        private int GetPagesForTeam(GameData.Team team)
        {
            if (GameStateController.Instance == null)
            {
                return 0;
            }

            BaseBehavior currentBase = GameStateController.Instance.Bases.FirstOrDefault(x => x.Key == team).Value;
            return currentBase.Pages;
        }
    }
}