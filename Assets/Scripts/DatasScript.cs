using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using UnityEngine.Localization.Settings;
using System.Linq;
using CI.QuickSave;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}

public static class DatasScript
{
    public record Axis(int axis, int value);
    public static SettingsData settings = new();
    public static GameSave save = new();

    public static void checkFolder()
    {
        if (!File.Exists(Application.persistentDataPath + "/settings.json") || File.ReadAllText(Application.persistentDataPath + "/settings.json") == String.Empty)
        {
            settings = new();
        }
        UpdateSettingsInfo();

        Application.quitting += () => SaveAll();
    }

    public static void SaveAll()
    {
        AudioListener.volume = 0;
        UpdateSettingsToJson();
        save.Save();
        return;
    }

    public static void UpdateSettingsToJson()
    {
        File.WriteAllText(Application.persistentDataPath + "/settings.json", JsonConvert.SerializeObject(settings, new JsonSerializerSettings { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto }));
        Debug.Log("settings saved");
    }

    static GameObject discord_object;

    public static void UpdateDiscordPresence()
    {
        if (settings.DiscordPresence && discord_object is null)
        {
            discord_object = new GameObject();
            discord_object.AddComponent<DiscordActivity>();
        }
        else if(!settings.DiscordPresence && discord_object is not null)
        {
            UnityEngine.Object.Destroy(discord_object);
        }
    }

    public static void UpdateSettingsInfo()
    {
        Debug.Log("loading settings");
        if(File.Exists(Application.persistentDataPath + "/settings.json"))
        {
            foreach (var val in JObject.Parse(File.ReadAllText(Application.persistentDataPath + "/settings.json")))
            {
                
                if (settings.GetType().GetField(val.Key) != null)
                {
                    settings.GetType().GetField(val.Key).SetValue(settings, val.Value.ToObject(settings.GetType().GetField(val.Key).GetValue(settings).GetType()));
                }
                else if(settings.GetType().GetProperty(val.Key) != null)
                {
                    if(val.Key == "KeysInt")
                    {
                        foreach(KeyValuePair<string, int> v in (Dictionary<string, int>)val.Value.ToObject(settings.KeysInt.GetType()))
                        {
                            settings.Keys[v.Key] = (KeyCode)v.Value;
                        }
                    }
                    else
                    {
                        settings.GetType().GetProperty(val.Key).SetValue(settings, val.Value.ToObject(settings.GetType().GetProperty(val.Key).GetValue(settings).GetType()));
                    }
                }
            }
        }

        Screen.SetResolution((int)settings.resolution.x, (int)settings.resolution.y, settings.fullscreen);
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[settings.language];
        AudioListener.volume = settings.volume;
        UpdateDiscordPresence();
        Debug.Log("settings loaded " + (File.Exists(Application.persistentDataPath + "/settings.json") ? "with existing file" : "with default values"));
    }
}

public class SettingsData
{
    public bool fullscreen = true;
    public (uint x, uint y) resolution = (x: (uint)Screen.resolutions[Screen.resolutions.Length - 1].width, y: (uint)Screen.resolutions[Screen.resolutions.Length - 1].height);
    public float volume = 0.5f;
    public bool notifications = true;
    public bool DiscordPresence = true;
    [JsonIgnore]
    public Dictionary<(int, int), KeyCode> PlacementControls = new()
    {
        { (0, 1), KeyCode.D },
        { (0, -1), Application.systemLanguage == SystemLanguage.French ? KeyCode.Q : KeyCode.A },
        { (1, 1), Application.systemLanguage == SystemLanguage.French ? KeyCode.Z : KeyCode.W },
        { (1, -1), KeyCode.S },
    };

    public Dictionary<string, int> PlacementInt
    {
        get
        {
            return PlacementControls.ToDictionary(w => w.Key.Item1.ToString() + w.Key.Item2.ToString(), x => (int)x.Value);
        }
        set
        {
            PlacementControls = value.ToDictionary(w => (int.Parse(w.Key[0].ToString()), int.Parse(w.Key.Remove(0, 1))), x => (KeyCode)x.Value);
        }
    }

    public Dictionary<string, int> KeysInt
    {
        get
        {
            return Keys.ToDictionary(w => w.Key, x => (int)x.Value);
        }
        set
        {
            Keys = value.ToDictionary(w => w.Key, x => (KeyCode)x.Value);
        }
    }
    [JsonIgnore]
    public Dictionary<string, KeyCode> Keys = new()
    {
        { "FirstWeapon", KeyCode.LeftShift },
        { "SecondWeapon", Application.systemLanguage == SystemLanguage.French ? KeyCode.A : KeyCode.Q },
        { "PowerUp", KeyCode.F },
    };
    public int language = LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);
}

