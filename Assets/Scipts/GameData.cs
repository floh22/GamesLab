using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance {get; private set;}

    public enum Levels {
        START = 0,
        MAP = 1,
        GAME_END = 2,
    }

    public bool GameIsPaused;
    public Levels CurrentLevel;

    void Awake()
    {
        if(Instance == null) {
            Instance = this;

            this.CurrentLevel  = (Levels)1;
            this.GameIsPaused = false;

            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
}
