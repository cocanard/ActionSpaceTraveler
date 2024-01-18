using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Components;

public class NotificationScript : MonoBehaviour
{
    [SerializeField] GameObject BaseMessage;

    static GameObject NotificationHub;
    static GameObject BaseNotification;
    static Dictionary<int, GameObject> NotificationList = new();
    static int count = 0;
    // Start is called before the first frame update
    void Awake()
    {
        NotificationHub = gameObject;
        BaseNotification = BaseMessage;
    }

    static public int AddNotification(string text, uint time = 3)
    {
        if (!DatasScript.settings.notifications) return 0;
        count += 1;
        GameObject clone = GameObject.Instantiate(BaseNotification, NotificationHub.transform);
        clone.SetActive(true);
        clone.GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = text;
        clone.GetComponent<LocalizeStringEvent>().RefreshString();
        NotificationList.Add(count, clone);
        if(time != 0)
        {
            NotificationHub.GetComponent<NotificationScript>().SetTimer(time);
        }

        return count;
    }

    void SetTimer(uint time)
    {
        StartCoroutine(Timer(count, time));
    }

    static IEnumerator Timer(int index, uint time)
    {
        yield return new WaitForSeconds(time);
        RemoveNotification(index);
    }

    static public void RemoveNotification(int index)
    {
        NotificationList[index].transform.LeanMove(NotificationList[index].transform.position + new Vector3(-300, 0), 0.75f);
        Object.Destroy(NotificationList[index], 0.75f);
        NotificationList.Remove(index);
    }
}