public enum WeaponsType { LaserWeapon, BombWeapon, ShieldWeapon, PlasmaWeapon }

public class GameSave
{
    //Set base values
    public List<Spaceship> spaceships { get; private set; } = new()
    {
        new() {name="Traveler", maxlevel=3, obj="Ships/Traveler", level_rate=(5, 0.1f, 0.05f), cost=50, health=100, damage=1, SceneSpeed=1f, possessed=true},
        new() {name="Dragon", maxlevel=5, obj="Ships/Dragon", level_rate=(4, 0.1f, 0.1f), cost=100, health=80, damage=0.8f, SceneSpeed=1.5f },
        new() {name="Cruiser", maxlevel=7, obj="Ships/Cruiser", level_rate=(10, 0.2f, 0.02f), cost=300, health=130, damage=1.1f, SceneSpeed=0.86f },
        new() {name="StarLight", maxlevel=10, obj="Ships/StarLight", level_rate=(10, 0.2f, 0.15f), cost=500, health=200, damage=1.5f, SceneSpeed=1.25f },
    };

    public List<Weapon> Weapons { get; private set; } = new()
    {
        new() {name="Laser", cooldown=0.15f, damage=10, hold=true, possessed=true, wait_time=0, energy=2, type=WeaponsType.LaserWeapon, cost = 0, baseweapon=Resources.Load<GameObject>("Laser"),speed=3 },
        new() {name="Missile", cooldown=0, damage=50, hold=false, possessed=false, wait_time=0.2f, energy=20, type=WeaponsType.BombWeapon, cost=750, baseweapon=Resources.Load<GameObject>("Missile"), speed=2 },
        new() {name="Shield", cooldown=0.1f, damage=0, hold=true, possessed=false, wait_time=0, energy=0.5f, type=WeaponsType.ShieldWeapon, cost=2000, baseweapon=Resources.Load<GameObject>("Shield"), speed=0 },
        new() {name="Plasma Cannon", cooldown=0.2f, damage=10, hold=true, possessed=false, wait_time=0.5f, energy=4, type=WeaponsType.PlasmaWeapon, cost=5000, baseweapon=Resources.Load<GameObject>("Orb"), speed=3 },
        new() {name="Overpowered Missile", cooldown=0, damage=80, hold=false, possessed=false, wait_time=0.2f, energy=40, type=WeaponsType.BombWeapon, cost=4000, baseweapon=Resources.Load<GameObject>("OverpoweredMissile"), speed = 3 },
    };
    public int currentship = 0;

    [JsonIgnore]
    public int[] highscore = new int[] {0, 0, 0};
    public List<int> list_highscore
    {
        get
        {
            return highscore.ToList();
        }
        private set
        {
            highscore = value.ToArray();
        }
    }

    public int money = 0;

    [JsonIgnore]
    public bool[] boss_defeated = new bool[] { false, false, false };
    public List<int> int_bossdefeated
    {
        get
        {
            return boss_defeated.Select(x => x ? 1 : 0).ToList();
        }
        private set
        {
            boss_defeated = value.Select(x => x != 0).ToArray();
        }
    }
    
    public Spaceship get_current_ship()
    {
        return spaceships[currentship];
    }

