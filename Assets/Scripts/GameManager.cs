using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] Transform Camera;
    [SerializeField] GameObject PauseMenu;
    [SerializeField] GameObject UI;

    public static int worldindex = 1;

    // Start is called before the first frame update
    void Awake()
    {
        Camera.Find("Canvas/Image").GetComponent<Image>().sprite = Resources.Load<Sprite>("Background/" + worldindex);
        DatasScript.checkFolder();
        DatasScript.UpdateSettingsInfo();
        GameObject spaceship =  Object.Instantiate(Resources.Load<GameObject>(DatasScript.save.get_current_ship().obj), new Vector3(0, 0, 0), Quaternion.Euler(0, 0, -90));
        SpaceShipPlayer player = spaceship.GetComponent<SpaceShipPlayer>();
        player.UI = UI;
        player.PauseMenu = PauseMenu;
        player.Camera = Camera;
        player.enabled = true;

        StartCoroutine(foo());
    }

    IEnumerator foo()
    {
        yield return new WaitForEndOfFrame();
        StartCoroutine(StorageManager.Storage.GetComponent<StorageManager>().create_enemyship(3));
    }
}
