using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.EventSystems;

public class ShipViewScript : MonoBehaviour
{
    [SerializeField] GameObject Image;
    [SerializeField] GameObject Button;
    [SerializeField] GameObject Title;
    [SerializeField] GameObject Message;
    ushort action;
    Spaceship instance;

    public void SetLinkedClass(Spaceship inst, ushort act)
    {
        gameObject.name = inst.name;
        instance = inst;
        action = act;
        Title.GetComponent<TMP_Text>().text = instance.name;
        Image.GetComponent<Image>().sprite = Resources.Load<GameObject>(instance.obj).GetComponent<SpriteRenderer>().sprite;

        UpdateState();
    }

    public void UpdateState()
    {

        switch (action)
        {
            case 0: // when it is in shop view
                if (!instance.possessed)
                {
                    Button.transform.Find("Text").GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = "Buy";
                }
                else if (instance.level == instance.maxlevel)
                {
                    Button.transform.Find("Text").GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = "More";
                }
                else
                {
                    Button.transform.Find("Text").GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = "Upgrade";
                }
                Message.GetComponent<LocalizeStringEvent>().RefreshString();

                break;

            case 1: //when it is an inventory view
                if (DatasScript.save.spaceships[DatasScript.save.currentship] == instance)
                {
                    Message.GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = "Selected";
                    Button.SetActive(false);
                    Message.SetActive(true);
                }
                else
                {
                    Button.transform.Find("Text").GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = "Select";
                    Button.SetActive(true);
                    Message.SetActive(false);
                }
                Message.GetComponent<LocalizeStringEvent>().RefreshString();
                Button.transform.Find("Text").GetComponent<LocalizeStringEvent>().RefreshString();

                break;
        }
    }

    public void ButtonClick()
    {
        if(action == 0)
        {
            transform.parent.parent.parent.LeanMove(new Vector2(-UIManagement.UI.GetComponent<RectTransform>().sizeDelta.x - 500, 0), 1).setMoveLocal();
            UIManagement.UI.transform.Find("ShipPanel").gameObject.SetActive(true);
            UIManagement.UI.transform.Find("ShipPanel").LeanMove(new Vector2(0, 0), 1).setMoveLocal();
            UIManagement.UI.transform.Find("ShipPanel").GetComponent<ShipUI>().ChangeSpaceShip(instance);
        }
        else if(action == 1)
        {
            DatasScript.save.currentship = DatasScript.save.spaceships.IndexOf(instance);
            UIManagement.UI.GetComponent<UIManagement>().SendMessage("updateStore");
            GameObject.Find("EventSystem").GetComponent<EventSystem>().SetSelectedGameObject(UIManagement.UI.transform.Find("MarketPanel/SubPage/InventoryButton").gameObject);
        }
    }
}