    public GameSave()
    {
        foreach(var v in spaceships)
        {
            v.save = this;
        }
        if(File.Exists(Application.persistentDataPath + "/QuickSave/GameSave.json"))
        {
            QuickSaveReader reader = QuickSaveReader.Create("GameSave", new QuickSaveSettings() { SecurityMode = SecurityMode.Aes, Password = Environment.UserName, CompressionMode = CompressionMode.None });

            //load those two elements before everything else
            foreach((string name, bool possessed) value in reader.Read<List<(string, bool)>>("Weapons"))
            {
                if(Weapons.Where(w => w.name == value.name).Any())
                {
                    Weapons[Weapons.IndexOf(Weapons.Where(w => w.name == value.name).First())].possessed = value.possessed;
                }
            }
            foreach (KeyValuePair<string, Dictionary<string, object>> s in reader.Read<Dictionary<string, Dictionary<string, object>>>("spaceships"))
            {
                if (spaceships.Where(w => w.name == s.Key).Any())
                {
                    spaceships[spaceships.IndexOf(spaceships.Where(w => w.name == s.Key).First())].dict = s.Value;
                }
            }

            foreach (string key in reader.GetAllKeys())
            {
                if (this.GetType().GetField(key) != null)
                {
                    var v = this.GetType().GetField(key).GetValue(this);
                    Debug.Log(reader.Read<object>(key));
                    Debug.Log(key);
                    try
                    {
                        this.GetType().GetField(key).SetValue(this, Convert.ChangeType(reader.Read<object>(key), this.GetType().GetField(key).GetValue(this).GetType()));
                    }
                    catch
                    {
                        Debug.Log("error while loading datas (field)");
                    }
                }
                if(this.GetType().GetProperty(key) != null && key != "Weapons" && key != "spaceships")
                {
                    try
                    {
                        this.GetType().GetProperty(key).SetValue(this, reader.Read<List<int>>(key));
                    }
                    catch(Exception ex)
                    {
                        Debug.Log("error when loading datas (property) : " + ex.ToString());
                    }
                }
            }
            if (!spaceships[currentship].possessed)
            {
                currentship = 0;
            }
        }
        else
        {
            QuickSaveWriter.Create("GameSave", new QuickSaveSettings() { SecurityMode = SecurityMode.Aes, Password = Environment.UserName, CompressionMode = CompressionMode.None }).Commit();
        }
    }

    public void Save()
    {
        Debug.Log("saving game");
        QuickSaveWriter writer = QuickSaveWriter.Create("GameSave", new QuickSaveSettings() { SecurityMode = SecurityMode.Aes, Password = Environment.UserName });
        foreach (FieldInfo v in this.GetType().GetFields())
        {
            if(Attribute.IsDefined(v, typeof(JsonIgnoreAttribute))) continue;
            writer.Write(v.Name, v.GetValue(this));
        }
        foreach(PropertyInfo v in this.GetType().GetProperties())
        {
            if (Attribute.IsDefined(v, typeof(JsonIgnoreAttribute))) continue;

            switch (v.Name)
            {
                case "spaceships":
                    writer.Write("spaceships", spaceships.ToDictionary(w => w.name, x => x.dict));
                    break;
                case "Weapons":
                    writer.Write("Weapons", Weapons.Select(w => (w.name, w.possessed)).ToList());
                    break;
                default:
                    writer.Write(v.Name, v.GetValue(this));
                    break;
            }
        }
        writer.Commit();
        Debug.Log("Game saved");
    }
}

[Serializable]
public class Spaceship
{
    private GameSave _save;
    public GameSave save
    {
        get
        {
            return _save;
        }

        set
        {
            if(_save == null)
            {
                _save = value;
            }
        }
    }

    private bool usage = true;

    public string obj;
    public string name { get; init; }
    private bool _possessed = false;
    public bool possessed {
        get
        {
            return _possessed;
        }
        set
        {
            if (DatasScript.save == null)
            {
                _possessed = value;
            }
        }
    }
    private float _damage;
    public float damage { get => _damage; init => _damage = value; }
    private float _SceneSpeed;
    public float SceneSpeed { get => _SceneSpeed; init => _SceneSpeed = value; }
    public int level
    {
        get
        {
            return _level;
        }
        set
        {
            if(usage)
            {
                usage = false;
                _level = value;
                _health += level_rate.health * level;
                _damage += level_rate.damage * level;
                _SceneSpeed += level_rate.speed * level;
            }
            else if(level < maxlevel)
            {
                upgrade_level();
            }
        }
    }

    private int _level = 0;
    public int maxlevel { get; init; }
    private int _health;
    public int health { get => _health; init => _health = value; }
    public (int health, float damage, float speed) level_rate { get; init; }
    public int cost { get; init; }

    private (int main, int second) _weapons = (0, -1);
    public (int main, int second) weapons {
        get
        {
            return _weapons;
        }
        set
        {
            if (save.Weapons.Count() >= value.main && value.main >= 0 && save.Weapons.Count() >= value.second && value.second >= -1 && value.main != value.second)
            {
                if (save.Weapons[value.main].possessed && (value.second == -1 || save.Weapons[value.second].possessed))
                {
                    _weapons = value;
                }
            }
        }
    }


    private void upgrade_level()
    {
        uint price = (uint)(cost * Mathf.Pow(2, level ));
        if(DatasScript.save.money >= price)
        {
            DatasScript.save.money -= (int)price;
            _level += 1;
            _health += level_rate.health;
            _SceneSpeed += level_rate.speed;
            _damage += level_rate.damage;
        }
    }

