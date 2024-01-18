using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Reflection;
using System.Linq;

[RequireComponent(typeof(ParticleSystem))]
public class SpaceShipPlayer : MonoBehaviour
{
    public static GameObject plr;

    public uint health { get; private set; } = 100;
    public uint maxhealth { get; private set; } = 100;
    float energy = 100;
    uint maxenergy = 100;
    bool canrefill = true;
    Coroutine currentcoroutine;
    WeaponInfo currently_used;
    bool disabled;

    public Transform Camera;
    public static uint Speed { get; private set; }
    Rigidbody2D rb2D;
    bool Playing = true;

    bool holdingescape = false;
    bool holdingFire = false;
    bool blocked = false;

    public GameObject UI;
    public GameObject PauseMenu;
    Spaceship ship;
    PowerUp _Power;
    public PowerUp Power
    {
        get
        {
            return _Power;
        }
        set
        {
            UI.transform.Find("PowerUpPanel").gameObject.SetActive(value != null);
            if (value != null)
            {
                UI.transform.Find("PowerUpPanel/Text").GetComponent<TMPro.TMP_Text>().text = value.GetType().ToString();
                UI.transform.Find("PowerUpPanel/Image").GetComponent<Image>().sprite = Resources.Load<Sprite>($"PowerUpIcons/{value.GetType().ToString()}_icon");
            }
            _Power = value;
        }
    }
    AudioSource sound;
    float BasePos;
    public float score { get; private set; } = 0;

    //gameObject ScoreUI = gameObject;
    Dictionary<KeyCode, (int, int)> keys = new Dictionary<KeyCode, (int axis, int value)> (){
        {KeyCode.UpArrow, (1, 1)},
        {KeyCode.DownArrow, (1, -1)},
        {KeyCode.RightArrow, (0, 1)},
        {KeyCode.LeftArrow, (0, -1)},
    };
    (WeaponInfo main, WeaponInfo second) attacks;

    static System.Type[] availables_powerups = Assembly.GetExecutingAssembly().GetTypes().Where(w => w.GetInterface("PowerUp") != null).ToArray();
    // Start is called before the first frame update
    void Start()
    {
        if(Input.GetJoystickNames().Length > 0)
        {
            holdingFire = true;
        }
        //setting ship datas from datasscript
        ship = DatasScript.save.get_current_ship();
        health = (uint)ship.health;
        maxhealth = health;

        plr = gameObject;
        Camera.position = new Vector3(transform.position.x, 0, -20.0f);
        rb2D = GetComponent<Rigidbody2D>();
        sound = Camera.Find("PlaneSound").GetComponent<AudioSource>();
        BasePos = transform.position.x;

        attacks = (DatasScript.save.Weapons[DatasScript.save.get_current_ship().weapons.main].AddWeaponToShip(), DatasScript.save.get_current_ship().weapons.second == -1 ? null : DatasScript.save.Weapons[DatasScript.save.get_current_ship().weapons.second].AddWeaponToShip());
        Debug.Log(attacks.second);

        keys.Add(DatasScript.settings.PlacementControls[(0, 1 )], (0, 1));
        keys.Add(DatasScript.settings.PlacementControls[(0, -1)], (0, -1));
        keys.Add(DatasScript.settings.PlacementControls[(1, 1)], (1, 1));
        keys.Add(DatasScript.settings.PlacementControls[(1, -1)], (1, -1));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("escape") || (Input.GetJoystickNames().Length > 0 && Gamepad.current.startButton.IsPressed()))
        {
            holdingescape = true;
        }
        else if(holdingescape || (!Application.isFocused && Playing))
        {
            holdingescape = false;
            if(health > 0)
            {
                Pause();
            }
        }

