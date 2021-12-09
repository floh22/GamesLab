using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject PauseMenuUI;
    public GameObject IngameUI;

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(GameData.Instance.CurrentLevel);
        if(GameData.Instance.CurrentLevel == GameData.Levels.MAP) {
            // if(Input.GetKeyDown(KeyCode.Escape)) {
            //     TogglePauseMenu();
            // }
        }
    }

    public void TogglePauseMenu() {
        if(GameData.Instance.GameIsPaused) {
            PauseMenuUI.SetActive(false);
            IngameUI.SetActive(true);
            GameData.Instance.GameIsPaused = false;
        } else {
            PauseMenuUI.SetActive(true);
            IngameUI.SetActive(false);
            GameData.Instance.GameIsPaused = true;
        }
    }
}