    public void Buy()
    {
        uint price = (uint)cost * 10;
        if(!possessed && DatasScript.save.money >= price)
        {
            DatasScript.save.money -= (int)price;
            _possessed = true;
            DatasScript.save.Save();
        }
    }

    private List<string> available_fields = new() { "possessed", "level", "weapons" };
    public Dictionary<string, object> dict
    {
        get
        {
            return new()
            {
                { "possessed", _possessed },
                { "level", _level },
                { "weapons", _weapons }
            };
        }
        set
        {
            foreach(var v in value)
            {
                if(available_fields.Contains(v.Key) && this.GetType().GetProperty(v.Key) != null)
                {
                    object _value = v.Value;
                    if(v.Value is JObject)
                    {
                        _value = ((JObject)v.Value).ToObject(this.GetType().GetProperty(v.Key).GetValue(this).GetType());
                    }
                    this.GetType().GetProperty(v.Key).SetValue(this, Convert.ChangeType(_value, this.GetType().GetProperty(v.Key).GetValue(this).GetType()));
                }
            }
        }
    }
}

[Serializable]
public class Weapon
{
    public int cost { get; init; }
    public WeaponsType type { get; init; }
    public GameObject baseweapon { get; init; }
    public float speed { get; init; }
    Type script = Type.GetType(nameof(type));

    public float energy { get; init; }
    public string name { get; init; }
    public float damage { get; init; }
    public float cooldown { get; init; }
    public float wait_time { get; init; }
    public bool hold { get; init; }

    public WeaponInfo component { get; private set; }

    private bool _possessed = false;
    public bool possessed
    {
        get
        {
            return _possessed;
        }
        set
        {
            if (DatasScript.save == null)
            {
                _possessed = value;
            }
        }
    }

    public void Buy()
    {
        if(!possessed && DatasScript.save.money >= cost)
        {
            _possessed = true;
            DatasScript.save.money -= cost;
            DatasScript.save.Save();
        }
    }

    public WeaponInfo AddWeaponToShip()
    {
        if(SpaceShipPlayer.plr != null)
        {
            component = (WeaponInfo)SpaceShipPlayer.plr.AddComponent(Type.GetType(type.ToString()));
            component.Link(this);
            return component;
        }
        return null;
    }
}

//everything about weapons
public abstract class WeaponInfo : MonoBehaviour
{
    public bool work = true;
    public bool use_energy { get; protected set; } = true;
    public Weapon w { get; private set; }
    public void Link(Weapon new_w)
    {
        if(w == null)
        {
            w = new_w;
        }
    }

    public abstract float Action();
    public abstract void Collide(GameObject Instance, Collider2D collided, Collision_Type c_type);

    public virtual IEnumerator WaitCooldown()
    {
        work = false;
        yield return new WaitForSeconds(w.wait_time);
        work = true;
    }

    public virtual void setPlaying(bool playing) { return; }
}

public class LaserWeapon : WeaponInfo
{
    short ypos = 5;
    public override float Action()
    {
        GameObject Laser = GameObject.Instantiate(w.baseweapon, gameObject.transform.position + new Vector3(5, ypos, 0), new Quaternion());
        ypos = (short)-ypos;
        Laser.transform.SetParent(StorageManager.Storage.transform);
        Laser.GetComponent<Rigidbody2D>().velocity = Vector3.right * SpaceShipPlayer.Speed * w.speed;
        transform.Find("LaserSound").GetComponent<AudioSource>().Play();
        UnityEngine.Object.Destroy(Laser, 5);
        Laser.AddComponent<Collision>();
        Laser.GetComponent<Collision>().init(Collide);
        Laser.layer = LayerMask.NameToLayer("PlayerObject");
        return 0;
    }

    public override void Collide(GameObject Instance, Collider2D collided, Collision_Type c_type)
    {
        UnityEngine.Object.Destroy(Instance);
        if (collided.gameObject.GetComponent<BehaviorScript>())
        {
            collided.gameObject.GetComponent<BehaviorScript>().onDamage((ushort)(DatasScript.save.get_current_ship().damage * w.damage), Instance.layer);
        }
        else if(collided.gameObject.GetComponentInParent<BehaviorScript>())
        {
            collided.gameObject.GetComponentInParent<BehaviorScript>().onDamage((ushort)(DatasScript.save.get_current_ship().damage * w.damage), Instance.layer);
        }
    }
}

public class BombWeapon : WeaponInfo
{
    GameObject Bomb;

