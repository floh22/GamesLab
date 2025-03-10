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
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
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

        [Header("UI Components")]
        public GameObject PauseMenuUI;
        public GameObject IngameUI;
        public GameObject GameOverUI;
        public Image MinionSwitchButtonImage;
        public GameObject ActionButtonsGroup;
        public GameObject LevelUpButtonsGroup;

        
        private GameObject PagesContainer;
        private Image[] PagesImages;
        [Header("Base Stats")]
        [SerializeField] private RectTransform baseHealthTransform;
        private float baseHealthDisplayWidth;

        [Header("Auto Attack Swap")]
        [SerializeField] private GameObject AutoAttackOnImage;
        [SerializeField] private GameObject AutoAttackOffImage;

        [Header("Slenderman Indicator")]
        [SerializeField] private Image SlenderImage;
        private bool IsSlenderCooldown;
        private float SlenderCooldownDuration;

        [Header("Death Indicator")]
        [SerializeField] private Image DeathIndicatorImage; 
        private bool IsDeathCooldown;
        private float DeathCooldownDuration;
        
        [Header("Game Over")]
        [SerializeField] private TextMeshProUGUI GameOver_Stat_Text_1; 
        [SerializeField] private TextMeshProUGUI GameOver_Stat_Text_2; 
        [SerializeField] private TextMeshProUGUI GameOver_Stat_Text_3;

        [Header("Post Game")] 
        [SerializeField] private GameObject fogOfWar;
        [SerializeField] private GameObject deadPlayerPrefab;
        private GameObject deadPlayer;
        private Vector3 losePosition;


        #endregion

        #region Level
        
        [Header("Level Up")]

        [SerializeField] private TextMeshProUGUI LevelLabel;
        [SerializeField] private TextMeshProUGUI LevelUpLabel;
        private GameObject[] levelUpButtons;
        
        [Header("Abilities")]
        public Image Circular_Meter_RANGE;
        public Image Circular_Meter_LINE;
        public Image Circular_Meter_MINION;

        #endregion


        [FormerlySerializedAs("GameTimer")] public Timer gameTimer;
        public bool isGameOver = false;

        #region Scoreboard

        public HashSet<GameData.Team> ScoreboardEntries { get; set; }
        [Header("Scoreboard")]
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

            GameObject parentGameObject = this.gameObject.transform.parent.gameObject;

            ScoreboardEntries = new HashSet<GameData.Team>();
            PauseMenuUI = parentGameObject.transform.Find("PauseMenu_UI").gameObject;

            if (PauseMenuUI != null && !PauseMenuUI.Equals(null))
                PauseMenuUI.SetActive(false);

            IngameUI = parentGameObject.transform.Find("Ingame_UI").gameObject;
            GameObject actionButtonsGroup = IngameUI.transform.Find("Action Buttons Group").gameObject;
            MinionSwitchButtonImage = actionButtonsGroup.transform.Find("Minion Switch").gameObject.transform.Find("Player_Color_Sprite").gameObject.GetComponent<Image>();
            AutoAttackOnImage = actionButtonsGroup.transform.Find("Auto Attack Switch").gameObject.transform.Find("ON_Sprite").gameObject;
            AutoAttackOffImage = actionButtonsGroup.transform.Find("Auto Attack Switch").gameObject.transform.Find("OFF_Sprite").gameObject;
            LevelLabel = IngameUI.transform.Find("Level Panel").gameObject.transform.Find("Level_Text").gameObject.GetComponent<TextMeshProUGUI>();
            LevelUpLabel = IngameUI.transform.Find("LevelUp_Label").gameObject.GetComponent<TextMeshProUGUI>();

            LevelUpLabel.enabled = false;
            levelUpButtons = GameObject.FindGameObjectsWithTag("LevelUpButton");

            UpdateCircularMeters();
            SetVisibilityOfLevelUpButtons(false);

            _playerController = PlayerController.LocalPlayerController;

            baseHealthDisplayWidth = baseHealthTransform.rect.width;

            PagesContainer = IngameUI.transform.Find("TopRightUI").transform.Find("Pages Container").gameObject;
            PagesImages = PagesContainer.GetComponentsInChildren<Image>();
            
            Debug.Log($"Found {PagesImages.Length} pages");

            SetAutoAttack(GameData.Instance.AutoAttack);

            SlenderImage = IngameUI.transform.Find("SlenderIndicator").gameObject.GetComponent<Image>();

            if (PlayerController.LocalPlayerController != null)
            {
                SetupUI();
            }
            else
                GameStateController.LocalPlayerSpawnEvent.AddListener(SetupUI);
            
        }

        // Update is called once per frame
        void Update()
        {
            if (isGameOver)
            {
                return;
            }
            _playerController = PlayerController.LocalPlayerController;
            if (_playerController != null)
            {
                LevelLabel.text = _playerController.Level.ToString();
            }
            
            if (GameStateController.Instance != null && GameStateController.Instance.Bases != null && GameStateController.Instance.Bases.TryGetValue(PersistentData.Team ?? throw new NullReferenceException(), out BaseBehavior ownBase))
            {
                baseHealthTransform.sizeDelta =
                    new Vector2((ownBase.Health / ownBase.MaxHealth) * baseHealthDisplayWidth, baseHealthTransform.rect.height);
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
            
            if (IsDeathCooldown)
            {
                DeathIndicatorImage.fillAmount -= 1 / DeathCooldownDuration * Time.deltaTime;
                if (DeathIndicatorImage.fillAmount <= 0)
                {
                    IsDeathCooldown = false;
                    DeathIndicatorImage.enabled = false;
                    ActionButtonsGroup.SetActive(true);
                    LevelUpButtonsGroup.SetActive(true);
                }
            }

            UpdateScoreboard();
            CheckForUnspentLevelUps();
        }

        public void SetupUI()
        {
            Debug.Log("Local Player spawned");
            _playerController = PlayerController.LocalPlayerController;

            SetPages(PlayerValues.PagesAmount);
            SetVisibilityOfLevelUpButtons(false);
            UpdateCircularMeters();
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
                if (PlayerController.LocalPlayerController != null)
                {
                    PlayerController.LocalPlayerController.AutoAttackOn();
                }
                AutoAttackOnImage.SetActive(true);
                AutoAttackOffImage.SetActive(false);
            }
            else
            {
                if (PlayerController.LocalPlayerController != null)
                {
                    PlayerController.LocalPlayerController.AutoAttackOff();
                }
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

        void CheckForUnspentLevelUps()
        {
            if (PlayerController.LocalPlayerInstance == null)
            {
                return;
            }
            if (_playerController == null)
            {
                _playerController = PlayerController.LocalPlayerInstance.GetComponent<PlayerController>();
            }

            if (_playerController.UnspentLevelUps > 0)
            {
                SetVisibilityOfLevelUpButtons(true);
            }
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

            Circular_Meter_RANGE.fillAmount = _playerController.UpgradesAbility1 * 0.25f;
            Circular_Meter_LINE.fillAmount = _playerController.UpgradesAbility2 * 0.25f;
            Circular_Meter_MINION.fillAmount = _playerController.UpgradesMinion * 0.25f;
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

        public void ShowDeathIndicatorCountdown(float duration)
        {
            DeathIndicatorImage.enabled = true;
            DeathIndicatorImage.fillAmount = 1;
            DeathCooldownDuration = duration + 0.5f;
            IsDeathCooldown = true;
            ActionButtonsGroup.SetActive(false);
            LevelUpButtonsGroup.SetActive(false);
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

            // _playerController.OnReceiveSlendermanBuff();
            _playerController.Health -= 200;
        }
        
        public void UIHELPERMETHOD3()
        {
            if (_playerController == null || _playerController.Equals(null))
            {
                _playerController = PlayerController.LocalPlayerController;

                if (_playerController == null || _playerController.Equals(null))
                    return;
            }

            var closest = PersistentData.Team?? throw  new NullReferenceException();
            var closestDist = Mathf.Infinity;
            
            foreach (var (key, value) in GameStateController.Instance.Bases)
            {
                var dist = Vector3.Distance(PlayerController.LocalPlayerController.Position, value.Position);
                if (!(dist < closestDist)) continue;
                closest = key;
                closestDist = dist;
            }
            GameStateController.Instance.Bases[closest].Pages--;

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
                        counter++;
                    }
                }
                else
                {
                    UpdateRowInScoreboard(counter, entry.Value.Team);
                    counter++;
                }
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

        public void SetGameOver(int kills, int deaths, int playerRemaining, Vector3 losePosition)
        {
            fogOfWar.SetActive(false);
            
            foreach (IGameUnit gameUnit in GameStateController.Instance.GameUnits.NotNull())
            {
                if(gameUnit.Equals(null) || gameUnit.IsDestroyed())
                    continue;
                switch (gameUnit.Type)
                {
                    case GameUnitType.None:
                        break;
                    case GameUnitType.Structure:
                        break;
                    case GameUnitType.Player:
                        PlayerController pc = (PlayerController)gameUnit;
                        pc.gameObject.GetComponent<UnitVisibilityScript>().enabled = false;
                        break;
                    case GameUnitType.Minion:
                        Minion minion = (Minion)gameUnit;
                        minion.gameObject.GetComponent<UnitVisibilityScript>().enabled = false;
                        break;
                    case GameUnitType.Monster:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            this.losePosition = losePosition;
            isGameOver = true;
            PauseMenuUI.SetActive(false);
            //gameTimer.SetInactive();
            IngameUI.SetActive(false);
            GameOverUI.SetActive(true);
            GameOver_Stat_Text_1.text = kills.ToString();
            GameOver_Stat_Text_2.text = deaths.ToString();
            GameOver_Stat_Text_3.text = playerRemaining.ToString();
        }
        
        public void CloseLoseRecap()
        {
            IngameUI.SetActive(true);
            GameOverUI.SetActive(false);
            
            deadPlayer = Instantiate(deadPlayerPrefab, losePosition, Quaternion.identity);
            CameraController deadCameraController = deadPlayer.GetComponent<CameraController>();
            //follow dead character
            deadCameraController.OnStartFollowing();
        }
    }
}