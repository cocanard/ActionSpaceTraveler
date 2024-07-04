using UnityEngine;
using System.Collections;
using Discord;
using UnityEngine.SceneManagement;

public class DiscordActivity : MonoBehaviour
{
    public static Discord.Discord connection { get; private set; }
    Discord.Activity activity;
    bool connecting;
    // Use this for initialization
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(TryConnect());
    }

    IEnumerator TryConnect()
    {
        connecting = true;
        while(System.Diagnostics.Process.GetProcessesByName("Discord").Length == 0)
        {
            yield return new WaitForSeconds(5);
        }
        try
        {
            connection = new Discord.Discord(0, (uint)Discord.CreateFlags.NoRequireDiscord);
            var ActivityManager = connection.GetActivityManager();
            activity = new Discord.Activity
            {
                State = "Hey",
                Details = "Hello there",
                Assets =
                {
                    LargeImage = "icon",
                    SmallImage = "cocaland",
                }
            };
            ActivityManager.UpdateActivity(activity, (res) => { });
        }
        catch { }
        connecting = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(connection is not null)
        {
            activity.Details = SceneManager.GetActiveScene().name == "GameMenu" ? "In menu" : $"Playing level {GameManager.worldindex}";
            activity.State = SceneManager.GetActiveScene().name == "GameMenu" ? "" : StorageManager.isBoss ? "Fighting against " +StorageManager.Storage.GetComponent<StorageManager>().area_info.Boss_Object.name : $"Has a score of {(int)SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().score}pts";
            connection.GetActivityManager().UpdateActivity(activity, (res) => { });
            try
            {
                connection.RunCallbacks();
            }
            catch
            {
                connection = null;
            }
        }
        else if(!connecting)
        {
            StartCoroutine(TryConnect());
        }
    }

    private void OnDestroy()
    {
        if(connection is not null)
        {
            DiscordActivity.connection.Dispose();
        }
    }
}