    public override float Action()
    {
        if (Bomb != null && !Bomb.GetComponent<SpriteRenderer>().isVisible)
        {
            UnityEngine.Object.Destroy(Bomb);
            Bomb = null;
        }

        if(Bomb == null)
        {
            Bomb = UnityEngine.Object.Instantiate(w.baseweapon, transform.position + Vector3.right * 10, Quaternion.Euler(0, 0, 0));
            Bomb.transform.rotation = transform.rotation;
            Bomb.transform.SetParent(StorageManager.Storage.transform);
            Bomb.GetComponent<Rigidbody2D>().velocity = SpaceShipPlayer.Speed * Vector3.right * w.speed;
            Bomb.layer = LayerMask.NameToLayer("CollidingPlayerObject");
            Bomb.GetComponent<SetUpScript>().Init();
            if(Bomb.GetComponent<Bomb>())
            {
                Bomb.GetComponent<Bomb>().max_damage = DatasScript.save.get_current_ship().damage * w.damage;
            }
            else
            {
                Bomb.GetComponent<BigBomb>().max_damage = DatasScript.save.get_current_ship().damage * w.damage;
            }
            Bomb.AddComponent<Collision>();
            Bomb.GetComponent<Collision>().init(Collide);
            use_energy = true;
        }
        else
        {
            if (Bomb.GetComponent<Bomb>())
            {
                Bomb.GetComponent<Bomb>().explode();
            }
            else
            {
                Bomb.GetComponent<BigBomb>().explode();
            }
            use_energy = false;
        }
        return 0;
    }

    public override void Collide(GameObject Instance, Collider2D collided, Collision_Type c_type)
    {
        use_energy = true;
    }
}

public class ShieldWeapon : WeaponInfo
{
    GameObject Shield;
    private float extra_energy;

    public override float Action()
    {
        if(Shield == null)
        {
            Shield = UnityEngine.Object.Instantiate(w.baseweapon, transform, false);
            Shield.layer = LayerMask.NameToLayer("Shield");

            Shield.AddComponent<Collision>();
            Shield.GetComponent<Collision>().init(Collide, this);
        }
        float value = extra_energy;
        extra_energy = 0;
        return value;
    }

    public void Damage(uint damage)
    {
        extra_energy += damage / 2;
    }

    public override void setPlaying(bool playing)
    {
        if(Shield)
        {
            if (!playing) Shield.transform.Find("Sound").GetComponent<AudioSource>().Pause();
            else Shield.transform.Find("Sound").GetComponent<AudioSource>().Play();
        }
    }

    public override void Collide(GameObject Instance, Collider2D collided, Collision_Type c_type)
    {
        if (collided.gameObject.GetComponent<ProjectileContainer>() != null)
        {
            extra_energy += collided.gameObject.GetComponent<ProjectileContainer>().Damage / 2;

            if(collided.gameObject.GetComponent<BehaviorScript>() != null && !collided.gameObject.CompareTag("Projectile"))
            {
                collided.gameObject.GetComponent<BehaviorScript>().onDamage(collided.gameObject.GetComponent<SetUpScript>().health, gameObject.layer);
            }
            else
            {
                UnityEngine.Object.Destroy(collided.gameObject);
            }
        }
    }

    public override IEnumerator WaitCooldown()
    {
        if(Shield != null)
        {
            UnityEngine.Object.Destroy(Shield);
        }
        return base.WaitCooldown();
    }
}

public class PlasmaWeapon : WeaponInfo
{
    int holdtime;

    public override float Action()
    {
        AudioSource sound = GameObject.Find("Camera/SoundObjects/ChargingWeapon").GetComponent<AudioSource>();
        if (!sound.isPlaying)
        {
            sound.Play();
        }
        holdtime += 1;
        return 0;
    }
    public override void setPlaying(bool playing)
    {
        AudioSource sound = GameObject.Find("Camera/SoundObjects/ChargingWeapon").GetComponent<AudioSource>();
        if(!playing && sound.isPlaying)
        {
            sound.Pause();
        }
        else if(playing && !sound.isPlaying && holdtime != 0)
        {
            sound.Play();
        }
    }

    public override void Collide(GameObject Instance, Collider2D collided, Collision_Type c_type)
    {
        if (collided.gameObject.GetComponent<BehaviorScript>() != null)
        {
            collided.gameObject.GetComponent<BehaviorScript>().onDamage((ushort)Instance.GetComponent<ProjectileContainer>().Damage, gameObject.layer);
        }
    }

