using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.Linq;

internal struct Planet_Combo
{
    public bool use;
    public int number;
    public int maxnumber;
    public float group_xpos;
    public int current_delay;
}

public class StorageManager : MonoBehaviour
{
    public static GameObject Storage;
    [SerializeField] GameObject Cam;

    float campos;
    public static bool isBoss { get; private set; }
    public delegate void OnEnemyCreated(GameObject enemy);
    public OnEnemyCreated signal;

    public AreaInfo area_info { get; private set; }
    (float[] xpos, List<GameObject> entities, bool enabled) obstacleinfos;
    (float xpos, List<GameObject> entities, bool enabled) circleinfos;
    (float xpos, List<GameObject> entities, bool enabled) enemyinfo;
    (GameObject obj, Sprite[] skinlist, Planet_Combo infos, int startingspeed, float xpos, List<GameObject> entities, bool enabled) planetinfo;

    GameObject portal;

    (int lastSpawned, int basespawn) bossSpawnScore;

    // Start is called before the first frame update
    void Start()
    {
        area_info = Resources.Load<AreaInfo>($"Area{GameManager.worldindex}");
        bossSpawnScore = (0, 0);
        bossSpawnScore.basespawn = Random.Range(area_info.base_spawn[0], area_info.base_spawn[1]);
        Storage = gameObject;
        campos = 0;
        enemyinfo = (0, new(), true);
        obstacleinfos = (new float[area_info.obstacles.Length], new(), true);
        circleinfos = (0, new(), true);
        var planets = Resources.Load<ResourcesList>("Planets");
        planetinfo = (planets.ObjectArray[0], planets.SpriteArray, new(), 50, -2500, new(), true);

        portal = Resources.Load<GameObject>("Storage/Portal");
        if(area_info.star_enabled)
        {
            for (int i = 0; i < 40/area_info.star_delay; i++)
            {
                create_circles(Random.Range(-200 + (i * 40 / area_info.star_delay), -200 + (i * 40 / area_info.star_delay) + 20));
            }
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        checklist(circleinfos.entities);
        checklist(planetinfo.entities);

        campos += SpaceShipPlayer.Speed * Time.fixedDeltaTime;
        if (area_info.star_enabled && campos - circleinfos.xpos > 40)
        {
            if(circleinfos.enabled)
            {
                create_circles(Random.Range(300, 320));
            }

            circleinfos.xpos = campos;
        }

        if(enemyinfo.enabled)
        {
            for (int i = 0; i < area_info.obstacles.Length; i++)
            {
                if (campos - obstacleinfos.xpos[i] > area_info.obstacles_delay[i] * SpaceShipPlayer.Speed)
                {
                    if (i == 0)
                    {
                        create_asteroid();
                    }
                    else
                    {
                        GameObject clone = Object.Instantiate(area_info.obstacles[i], new Vector3(Random.Range(200, 250), Random.Range(-100, 100), 0), Quaternion.Euler(0, 0, Random.Range(0, 360)));
                        clone.transform.SetParent(transform);

                        clone.GetComponent<Rigidbody2D>().velocity = Vector2.left * SpaceShipPlayer.Speed;
                        if(clone.GetComponent<SetUpScript>() is not null)
                        {
                            clone.GetComponent<SetUpScript>().Init();
                        }

                        obstacleinfos.entities.Add(clone);
                    }
                    obstacleinfos.xpos[i] = campos;
                }
            }
        }
        if (SpaceShipPlayer.Speed >= area_info.enemy_startingspeed[0] && campos - enemyinfo.xpos > area_info.enemy_delay * SpaceShipPlayer.Speed)
        {
            if(enemyinfo.enabled)
            {
                StartCoroutine(create_enemyship((ushort)Random.Range(2, 5)));
            }

            enemyinfo.xpos = campos;
        }
        if (planetinfo.enabled && SpaceShipPlayer.Speed >= planetinfo.startingspeed)
        {
            if (planetinfo.infos.use && campos - planetinfo.infos.group_xpos > planetinfo.infos.current_delay * SpaceShipPlayer.Speed)
            {
                create_planet();
            }
            else if (!planetinfo.infos.use && campos - planetinfo.xpos > 50 * SpaceShipPlayer.Speed)
            {
                planetinfo.infos.use = true;
                planetinfo.infos.number = 0;
                planetinfo.infos.maxnumber = Random.Range(1, 6);
                create_planet();
            }
        }

        if (enemyinfo.enabled)
        {
            if ((SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().score > bossSpawnScore.basespawn && bossSpawnScore.lastSpawned == 0) || bossSpawnScore.lastSpawned + area_info.boss_delay <= SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().score)
            {
                GameObject b_instance = Object.Instantiate(area_info.Boss_Object, transform);
                b_instance.name = b_instance.name.Replace("(Clone)", "");
                b_instance.transform.position += new Vector3(300, 0);
                b_instance.GetComponent<BossSetUpScript>().bossLevel = 1 + (uint)(((int)SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().score - bossSpawnScore.basespawn) / area_info.boss_delay);
                b_instance.GetComponent<BossSetUpScript>().Init();
                bossSpawnScore.lastSpawned = (int)SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().score;
            }
        }
    }
    public void Update_speed()
    {
        foreach(GameObject v in circleinfos.entities)
        {
            v.GetComponent<Rigidbody2D>().velocity = Vector2.left * SpaceShipPlayer.Speed;
        }
    }

    public void Boss(bool enabled, bool change_planet = true)
    {
        isBoss = enabled;
        Debug.Log(!enabled);
        enemyinfo.enabled = !enabled;
        obstacleinfos.enabled = !enabled;
        planetinfo.enabled = change_planet ? !enabled : planetinfo.enabled;
    }

    void checklist(List<GameObject> l)
    {
        foreach (GameObject obj in l.Where(w => w is null).ToList())
        {
            l.Remove(obj);
        }
        foreach (GameObject obj in l.Where(w => (w.GetComponent<SpriteRenderer>() ? !w.GetComponent<SpriteRenderer>().isVisible : true) && !w.GetComponentsInChildren<SpriteRenderer>().Where(x => x.isVisible).Any()
            && w.transform.position.x < SpaceShipPlayer.plr.transform.position.x).ToList()) // if an object is not renderer and behing the player
        {
            l.Remove(obj);
            Object.Destroy(obj);
        }
    }

    public void create_circles(int xpos)
    {
        GameObject clone = Object.Instantiate(area_info.star, new Vector3(xpos, Random.Range(-100, 100), 0), Quaternion.Euler(0, 0, Random.Range(0, 360)));
        clone.GetComponent<Rigidbody2D>().velocity = Vector2.left * SpaceShipPlayer.Speed;
        clone.transform.SetParent(transform);

        Light2D light = clone.transform.Find("Light").GetComponent<Light2D>();
        light.pointLightOuterRadius = Random.Range(light.pointLightOuterRadius, light.pointLightOuterRadius * 2.5f);
        light.intensity = Random.Range(light.intensity * 10, light.intensity * 15) / 10;
        circleinfos.entities.Add(clone);
    }

    public void create_asteroid()
    {
        GameObject clone = Object.Instantiate(area_info.obstacles[0], new Vector3(Random.Range(200, 250), Random.Range(-150, 150), 0), Quaternion.Euler(0, 0, Random.Range(0, 360)));
        clone.transform.SetParent(transform);

        clone.GetComponent<SpriteRenderer>().sprite = Resources.Load<ResourcesList>("Asteroids").SpriteArray[Random.Range(0, 6)];
        clone.GetComponent<Rigidbody2D>().velocity = (Vector3.zero - clone.transform.position).normalized * SpaceShipPlayer.Speed * 2;
        clone.GetComponent<SetUpScript>().Init();
        int rnd = Random.Range((int)clone.transform.localScale.x, (int)(clone.transform.localScale.x * 1.5f) + 1);
        clone.transform.localScale = new Vector3(rnd, rnd, 1);
        obstacleinfos.entities.Add(clone);
        clone.GetComponent<BehaviorScript>().OnDestroyed += () => obstacleinfos.entities.Remove(clone);
    }

    public void create_asteroid(Vector2 point, Vector2 direction)
    {
        GameObject clone = Object.Instantiate(area_info.obstacles[0], point, Quaternion.Euler(0, 0, Random.Range(0, 360)));
        clone.transform.SetParent(transform);

        clone.GetComponent<SpriteRenderer>().sprite = Resources.Load<ResourcesList>("Asteroids").SpriteArray[Random.Range(0, 7)];
        clone.GetComponent<Rigidbody2D>().velocity = direction * SpaceShipPlayer.Speed;
        clone.GetComponent<SetUpScript>().Init();
        int rnd = Random.Range((int)clone.transform.localScale.x, (int)(clone.transform.localScale.x * 1.5f) + 1);
        clone.transform.localScale = new Vector3(rnd, rnd, 1);
        obstacleinfos.entities.Add(clone);
        clone.GetComponent<BehaviorScript>().OnDestroyed += () => obstacleinfos.entities.Remove(clone);
    }

    public IEnumerator create_enemyship(ushort number, Transform spawnpoint = null, int[]  avoided_enemies = null)
    {
        enemyinfo.entities = enemyinfo.entities.Where(w => w != null).ToList();
        if (enemyinfo.entities.Count <= 10)
        {
            for (int i = 0; i < number; i++)
            {
                int rnd = Random.Range(1, 101);
                int choosed = System.Array.FindIndex(area_info.enemy_probabilities, w => w >= rnd); //choose the enemy
                choosed = area_info.enemy_startingspeed[choosed] <= SpaceShipPlayer.Speed ? choosed : 0;
                if(avoided_enemies != null && avoided_enemies.Contains(choosed))
                {
                    choosed = area_info.enemy_object.Select(x => area_info.enemy_object.ToList().IndexOf(x)).Where(w => !avoided_enemies.Contains(w)).First();
                }

                GameObject clone = Object.Instantiate(area_info.enemy_object[choosed], spawnpoint != null ? spawnpoint.position : new Vector3(200, 0, 0), Quaternion.Euler(0, 0, 90));
                clone.transform.SetParent(transform);

                clone.GetComponent<SetUpScript>().Init();
                enemyinfo.entities.Add(clone);

                signal?.Invoke(clone);

                yield return new WaitForSeconds(1.5f);
            }
        }
    }


    public GameObject create_portal(Vector3 position, bool instant)
    {
        GameObject clone = Object.Instantiate(portal, position, Quaternion.Euler(0, 0, 0));
        clone.transform.SetParent(transform);
        clone.GetComponent<SetUpScript>().Init();
        if(instant)
        {
            StartCoroutine(clone.GetComponent<Portal>().Action());
        }
        return clone;
    }
    public GameObject create_portal(Vector3 position, float cooldown)
    {
        GameObject clone = Object.Instantiate(portal, position, Quaternion.Euler(0, 0, 0));
        clone.GetComponent<SetUpScript>().Init();
        StartCoroutine(waitForPortal(clone, cooldown));
        return portal;
    }
    IEnumerator waitForPortal(GameObject obj, float t)
    {
        yield return new WaitForSeconds(t);
        StartCoroutine(obj.GetComponent<Portal>().Action());
    }

    void create_planet()
    {
        planetinfo.infos.group_xpos = campos;
        planetinfo.infos.current_delay = Random.Range(10, 21);
        planetinfo.infos.number += 1;
        if(planetinfo.infos.number == planetinfo.infos.maxnumber)
        {
            planetinfo.infos.use = false;
            planetinfo.xpos = campos;
        }

        GameObject clone = Object.Instantiate(planetinfo.obj, new Vector3(400, Random.Range(-90, 90), 0), Quaternion.Euler(0, 0, Random.Range(0, 360)));
        clone.transform.SetParent(transform);
        clone.GetComponent<SpriteRenderer>().sprite = planetinfo.skinlist[Random.Range(0, planetinfo.skinlist.Length)];

        clone.GetComponent<SetUpScript>().Init();
        planetinfo.entities.Add(clone);
    }
}