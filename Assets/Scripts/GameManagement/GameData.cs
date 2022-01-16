using System;
using UnityEngine;

namespace GameManagement
{
    [Serializable]
    public class GameData : MonoBehaviour
    {
        public static GameData Instance {get; private set;}

        public const float SecondsPerRound = 300;

        public enum Levels {
            START = 0,
            MAP = 1,
            GAME_END = 2,
        }

        [Serializable]
        public enum Team {
            RED = 0,
            GREEN = 1,
            YELLOW = 2,
            BLUE = 3,
        }

        public string[] PlayerNames;

        public static float[][] PlayerColors = new float[4][];
        public bool GameIsPaused;
        public bool IsMinionSwitchMenuOpen;
        public Levels CurrentLevel;
        public Team currentTeam;
        public Team SelectedMinionTarget;

        public bool AutoAttack;

        [Range(0, 10)] public int NumberOfPages;

        // R  G
        //
        // B  Y
        void Awake()
        {
            if(Instance == null) {
                Instance = this;

                CurrentLevel  = (Levels)1;
                GameIsPaused = false;
                IsMinionSwitchMenuOpen = true;
                NumberOfPages = 10;

                PlayerColors[0] = new float[] {0.7169812F, 0.131221F, 0.131221F, 1}; //Red
                PlayerColors[1] = new float[] {0.05490196F, 0.4823529F, 0.06666667F, 1}; //Green
                PlayerColors[2] = new float[] {0.972549F, 0.9215686F, 0.05882353F, 1}; //Yellow
                PlayerColors[3] = new float[] {0.1294118F, 0.1921569F, 0.7176471F, 1}; //Blue

                PlayerNames = new string[4];
                SetCurrentPlayer(3);
                SetPlayers("Redboy", "Greendude", "Yellowman", "Blueguy");
            
                AutoAttack = true;
                
            }
        }

        void SetPlayers(string NameRed, string NameGreen, string NameYellow, string NameBlue) {
            PlayerNames[0] = NameRed;
            PlayerNames[1] = NameGreen;
            PlayerNames[2] = NameYellow;
            PlayerNames[3] = NameBlue;
        }

        void SetCurrentPlayer(int player) {
            currentTeam = (Team)player;
        }
    }
}