        if (Playing && health != 0)
        {
            uint new_Speed = 50 + (uint)Mathf.Floor(score / 100);
            if (new_Speed != Speed && Speed < 150)
            {
                Speed = new_Speed;
                StorageManager.Storage.GetComponent<StorageManager>().Update_speed();
            }
            if(!disabled)
            {
                if (!holdingFire && energy > 0 && !blocked)
                {
                    bool isAction1 = (Input.GetKey(DatasScript.settings.Keys["FirstWeapon"]) || (Input.GetJoystickNames().Length > 0 && Gamepad.current.buttonSouth.isPressed)) && attacks.main.work;
                    bool isAction2 = (Input.GetKey(DatasScript.settings.Keys["SecondWeapon"]) || (Input.GetJoystickNames().Length > 0 && Gamepad.current.buttonEast.isPressed)) && attacks.second != null && attacks.second.work;
                    if (isAction1 || isAction2)
                    {
                        holdingFire = true;
                        if (currentcoroutine != null)
                        {
                            StopCoroutine(currentcoroutine);
                            if(currently_used is not null)
                            {
                                StartCoroutine(currently_used.WaitCooldown());
                            }
                        }
                        currentcoroutine = StartCoroutine(Fire(isAction1 ? attacks.main : attacks.second));
                    }
                }
                else if (holdingFire && !((Input.GetKey(DatasScript.settings.Keys["FirstWeapon"]) || (Input.GetJoystickNames().Length > 0 && Gamepad.current.buttonSouth.isPressed)) || ((Input.GetKey(DatasScript.settings.Keys["SecondWeapon"]) || (Input.GetJoystickNames().Length > 0 && Gamepad.current.buttonEast.isPressed)) && attacks.second != null)) && !blocked)
                {
                    holdingFire = false;
                }

                if ((Input.GetKey(DatasScript.settings.Keys["PowerUp"]) || (Input.GetJoystickNames().Length > 0 && Gamepad.current.buttonWest.isPressed)) && Power != null)
                {
                    if (Power.Action())
                    {
                        Power = null;
                    }
                }

                if (canrefill && energy < maxenergy)
                {
                    energy = energy + 20 * Time.deltaTime >= maxenergy ? maxenergy : energy + 20 * Time.deltaTime;
                    UI.transform.Find("EnergyBar/Energy").gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(280f * ((float)energy / maxenergy), 0.85f);
                }

                float[] input = new float[2] { 0, 0 };
                bool using_controller = false;
                foreach (KeyValuePair<KeyCode, (int axis, int value)> key in keys)
                {
                    if (Input.GetKey(key.Key))
                    {
                        input[key.Value.axis] += key.Value.value;
                    }
                }
                if (Input.GetJoystickNames().Length > 0 && (Input.GetJoystickNames().Length > 0 && Gamepad.current.leftStick.value != Vector2.zero))
                {
                    using_controller = true;
                    input = new float[2] { Gamepad.current.leftStick.value.x, Gamepad.current.leftStick.value.y };
                }

                if (input[0] != 0 || input[1] != 0)
                {
                    if (using_controller)
                    {
                        Moove(input[0], input[1]);
                    }
                    else
                    {
                        Moove(input[0] != 0 ? input[0] / Mathf.Abs(input[0]) : 0, input[1] != 0 ? input[1] / Mathf.Abs(input[1]) : 0);
                    }
                }
                else if (rb2D.velocity.x != 0 || rb2D.velocity.y != 0)
                {
                    rb2D.velocity = Vector3.zero;
                }
            }
            else
            {
                if (rb2D.velocity.x != 0 || rb2D.velocity.y != 0)
                {
                    rb2D.velocity = Vector3.zero;
                }
            }

            score += SpaceShipPlayer.Speed * Time.deltaTime / 10;
            UI.transform.Find("ScoreText").GetComponent<TMP_Text>().text = "Score :" + Mathf.Floor(score).ToString();
        }
    }

    void effects()
    {
        if((float)health/maxhealth <= 0.5f)
        {
            //Create fire effect below half of maxhealth
            if (!gameObject.GetComponent<ParticleSystem>().isPlaying)
            {
                gameObject.GetComponent<ParticleSystem>().Play(false);
            }
            var em = gameObject.GetComponent<ParticleSystem>().emission;
            em.rateOverTime = 1 + (0.5f - (float)health / maxhealth) * 15;
        }
        else if(gameObject.GetComponent<ParticleSystem>().isPlaying)
        {
            gameObject.GetComponent<ParticleSystem>().Stop();
        }
    }

    public IEnumerator StunPlayer(float time)
    {
        disabled = true;
        if(holdingFire)
        {
            StopCoroutine(currentcoroutine);
            if(currently_used is not null)
            {
                StartCoroutine(currently_used.WaitCooldown());
            }
            UI.transform.Find("EnergyBar/Energy").gameObject.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 255, 255);

            canrefill = true;
            blocked = false;
        }
        transform.Find("StunEffect").GetComponent<AudioSource>().Play();
        transform.Find("StunEffect").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(time);
        transform.Find("StunEffect").GetComponent<AudioSource>().Stop();
        transform.Find("StunEffect").GetComponent<ParticleSystem>().Stop();
        holdingFire = false;
        disabled = false;
    }

    void Moove(float directionx, float directiony)
    {
        rb2D.velocity = (Vector2.up * directiony + Vector2.right * directionx) * 50 * ship.SceneSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collided)
    {
        if (collided.gameObject.CompareTag("Projectile"))
        {
            GameObject.Destroy(collided.gameObject);
            Damage(collided.gameObject.GetComponent<ProjectileContainer>().Damage);
        }
    }

    private void OnTriggerEnter2D(Collider2D collided)
    {
        if (collided.gameObject.CompareTag("Projectile"))
        {
            GameObject.Destroy(collided.gameObject);
            Damage(collided.gameObject.GetComponent<ProjectileContainer>().Damage);
        }
    }

    public void Damage(uint damage)
    {
        if(health == 0)
        {
            return;
        }
        if(health > damage)
        {
            health -= damage;
            effects();
            StartCoroutine(onDamage());
            transform.Find("DamageSound").GetComponent<AudioSource>().Play();
        }
        else
        {
            health = 0;
            StartCoroutine(Destroy());
        }
        UI.transform.Find("HealthBar/Health").GetComponent<RectTransform>().sizeDelta = new Vector2(280f * ((float)health /maxhealth), 0.85f);
    }

    IEnumerator Destroy()
    {
        bool highscore = false;
        if(score > DatasScript.save.highscore[GameManager.worldindex - 1])
        {
            DatasScript.save.highscore[GameManager.worldindex - 1] = (int)score;
            highscore = true;
        }
        DatasScript.save.money += (int)Random.Range(score/5, score/4);
        DatasScript.save.Save();
        transform.Find("ExplosionEffect").GetComponent<ParticleSystem>().Play();
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
        
        yield return new WaitForSeconds(2);
        if(Playing)
        {
            Pause();
        }
        Object.Destroy(PauseMenu.transform.Find("Panel/OptionList/ContinueButton").gameObject);
        PauseMenu.transform.Find("Panel/OptionList/Score").GetComponent<TMP_Text>().alpha = 1;
        for(int i = 1; i < 200; i += 1)
        {
            yield return new WaitForSecondsRealtime(0.01f);
            PauseMenu.transform.Find("Panel/OptionList/Score").gameObject.GetComponent<LocalizeStringEvent>().StringReference["scoreshown"] = new IntVariable() { Value = (int)(score / ((float)200 / i)) };
            PauseMenu.transform.Find("Panel/OptionList/Score").gameObject.GetComponent<LocalizeStringEvent>().RefreshString();
        }
        PauseMenu.transform.Find("Panel/OptionList/Score").gameObject.GetComponent<LocalizeStringEvent>().StringReference["scoreshown"] = new IntVariable() { Value = (int)score };
        PauseMenu.transform.Find("Panel/OptionList/Score").gameObject.GetComponent<LocalizeStringEvent>().StringReference["highscore"] = new BoolVariable() { Value = highscore };
        PauseMenu.transform.Find("Panel/OptionList/Score").gameObject.GetComponent<LocalizeStringEvent>().RefreshString();
    }

    public void AddRandomPowerUp()
    {
        if(Power == null)
        {
            Power = (PowerUp)System.Activator.CreateInstance(availables_powerups[Random.Range(0, availables_powerups.Length)]);
        }
    }

    public void AddHealth(uint n)
    {
        if (health == 0) return;
        Debug.Log(n);
        health += health + n >= maxhealth ? maxhealth - health : n;
        UI.transform.Find("HealthBar/Health").GetComponent<RectTransform>().sizeDelta = new Vector2(280f * ((float)health / maxhealth), 0.85f);
        effects();
    }

    void Pause()
    {
        PauseMenu.SetActive(Playing);
        holdingFire = Time.timeScale == 0 ? true : holdingFire;
        Playing = !Playing;
        attacks.main.setPlaying(Playing);
        if(attacks.second != null)
        {
            attacks.second.setPlaying(Playing);
        }
        if (transform.Find("StunEffect").GetComponent<AudioSource>().isPlaying) transform.Find("StunEffect").GetComponent<AudioSource>().Pause();

        Time.timeScale = Playing ? 1 : 0;
        if (Playing)
        {
            if (disabled) transform.Find("StunEffect").GetComponent<AudioSource>().Play();
            sound.Play();
        }
        else
        {
            PauseMenu.transform.Find("Highscore").gameObject.GetComponent<LocalizeStringEvent>().StringReference["score"] = new IntVariable() { Value = DatasScript.save.highscore[GameManager.worldindex - 1] };
            PauseMenu.transform.Find("Highscore").gameObject.GetComponent<LocalizeStringEvent>().RefreshString();
            GameObject.Find("EventSystem").GetComponent<EventSystem>().firstSelectedGameObject = PauseMenu.transform.Find("Panel/OptionList/" + (health > 0 ? "ContinueButton" : "RestartButton")).gameObject;
            GameObject.Find("EventSystem").GetComponent<EventSystem>().SetSelectedGameObject(GameObject.Find("EventSystem").GetComponent<EventSystem>().firstSelectedGameObject);
            sound.Stop();
        }
    }

    IEnumerator onDamage()
    {
        gameObject.GetComponent<SpriteRenderer>().color = Color.red;
        UI.transform.Find("HealthBar/Health").GetComponent<UnityEngine.UI.Image>().color = Color.red;
        yield return new WaitForSeconds(0.1f);
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        UI.transform.Find("HealthBar/Health").GetComponent<UnityEngine.UI.Image>().color = Color.green;
    }

    IEnumerator Fire(WeaponInfo weapon)
    {
        currently_used = weapon;
        while ((Input.GetKey(DatasScript.settings.Keys[weapon == attacks.main ? "FirstWeapon" : "SecondWeapon"]) || (Input.GetJoystickNames().Length > 0 && Gamepad.current[weapon == attacks.main ? "buttonSouth" : "buttonEast"].IsPressed())) && health != 0 && energy > 0 && weapon.work)
        {
            canrefill = false;
            float extra_energy = weapon.Action();
            energy -= !weapon.use_energy ? 0 : energy >= weapon.w.energy ? weapon.w.energy : energy;
            energy -= extra_energy >= energy ? energy : extra_energy;

            UI.transform.Find("EnergyBar/Energy").gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(280f * ((float)energy / maxenergy), 0.85f);
            if (!weapon.w.hold)
            {
                break;
            }
            yield return new WaitForSeconds(weapon.w.cooldown);
        }
        currently_used = null;

        if(energy == 0)
        {
            blocked = true;
        }
        StartCoroutine(weapon.WaitCooldown());
        
        yield return new WaitForSeconds(1);

        canrefill = true;
        if (energy == 0)
        {
            while((uint)energy != maxenergy)
            {
                UI.transform.Find("EnergyBar/Energy").gameObject.GetComponent<UnityEngine.UI.Image>().color = UI.transform.Find("EnergyBar/Energy").gameObject.GetComponent<UnityEngine.UI.Image>().color == new Color(0, 255, 255) ? new Color(255, 0, 0) : new Color(0, 255, 255);
                yield return new WaitForSeconds(0.15f);
            }
            UI.transform.Find("EnergyBar/Energy").gameObject.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 255, 255);
            blocked = false;
        }
    }

    public void PickupItem(GameObject obj)
    {
        switch(obj.GetComponent<ItemScript>().v)
        {
            case "health":
                health += (uint)obj.GetComponent<ItemScript>().value;
                if(health > maxhealth)
                {
                    health = maxhealth;
                }
                UI.transform.Find("HealthBar/Health").GetComponent<RectTransform>().sizeDelta = new Vector2(280f * ((float)health / maxhealth), 0.85f);
                effects();
                break;
        }
        Object.Destroy(obj);
    }
}