using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UIManagement : MonoBehaviour
{
    bool listen = false;
    bool updating = false;
    KeyCode inputlistener;
    //Create a list of listened keys and filter it
    KeyCode[] keyslistened = ((KeyCode[])System.Enum.GetValues(typeof(KeyCode))).Where(w => !w.ToString().Contains("Alpha") && !w.ToString().Contains("Joystick") && (!w.ToString().Contains("F") || w.ToString().Equals("F")) && !w.ToString().Contains("Mouse")).ToArray();
    Coroutine current_wait;
    [SerializeField] GameObject SettingsPanel;
    [SerializeField] GameObject MarketPanel;
    [SerializeField] GameObject PlayPanel;
    GameObject ShipViewModel;
    public static GameObject UI;

    private void Awake()
    {
        UI = gameObject;
    }
    void Start()
    {
        ShipViewModel = Resources.Load<GameObject>("ShipContent");
        DatasScript.checkFolder();
    }

    GameObject last_selected;
    private void Update()
    {
        GameObject obj = GameObject.Find("EventSystem").GetComponent<EventSystem>().currentSelectedGameObject;
        if (obj == null) return;

        ScrollRect rect = null;

        try
        {
            rect = obj.GetComponentInParent<ScrollRect>();
        }
        catch
        {
            Debug.LogWarning("Error when trying to get a rect ");
        }

        if (rect != null && Input.GetJoystickNames().Length > 0 && last_selected != null && obj != last_selected)
        {
            float yposition = -obj.transform.GetComponentsInParent<RectTransform>().Where(w => w.parent == rect.content).First().localPosition.y;
            yposition = yposition >= 0 ? yposition <= rect.content.GetComponent<RectTransform>().sizeDelta.y - rect.viewport.GetComponent<RectTransform>().rect.height ? yposition : rect.content.GetComponent<RectTransform>().sizeDelta.y - rect.viewport.GetComponent<RectTransform>().rect.height : 0;
            Debug.Log(yposition);
            rect.content.GetComponent<RectTransform>().localPosition += new Vector3(0, -rect.content.GetComponent<RectTransform>().localPosition.y + yposition, 0);
        }

        if (listen)
        {
            if(Input.GetKey(KeyCode.Escape) || (Input.GetJoystickNames().Any() && Gamepad.current.buttonEast.IsPressed()))
            {
                listen = false;
                inputlistener = KeyCode.None;
            }
            else
            {
                foreach (KeyCode key in keyslistened)
                {
                    if (Input.GetKey(key) && !DatasScript.settings.PlacementControls.Values.Contains(key) && !DatasScript.settings.Keys.Values.Contains(key))
                    {
                        Debug.Log(key);

                        inputlistener = key;
                        listen = false;
                        break;
                    }
                }
            }
         }
    }

    //Main button
    public void OnPlayClick(int index)
    {
        if(Time.timeSinceLevelLoad > 0.1f)
        {
            GameManager.worldindex = index;
            DatasScript.save.Save();
            DatasScript.UpdateSettingsToJson();
            SceneManager.LoadScene("Game");
        }
    }

    void applySettings()
    {
        DatasScript.UpdateSettingsToJson();
    }

    public void redirecttodiscord()
    {
        Application.OpenURL("https://discord.com/invite/nYs66CMpSg");
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void UpdateGameMenu()
    {
        PlayPanel.transform.Find("Area1/RecordText").GetComponent<LocalizeStringEvent>().StringReference["score"] = new IntVariable() { Value = DatasScript.save.highscore[0] };
        PlayPanel.transform.Find("Area1/RecordText").GetComponent<LocalizeStringEvent>().RefreshString();
        PlayPanel.transform.Find("Area2/RecordText").GetComponent<LocalizeStringEvent>().StringReference["score"] = new IntVariable() { Value = DatasScript.save.highscore[1] };
        PlayPanel.transform.Find("Area2/RecordText").GetComponent<LocalizeStringEvent>().RefreshString();
        PlayPanel.transform.Find("Area3/RecordText").GetComponent<LocalizeStringEvent>().StringReference["score"] = new IntVariable() { Value = DatasScript.save.highscore[2] };
        PlayPanel.transform.Find("Area3/RecordText").GetComponent<LocalizeStringEvent>().RefreshString();

        PlayPanel.transform.Find("Area2/BlockText").gameObject.SetActive(!DatasScript.save.boss_defeated[0]);
        PlayPanel.transform.Find("Area2/RecordText").gameObject.SetActive(DatasScript.save.boss_defeated[0]);
        PlayPanel.transform.Find("Area2/PlayButton").gameObject.SetActive(DatasScript.save.boss_defeated[0]);

        PlayPanel.transform.Find("Area3/BlockText").gameObject.SetActive(!DatasScript.save.boss_defeated[1]);
        PlayPanel.transform.Find("Area3/RecordText").gameObject.SetActive(DatasScript.save.boss_defeated[1]);
        PlayPanel.transform.Find("Area3/PlayButton").gameObject.SetActive(DatasScript.save.boss_defeated[1]);
    }

    enum nametoinput { Up = 2, Down = 3, Right = 0, Left = 1 };

    public void listenInput(string input)
    {
        if (current_wait != null)
        {
            StopCoroutine(current_wait);
        }
        inputlistener = KeyCode.None;
        listen = true;

        System.Enum.TryParse(typeof(nametoinput), input, out object result);
        if (result != null)
        {
            SettingsPanel.transform.Find($"ControlsPage/Viewport/Content/MainKeys/{input}Frame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = "...";
            if (DatasScript.settings.PlacementControls.Keys.ToArray().Length > (int)System.Enum.Parse(typeof(nametoinput), input))
            {
                current_wait = StartCoroutine(WaitForInput(DatasScript.settings.PlacementControls.Keys.ToArray()[(int)System.Enum.Parse(typeof(nametoinput), input)], SettingsPanel.transform.Find($"ControlsPage/Viewport/Content/MainKeys/{input}Frame/Button/ButtonText").gameObject.GetComponent<TMP_Text>()));
            }
        }
        else if (DatasScript.settings.Keys.ContainsKey(input))
        {
            SettingsPanel.transform.Find($"ControlsPage/Viewport/Content/ActionKeys/{input}Frame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = "...";
            current_wait = StartCoroutine(WaitForInput(input, SettingsPanel.transform.Find($"ControlsPage/Viewport/Content/ActionKeys/{input}Frame/Button/ButtonText").gameObject.GetComponent<TMP_Text>()));
        }
    }

    IEnumerator WaitForInput((int, int) index, TMP_Text text)
    {
        while (listen)
        {
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("Stopped listening");
        if(inputlistener != KeyCode.None)
        {
            Debug.Log(inputlistener);
            DatasScript.settings.PlacementControls[index] = inputlistener;
        }

        text.text = DatasScript.settings.PlacementControls[index].ToString();
        current_wait = null;
    }

    IEnumerator WaitForInput(string key, TMP_Text text)
    {
        while (listen)
        {
            yield return new WaitForEndOfFrame();
        }
        if (inputlistener != KeyCode.None)
        {
            Debug.Log(inputlistener);
            DatasScript.settings.Keys[key] = inputlistener;
        }

        text.text = DatasScript.settings.Keys[key].ToString();
        current_wait = null;
    }

    IEnumerator WaitForInput(string index)
    {
        while (inputlistener == KeyCode.None || listen == false)
        {
            yield return new WaitForSeconds(0.05f);
        }
        current_wait = null;
    }

    void updateOptions()
    {
        updating = true;
        //resolution
        Debug.Log(DatasScript.settings.resolution);
        GameObject resdropdown = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Resolution/ResolutionChoice").gameObject;
        resdropdown.GetComponent<TMP_Dropdown>().options = Screen.resolutions.Where(w => w.refreshRate == Screen.resolutions[0].refreshRate).Select(w => new TMP_Dropdown.OptionData { text = w.width + " x " + w.height }).ToList();
        var fetching_res = resdropdown.GetComponent<TMP_Dropdown>().options.Where(w => w.text.Contains(DatasScript.settings.resolution.x.ToString() + " x " + DatasScript.settings.resolution.y.ToString()));
        int currentresolution = fetching_res.Any() ? resdropdown.GetComponent<TMP_Dropdown>().options.IndexOf(fetching_res.Last()) : resdropdown.GetComponent<TMP_Dropdown>().options.Count - 1;
        resdropdown.GetComponent<TMP_Dropdown>().value = currentresolution;
        SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Resolution/Toggle").gameObject.GetComponent<Toggle>().isOn = DatasScript.settings.fullscreen;
        SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Resolution/ApplyButton").gameObject.SetActive(false);

        //language
        GameObject landropdown = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Language/LanguageChoice").gameObject;
        landropdown.GetComponent<TMP_Dropdown>().options = LocalizationSettings.AvailableLocales.Locales.Select(w => new TMP_Dropdown.OptionData { text = w.name }).ToList();
        landropdown.GetComponent<TMP_Dropdown>().value = DatasScript.settings.language;

        //audio
        Transform audio = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Volume");
        audio.Find("Value").GetComponent<TMP_Text>().text = (Mathf.Round(DatasScript.settings.volume * 100) / 100).ToString();
        audio.Find("Slider").GetComponent<Slider>().value = DatasScript.settings.volume;

        //other
        SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Other/NotificationToggle").GetComponent<Toggle>().isOn = DatasScript.settings.notifications;
        SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Other/DiscordToggle").GetComponent<Toggle>().isOn = DatasScript.settings.DiscordPresence;

        //controls
        Transform MKF = SettingsPanel.transform.Find("ControlsPage/Viewport/Content/MainKeys"); //MKF => MainKeysFrame
        MKF.Find("UpFrame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = DatasScript.settings.PlacementControls[(1, 1)].ToString();
        MKF.Find("LeftFrame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = DatasScript.settings.PlacementControls[(0, -1)].ToString();
        MKF.Find("DownFrame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = DatasScript.settings.PlacementControls[(1, -1)].ToString();
        MKF.Find("RightFrame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = DatasScript.settings.PlacementControls[(0, 1)].ToString();

        bool Connected_Playsation = Input.GetJoystickNames().Length > 0 && Gamepad.current.name.Contains("DualShock");
        Transform AKF = SettingsPanel.transform.Find("ControlsPage/Viewport/Content/ActionKeys"); //AKF => ActionKeysFrame
        AKF.Find("FirstWeaponFrame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = DatasScript.settings.Keys["FirstWeapon"].ToString();
        AKF.Find("FirstWeaponFrame/SecondKey/DualShock").gameObject.SetActive(Connected_Playsation);
        AKF.Find("FirstWeaponFrame/SecondKey/Xbox").gameObject.SetActive(!Connected_Playsation);

        AKF.Find("SecondWeaponFrame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = DatasScript.settings.Keys["SecondWeapon"].ToString();
        AKF.Find("SecondWeaponFrame/SecondKey/DualShock").gameObject.SetActive(Connected_Playsation);
        AKF.Find("SecondWeaponFrame/SecondKey/Xbox").gameObject.SetActive(!Connected_Playsation);

        AKF.Find("PowerUpFrame/Button/ButtonText").gameObject.GetComponent<TMP_Text>().text = DatasScript.settings.Keys["PowerUp"].ToString();
        AKF.Find("PowerUpFrame/SecondKey/DualShock").gameObject.SetActive(Connected_Playsation);
        AKF.Find("PowerUpFrame/SecondKey/Xbox").gameObject.SetActive(!Connected_Playsation);
        updating = false;
    }

    int current_weapon = -1;
    public void updateStore()
    {
        MarketPanel.transform.Find("ShipsPage/MoneyAmount").GetComponent<LocalizeStringEvent>().StringReference["amount"] = new IntVariable() { Value = DatasScript.save.money };
        MarketPanel.transform.Find("ShipsPage/MoneyAmount").GetComponent<LocalizeStringEvent>().RefreshString();
        MarketPanel.transform.Find("WeaponsPage/MoneyAmount").GetComponent<LocalizeStringEvent>().StringReference["amount"] = new IntVariable() { Value = DatasScript.save.money };
        MarketPanel.transform.Find("WeaponsPage/MoneyAmount").GetComponent<LocalizeStringEvent>().RefreshString();
        foreach (Spaceship p in DatasScript.save.spaceships)
        {
            if (p.possessed)
            {
                if (MarketPanel.transform.Find("InventoryPage/Content/" + p.name) == null)
                {
                    GameObject view1 = Object.Instantiate(ShipViewModel, MarketPanel.transform.Find("InventoryPage/Content"));
                    view1.GetComponent<ShipViewScript>().SetLinkedClass(p, 1);
                }
                else
                {
                    MarketPanel.transform.Find("InventoryPage/Content/" + p.name).GetComponent<ShipViewScript>().UpdateState();
                }
            }
            if (MarketPanel.transform.Find("ShipsPage/Content/" + p.name) == null)
            {
                GameObject view2 = Object.Instantiate(ShipViewModel, MarketPanel.transform.Find("ShipsPage/Content"));
                view2.GetComponent<ShipViewScript>().SetLinkedClass(p, 0);
            }
            else
            {
                MarketPanel.transform.Find("ShipsPage/Content/" + p.name).GetComponent<ShipViewScript>().UpdateState();
            }
        }
    }

    public void ShowWeapon(int index)
    {
        if (index < DatasScript.save.Weapons.Count && index >= 0)
        {
            Weapon instance = DatasScript.save.Weapons[index];
            Transform WeaponInfo = MarketPanel.transform.Find("WeaponsPage/WeaponInfo");
            
            WeaponInfo.Find("Title").GetComponent<TMP_Text>().text = instance.name;
            WeaponInfo.Find("Description").GetComponent<LocalizeStringEvent>().StringReference.TableEntryReference = instance.name;
            WeaponInfo.Find("Description").GetComponent<LocalizeStringEvent>().RefreshString();
            WeaponInfo.Find("Cost").GetComponent<LocalizeStringEvent>().StringReference["amount"] = new IntVariable() { Value = instance.cost };
            WeaponInfo.Find("Cost").GetComponent<LocalizeStringEvent>().RefreshString();
            WeaponInfo.Find("Cost").gameObject.SetActive(!instance.possessed);

            WeaponInfo.Find("BuyButton").GetComponent<Button>().interactable = !instance.possessed && DatasScript.save.money >= instance.cost;
            WeaponInfo.gameObject.SetActive(true);
            current_weapon = index;
        }
    }

    public void BuyWeapon()
    {
        if (current_weapon < DatasScript.save.Weapons.Count && current_weapon >= 0 && !DatasScript.save.Weapons[current_weapon].possessed && DatasScript.save.money >= DatasScript.save.Weapons[current_weapon].cost)
        {
            DatasScript.save.Weapons[current_weapon].Buy();
            ShowWeapon(current_weapon);
        }
    }

    public void SetAudio()
    {
        if(!updating)
        {
            float value = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Volume/Slider").GetComponent<Slider>().value;
            DatasScript.settings.volume = value;
            SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Volume/Value").GetComponent<TMP_Text>().text = (Mathf.Round(value * 100) / 100).ToString();
            AudioListener.volume = value;
        }
    }

    public void SetBoolValues()
    {
        DatasScript.settings.notifications = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Other/NotificationToggle").GetComponent<Toggle>().isOn;
        DatasScript.settings.DiscordPresence = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Other/DiscordToggle").GetComponent<Toggle>().isOn;
        DatasScript.UpdateDiscordPresence();
    }

    public void SetLanguage()
    {
        if(!updating)
        {
            int index = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Language/LanguageChoice").gameObject.GetComponent<TMP_Dropdown>().value;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
            DatasScript.settings.language = index;
        }
    }
    

    public void setResolution()
    {
        if(!updating)
        {
            Resolution res = Screen.resolutions.Where(w => w.refreshRate == Screen.resolutions[0].refreshRate).ToList()[SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Resolution/ResolutionChoice").gameObject.GetComponent<TMP_Dropdown>().value];
            Screen.SetResolution(res.width, res.height, SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Resolution/Toggle").gameObject.GetComponent<Toggle>().isOn);

            DatasScript.settings.resolution = ((uint)res.width, (uint)res.height);
            DatasScript.settings.fullscreen = SettingsPanel.transform.Find("GeneralPage/Viewport/Content/Resolution/Toggle").gameObject.GetComponent<Toggle>().isOn;
        }
    }
}