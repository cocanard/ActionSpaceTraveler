using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonScript : MonoBehaviour
{
    public GameObject player;

    // Update is called once per frame
    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Game");
    }
    public void unPause()
    {
       SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().SendMessage("Pause");
    }
    public void Menu()
    {
        SceneManager.LoadScene("GameMenu");
        Time.timeScale = 1;
    }
}
