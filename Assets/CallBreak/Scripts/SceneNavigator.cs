using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public void GoToGameScene()
    {
        SceneManager.LoadScene("Game");
    }
    public void GoToHomeScene()
    {
        SceneManager.LoadScene("Home");
    }

    public void PlayAgain()
    {
        int totalRound = GameManager.instance.totalRounds;
        SceneManager.LoadScene("Game");
        GameManager.instance.totalRounds = totalRound;
    }

    public void SelectRound()
    {
        if (GameDataManager.Instance.Selected_Round <= 0) return;
        GoToGameScene();
    }
}
