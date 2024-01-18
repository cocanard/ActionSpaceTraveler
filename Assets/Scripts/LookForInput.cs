using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class LookForInput : MonoBehaviour
{
    [SerializeField] private string InputString;
    List<string> InputList = new List<string>();
    short CurrentInput = 0;
    bool holding = false;
    float t = 0.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        foreach (char character in InputString)
        {
            InputList.Add(Convert.ToString(character));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!(InputList.Count > CurrentInput))
        {
            this.gameObject.GetComponent<TMP_Text>().enabled = true;
            CurrentInput = 0;
            Application.Quit();
        }
        else if(!holding)
        {
            if(Input.GetKey(InputList[CurrentInput]))
            {
                if(CurrentInput+1 < InputList.Count && InputList[CurrentInput] == InputList[CurrentInput+1])
                {
                    holding = true;
                }
                CurrentInput++;
                t = 0.0f;
            }
        }
        else if(!Input.GetKey(InputList[CurrentInput]))
        {
            holding = false;
        }

        if(CurrentInput != 0)
        {
            if(t > 5.0)
            {
                holding = false;
                CurrentInput = 0;
                t = 0.0f;
            }
            else
            {
                t += Time.deltaTime;
            }
        }
    }
}