    public override IEnumerator WaitCooldown()
    {
        if (holdtime != 0)
        {
            float damage = w.damage * (1 + 19 * (float)Math.Pow(Math.E, -5 * Math.Pow(Math.E, -holdtime / 6)));
            Debug.Log(damage);
            GameObject attack = UnityEngine.Object.Instantiate(w.baseweapon, SpaceShipPlayer.plr.transform.position + (SpaceShipPlayer.plr.transform.up * SpaceShipPlayer.plr.GetComponent<SpriteRenderer>().bounds.size.y / 2), Quaternion.Euler(0, 0, 0));
            attack.transform.localScale = Vector3.one + new Vector3(0.01f, 0.01f, 0) * damage;
            attack.transform.SetParent(StorageManager.Storage.transform);
            attack.GetComponent<Rigidbody2D>().velocity = Vector2.right * SpaceShipPlayer.Speed * w.speed;
            attack.GetComponent<Rigidbody2D>().angularVelocity = 360;
            attack.AddComponent<ProjectileContainer>().Damage = (uint)(damage * DatasScript.save.get_current_ship().damage);
            attack.AddComponent<Collision>().init(Collide);
            attack.GetComponent<TrailRenderer>().widthMultiplier = attack.transform.localScale.x * 15;
            UnityEngine.Object.Destroy(attack, 5);
        }
        GameObject.Find("ChargingWeapon").GetComponent<AudioSource>().Stop();


        holdtime = 0;
        return base.WaitCooldown();
    }
}

// everything about power up
public interface PowerUp
{
    bool Action();
}

public class Shockwave : PowerUp
{
    bool PowerUp.Action()
    {
        foreach(Collider2D collider in Physics2D.OverlapCircleAll(SpaceShipPlayer.plr.transform.position, 1000))
        {
            if(collider.gameObject.GetComponent<BehaviorScript>() != null && collider.gameObject.GetComponent<Planet>() == null)
            {
                collider.gameObject.GetComponent<BehaviorScript>().onDamage(30, SpaceShipPlayer.plr.layer);
                if(collider.gameObject.GetComponent<Attacker>() != null)
                {
                    collider.gameObject.GetComponent<Attacker>().DisableAI(5);
                }
            }
        }
        GameObject.Find("Camera/SoundObjects/Shockwave").GetComponent<AudioSource>().Play();
        GameObject obj = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Effects/Shockwave"), SpaceShipPlayer.plr.transform, true);
        obj.transform.position = SpaceShipPlayer.plr.transform.position;
        obj.transform.LeanScale(new Vector3(100, 100), 0.75f);
        UnityEngine.Object.Destroy(obj, 0.75f);
        return true;
    }
}

public class RepairKit : PowerUp
{
    bool PowerUp.Action()
    {
        SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().AddHealth(SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().maxhealth);
        return true;
    }
}

public class TargetedLaser : PowerUp
{
    bool PowerUp.Action()
    {
        //take the enemy with the most health
        Collider2D[] enemies = Physics2D.OverlapCircleAll(SpaceShipPlayer.plr.transform.position, 1000).Where(w => w.GetComponent<BehaviorScript>() != null && w.GetComponent<Planet>() == null && w.GetComponent<SpriteRenderer>().isVisible).OrderByDescending(w => w.GetComponent<BehaviorScript>().health).ToArray();
        if (!enemies.Any()) return false;

        GameObject enemy = enemies.First().gameObject;
        LineRenderer ray = SpaceShipPlayer.plr.AddComponent<LineRenderer>();
        ray.material = SpaceShipPlayer.plr.GetComponent<SpriteRenderer>().material;
        ray.startColor = new Color(255, 100, 100);
        ray.endColor = Color.red;
        ray.startWidth = ray.endWidth = 1;
        ray.positionCount = 2;
        SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().StartCoroutine(Effects(ray, enemy.transform.position));
        GameObject.Find("Camera/SoundObjects/TargetedLaserSound").GetComponent<AudioSource>().Play();

        UnityEngine.Object.Destroy(ray, 0.2f);
        enemy.GetComponent<BehaviorScript>().onDamage(400, SpaceShipPlayer.plr.layer);
        return true;

    }

    IEnumerator Effects(LineRenderer l, Vector3 enemypos)
    {
        while(l != null)
        {
            l.SetPositions(new Vector3[] { SpaceShipPlayer.plr.transform.position, enemypos } );
            yield return new WaitForEndOfFrame();
        }
    }
}