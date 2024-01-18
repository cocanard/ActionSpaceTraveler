using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateEvent : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] UIManagement script;
    [SerializeField] string message;

    private void OnEnable()
    {
        script.SendMessage(message);
    }
    private void OnDisable()
    {
        script.SendMessage(message);
        script.SendMessage("applySettings");
    }
}
