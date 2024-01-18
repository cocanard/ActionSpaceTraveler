using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ShipUI : MonoBehaviour
{
    GameObject first_selected;
    private Spaceship instance;

    public void ChangeSpaceShip(Spaceship new_ship)
    {
        if(DatasScript.save.spaceships.Contains(new_ship))
        {
            instance = new_ship;
            transform.Find("Stats/Health/Bar/Current").GetComponent<RectTransform>().sizeDelta = new Vector2(-600, 0);
            transform.Find("Stats/Damage/Bar/Current").GetComponent<RectTransform>().sizeDelta = new Vector2(-600, 0);
            transform.Find("Stats/Speed/Bar/Current").GetComponent<RectTransform>().sizeDelta = new Vector2(-600, 0);
            transform.Find("Stats/Health/Bar/UpgradeEffect").GetComponent<RectTransform>().sizeDelta = new Vector2(-600, 0);
            transform.Find("Stats/Damage/Bar/UpgradeEffect").GetComponent<RectTransform>().sizeDelta = new Vector2(-600, 0);
            transform.Find("Stats/Speed/Bar/UpgradeEffect").GetComponent<RectTransform>().sizeDelta = new Vector2(-600, 0);

            Refresh();
        }
    }
    private bool settingWeapon = false;

    void SetWeapons()
    {
        settingWeapon = true;
        Transform Weapons = transform.Find("Weapons");
        List<Weapon> MainWeapon = DatasScript.save.Weapons.Where(w => w.possessed && (instance.weapons.second == -1 ? true : w != DatasScript.save.Weapons[instance.weapons.second])).ToList();
        Weapons.Find("MainDropdown").GetComponent<TMP_Dropdown>().options = MainWeapon.Select(w => new TMP_Dropdown.OptionData() { text = w.name }).ToList();
        Weapons.Find("MainDropdown").GetComponent<TMP_Dropdown>().value = MainWeapon.IndexOf(DatasScript.save.Weapons[instance.weapons.main]);
        Weapons.Find("MainDropdown").GetComponent<TMP_Dropdown>().interactable = instance.possessed;

        List<Weapon> SecondWeapon = DatasScript.save.Weapons.Where(w => w.possessed && DatasScript.save.Weapons[instance.weapons.main] != w).ToList();
        Weapons.Find("SecondDropdown").GetComponent<TMP_Dropdown>().options = SecondWeapon.Select(w => new TMP_Dropdown.OptionData() { text = w.name }).ToList();
        Weapons.Find("SecondDropdown").GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData() { text = "-" });
        Weapons.Find("SecondDropdown").GetComponent<TMP_Dropdown>().value = instance.weapons.second != -1 ? SecondWeapon.IndexOf(DatasScript.save.Weapons[instance.weapons.second]) : Weapons.Find("SecondDropdown").GetComponent<TMP_Dropdown>().options.Count();
        Weapons.Find("SecondDropdown").GetComponent<TMP_Dropdown>().interactable = instance.possessed;
        settingWeapon = false;
    }

    public void Refresh()
    {
        //Infos part
        Transform infos = transform.Find("Infos");
        infos.Find("Name").GetComponent<TMP_Text>().text = instance.name;
        infos.Find("ShipImage").GetComponent<Image>().sprite = Resources.Load<GameObject>(instance.obj).GetComponent<SpriteRenderer>().sprite;
        infos.Find("Possessed").GetComponent<Toggle>().isOn = instance.possessed;

        //Stats part
        float t = 1;
        Transform Stats = transform.Find("Stats");
        Stats.Find("Health/CurrentValue").GetComponent<LocalizeStringEvent>().StringReference["amount"] = new IntVariable() { Value = instance.health };
        Stats.Find("Health/CurrentValue").GetComponent<LocalizeStringEvent>().RefreshString();
        Stats.Find("Health/UpgradeValue").GetComponent<TMP_Text>().text = "+" + instance.level_rate.health;
        Stats.Find("Health/UpgradeValue").gameObject.SetActive(instance.possessed && instance.level != instance.maxlevel);
        Stats.Find("Health/Bar/UpgradeEffect").gameObject.SetActive(instance.possessed && instance.level != instance.maxlevel);
        Stats.Find("Health/Bar/Current").GetComponent<RectTransform>().LeanScale(-new Vector2((1 - (float)instance.health / 300) * 600, 1), t).setCanvasSizeDelta(); //scale the health from 0 to 300
        Stats.Find("Health/Bar/UpgradeEffect").GetComponent<RectTransform>().LeanScale(-new Vector2((1 - (float)(instance.health + instance.level_rate.health) / 300) * 600, 1), t).setCanvasSizeDelta(); // Same as current but add level_rate

        Stats.Find("Damage/CurrentValue").GetComponent<LocalizeStringEvent>().StringReference["amount"] = new FloatVariable() { Value = instance.damage };
        Stats.Find("Damage/CurrentValue").GetComponent<LocalizeStringEvent>().RefreshString();
        Stats.Find("Damage/UpgradeValue").GetComponent<TMP_Text>().text = "+" + instance.level_rate.damage;
        Stats.Find("Damage/UpgradeValue").gameObject.SetActive(instance.possessed && instance.level != instance.maxlevel);
        Stats.Find("Damage/Bar/UpgradeEffect").gameObject.SetActive(instance.possessed && instance.level != instance.maxlevel);
        Stats.Find("Damage/Bar/Current").GetComponent<RectTransform>().LeanScale(-new Vector2((1 - (float)instance.damage / 3.5f) * 600, 1), t).setCanvasSizeDelta(); //scale the damage from 0 to 3.5
        Stats.Find("Damage/Bar/UpgradeEffect").GetComponent<RectTransform>().LeanScale(-new Vector2((1 - (float)(instance.damage + instance.level_rate.damage) / 3.5f) * 600, 1), t).setCanvasSizeDelta(); // Same as current but add level_rate

        Stats.Find("Speed/CurrentValue").GetComponent<LocalizeStringEvent>().StringReference["amount"] = new FloatVariable() { Value = instance.SceneSpeed };
        Stats.Find("Speed/CurrentValue").GetComponent<LocalizeStringEvent>().RefreshString();
        Stats.Find("Speed/UpgradeValue").GetComponent<TMP_Text>().text = "+" + instance.level_rate.speed;
        Stats.Find("Speed/UpgradeValue").gameObject.SetActive(instance.possessed && instance.level != instance.maxlevel);
        Stats.Find("Speed/Bar/UpgradeEffect").gameObject.SetActive(instance.possessed && instance.level != instance.maxlevel);
        Stats.Find("Speed/Bar/Current").GetComponent<RectTransform>().LeanScale(-new Vector2((1 - (float)instance.SceneSpeed / 3) * 600, 1), t).setCanvasSizeDelta(); //scale the speed from 0 to 3
        Stats.Find("Speed/Bar/UpgradeEffect").GetComponent<RectTransform>().LeanScale(-new Vector2((1 - (float)(instance.SceneSpeed + instance.level_rate.speed) / 3) * 600, 1), t).setCanvasSizeDelta(); // Same as current but add level_rate

        //Weapons part
        SetWeapons();

        //Buy part
        Transform BuyP = transform.Find("Buy");
        BuyP.Find("Message").GetComponent<LocalizeStringEvent>().StringReference["amount"] = new IntVariable() { Value = instance.possessed ? (int)(instance.cost * Mathf.Pow(2, instance.level)) : instance.cost * 10 };
        BuyP.Find("Message").GetComponent<LocalizeStringEvent>().StringReference["ship_name"] = new StringVariable() { Value = instance.name };
        BuyP.Find("Message").GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = instance.possessed ? instance.maxlevel == instance.level ? "Maxlevel" : "UpgradeMessage" : "BuyMessage";
        BuyP.Find("Message").GetComponent<LocalizeStringEvent>().RefreshString();
        BuyP.Find("BuyButton").GetComponent<Button>().interactable = (instance.possessed && instance.maxlevel != instance.level && DatasScript.save.money >= instance.cost * Mathf.Pow(2, instance.level)) || (!instance.possessed && DatasScript.save.money >= instance.cost * 10);
        if (GameObject.Find("EventSystem").GetComponent<EventSystem>().firstSelectedGameObject != transform.Find("Buy").gameObject)
        {
            first_selected = GameObject.Find("EventSystem").GetComponent<EventSystem>().firstSelectedGameObject;
            GameObject.Find("EventSystem").GetComponent<EventSystem>().firstSelectedGameObject = transform.Find("Buy/BuyButton").gameObject;
            GameObject.Find("EventSystem").GetComponent<EventSystem>().SetSelectedGameObject(transform.Find("Buy/BuyButton").gameObject);
        }
    }

    public void Buy()
    {
        if(instance.possessed)
        {
            instance.level += 1;
        }
        else
        {
            instance.Buy();
        }
        
        Refresh();
    }

    public void ChangeWeapon(Transform inst)
    {
        if(settingWeapon)
        {
            return;
        }
        if(inst.name == "MainDropdown" || inst.name == "SecondDropdown")
        {
            IEnumerable<Weapon> l = DatasScript.save.Weapons.Where(x => x.name == inst.GetComponent<TMP_Dropdown>().options[inst.GetComponent<TMP_Dropdown>().value].text && x.possessed);
            Weapon selected = l.Any() ? l.First() : null;
            if((selected == null && inst.name == "SecondDropdown") || (selected != null && instance.weapons.main != DatasScript.save.Weapons.IndexOf(selected) && instance.weapons.second != DatasScript.save.Weapons.IndexOf(selected)))
            {
                instance.weapons = inst.name == "MainDropdown" ? (DatasScript.save.Weapons.IndexOf(selected), instance.weapons.second) : (instance.weapons.main, selected != null ? DatasScript.save.Weapons.IndexOf(selected) : -1);
            }
        }
        SetWeapons();
    }

    public void Remove_Window()
    {
        GameObject.Find("EventSystem").GetComponent<EventSystem>().firstSelectedGameObject = first_selected;
        GameObject.Find("EventSystem").GetComponent<EventSystem>().SetSelectedGameObject(first_selected);
        first_selected = null;
        transform.LeanMove(new Vector2(UIManagement.UI.GetComponent<RectTransform>().sizeDelta.x + 500, 0), 1).setMoveLocal();
        transform.parent.Find("MarketPanel").LeanMove(Vector2.zero, 1).setMoveLocal();
        UIManagement.UI.GetComponent<UIManagement>().SendMessage("updateStore");
    }
}
