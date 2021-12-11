using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance {get; private set;}

    public enum Levels {
        START = 0,
        MAP = 1,
        GAME_END = 2,
    }

    public enum Player {
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
    public Player CurrentPlayer;
    public Player SelectedMinionTarget;

    // R  G
    //
    // B  Y
    void Awake()
    {
        if(Instance == null) {
            Instance = this;

            this.CurrentLevel  = (Levels)1;
            this.GameIsPaused = false;
            this.IsMinionSwitchMenuOpen = true;

            PlayerColors[0] = new float[] {0.7169812F, 0.131221F, 0.131221F, 1};
            PlayerColors[1] = new float[] {0.05490196F, 0.4823529F, 0.06666667F, 1};
            PlayerColors[2] = new float[] {0.972549F, 0.9215686F, 0.05882353F, 1};
            PlayerColors[3] = new float[] {0.1294118F, 0.1921569F, 0.7176471F, 1};

            PlayerNames = new string[4];

            SetCurrentPlayer(3);
            SetPlayers("Redboy", "Greendude", "Yellowman", "Blueguy");
            

            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void SetPlayers(string NameRed, string NameGreen, string NameYellow, string NameBlue) {
        PlayerNames[0] = NameRed;
        PlayerNames[1] = NameGreen;
        PlayerNames[2] = NameYellow;
        PlayerNames[3] = NameBlue;
    }

    void SetCurrentPlayer(int player) {
        CurrentPlayer = (Player)player;
    }
}
