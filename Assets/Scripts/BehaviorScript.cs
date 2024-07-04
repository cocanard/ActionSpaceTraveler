using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BehaviorScript : MonoBehaviour
{
    public delegate void GlitchedEvent(GameObject obj);
    public static GlitchedEvent OnGlitchCreated;
    [SerializeField] private bool Destroy = true;

    protected ushort reward;
    protected ushort _health;
    internal ushort health
    {
        get
        {
            return _health;
        }
        set
        {
            _health = value;
            if(this is not Boss && this is not Planet)
            {
                if (transform.Find("HealthBar"))
                {
                    Object.Destroy(transform.Find("HealthBar").gameObject);
                }
                GameObject hb = Object.Instantiate(Resources.Load<GameObject>("HealthBar"), transform);
                hb.transform.rotation = Quaternion.Euler(0, 0, gameObject.transform.rotation.eulerAngles.x);
                hb.name = "HealthBar";

                hb.transform.Find("HealthBar/Health").GetComponent<RectTransform>().localScale = new Vector3((float)health / (float)basehealth, 1, 1);
                Object.Destroy(hb, 0.5f);
            }
        }
    }
    protected AudioSource Hit;
    protected AudioSource Destroyed;
    internal ushort basehealth;
    protected uint MetalSpawnRate;

    private void Start()
    {
        reward = gameObject.GetComponent<SetUpScript>().reward;
        _health = gameObject.GetComponent<SetUpScript>().health;
        basehealth = health;
        Hit = gameObject.GetComponent<SetUpScript>().HitSound;
        Destroyed = gameObject.GetComponent<SetUpScript>().DestroyedSound;
        MetalSpawnRate = (uint)(gameObject.GetComponent<SetUpScript>().MetalSpawnRate * 10);
    }

    public virtual void onAttackCollide(GameObject obj, Collider2D collision, Collision_Type c_type) { }

    internal delegate void OnDestruction();

    internal OnDestruction OnDestroyed;
    

    internal void onDestroyed(float destroytime = 0)
    {
        DatasScript.save.money += reward;
        Destroyed.Play();
        //Create an explosion
        GameObject Explosion = gameObject.transform.Find("Explosion").gameObject;
        Explosion.transform.SetParent(gameObject.transform.parent, true);
        Explosion.transform.localScale = gameObject.transform.localScale/5;
        Explosion.GetComponent<ParticleSystem>().Play();
        Object.Destroy(Explosion, Explosion.GetComponent<ParticleSystem>().main.duration + Explosion.GetComponent<ParticleSystem>().main.startLifetime.constant);
        //Spawn metal piece
        if(Random.Range(1, 1000) <= MetalSpawnRate)
        {
            Object.Instantiate(Resources.Load<GameObject>($"Items/Debris ({Random.Range(1, 6)})"), transform.position, new Quaternion(0, 0, 0, 0));
        }
        if(OnDestroyed is not null) OnDestroyed.Invoke();
        Object.Destroy(gameObject, destroytime);
    }

    public virtual void onDamage(ushort DamageAmount, LayerMask origin)
    {
        if (health == 0) return;
        health -= DamageAmount > health ? health : DamageAmount;
        if(this is Attacker)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }
        if (health == 0)
        {
            onDestroyed();
        }
        else
        {
            StartCoroutine(DamageEffect());
        }
    }

    internal IEnumerator DamageEffect()
    {
        Hit.Play();
        gameObject.GetComponent<SpriteRenderer>().color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (this != null)
        {
            gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }
}

public class Portal : MonoBehaviour
{
    public int[] excluded_enemies = new int[] { 3 };
    public float time;
    bool use = false;
    private void Start()
    {
        GetComponent<Animator>().SetFloat("Time", 10f);
    }

    public IEnumerator Action(float t = 2.5f)
    {
        if (!use)
        {
            use = true;
            Object.Destroy(gameObject, t);
            StorageManager.Storage.GetComponent<StorageManager>().StartCoroutine(GameObject.Find("Storage").GetComponent<StorageManager>().create_enemyship(1, transform, excluded_enemies));
            
            transform.Find("Light").GetComponent<UnityEngine.Rendering.Universal.Light2D>().enabled = true;
            yield return new WaitForSeconds(t - 2);
            GetComponent<Animator>().SetTrigger("Disappear");
        }
    }
}


public class Bomb : MonoBehaviour
{
    public float max_damage = 30;
    ushort health;
    AudioSource Destroyed;
    private ushort basehealth;
    public GameObject target;
    public Vector2 current_pos;

    private void Start()
    {
        health = gameObject.GetComponent<SetUpScript>().health;
        basehealth = health;
        Destroyed = gameObject.GetComponent<SetUpScript>().DestroyedSound;
    }

    private void FixedUpdate()
    {
        if (target != null && Vector2.Distance(transform.position, current_pos) < 1.5f)
        {
            explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if((collision.gameObject.layer == LayerMask.NameToLayer("PlayerObject") || collision.gameObject.layer == LayerMask.NameToLayer("EnemyObject")) && collision.gameObject.GetComponent<Collision>() == null)
        {
            Object.Destroy(collision.gameObject);
        }
        explode();
    }

    public void explode()
    {
        if (GetComponent<Renderer>().isVisible && transform.Find("Effects") != null)
        {
            GameObject effects = transform.Find("Effects").gameObject;
            effects.transform.SetParent(GameObject.Find("Storage").transform, false);
            effects.transform.position = transform.position;
            effects.GetComponent<ParticleSystem>().Play();
            Destroyed.Play();
            GameObject.Destroy(effects, effects.GetComponent<ParticleSystem>().main.duration + effects.GetComponent<ParticleSystem>().main.startLifetime.constant);
            foreach (var v in Physics2D.OverlapCircleAll(transform.position, 30))
            {
                if (!v.Equals(GetComponent<Collider2D>()))
                {
                    Collider2D shield = Physics2D.Raycast(transform.position, v.transform.position - transform.position, Vector2.Distance(transform.position, v.transform.position), LayerMask.GetMask("Shield")).collider;
                    if (shield != null)
                    {
                        if(shield == v)
                        {
                            Debug.Log("Shield found");
                            v.GetComponent<Collision>().Damage((uint)(max_damage/30 * 5 * (Mathf.Round(30 - v.Distance(GetComponent<Collider2D>()).distance) / 5)));
                        }
                        continue;
                    }

                    if ((v.GetComponent<BehaviorScript>()))
                    {
                        v.GetComponent<BehaviorScript>().onDamage((ushort)(max_damage/30 * 5 * (Mathf.Round(30 - v.Distance(GetComponent<Collider2D>()).distance)/5)), gameObject.layer);
                    }
                    else if(v.GetComponent<SpaceShipPlayer>())
                    {
                        v.GetComponent<SpaceShipPlayer>().Damage((uint)(max_damage/30 * 5 * (Mathf.Round(30 - v.Distance(GetComponent<Collider2D>()).distance)/5)));
                    }
                }
            }
        }
        GameObject.Destroy(gameObject);
    }
}

public class BigBomb : MonoBehaviour
{
    public float max_damage = 75;
    ushort health;
    AudioSource Destroyed;
    private ushort basehealth;
    public GameObject target;
    public Vector2 current_pos;

    private void Start()
    {
        health = gameObject.GetComponent<SetUpScript>().health;
        basehealth = health;
        Destroyed = gameObject.GetComponent<SetUpScript>().DestroyedSound;
    }

    private void FixedUpdate()
    {
        if (target != null && Vector2.Distance(transform.position, current_pos) < 1.5f)
        {
            explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.gameObject.layer == LayerMask.NameToLayer("PlayerObject") || collision.gameObject.layer == LayerMask.NameToLayer("EnemyObject")) && collision.gameObject.GetComponent<Collision>() == null)
        {
            Object.Destroy(collision.gameObject);
        }
        explode();
    }

    public void explode()
    {
        if (GetComponent<Renderer>().isVisible && transform.Find("Effects") != null)
        {
            GameObject effects = transform.Find("Effects").gameObject;
            effects.transform.SetParent(GameObject.Find("Storage").transform, false);
            effects.transform.position = transform.position;
            effects.GetComponent<ParticleSystem>().Play();
            Destroyed.Play();
            GameObject.Destroy(effects, effects.GetComponent<ParticleSystem>().main.duration + effects.GetComponent<ParticleSystem>().main.startLifetime.constant);
            foreach (var v in Physics2D.OverlapCircleAll(transform.position, 50))
            {
                if (!v.Equals(GetComponent<Collider2D>()))
                {
                    Collider2D shield = Physics2D.Raycast(transform.position, v.transform.position - transform.position, Vector2.Distance(transform.position, v.transform.position), LayerMask.GetMask("Shield")).collider;
                    if (shield != null)
                    {
                        if (shield == v)
                        {
                            Debug.Log("Shield found");
                            v.GetComponent<Collision>().Damage((uint)(max_damage / 50 * 5 * (Mathf.Round(50 - v.Distance(GetComponent<Collider2D>()).distance) / 5)));
                        }
                        continue;
                    }

                    if ((v.GetComponent<BehaviorScript>()))
                    {
                        if (v.GetComponent<Attacker>())
                        {
                            v.GetComponent<Attacker>().DisableAI(3);
                        }
                        v.GetComponent<BehaviorScript>().onDamage((ushort)(max_damage / 50 * 5 * (Mathf.Round(50 - v.Distance(GetComponent<Collider2D>()).distance) / 5)), gameObject.layer);
                    }
                    else if (v.GetComponent<SpaceShipPlayer>())
                    {
                        SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().StartCoroutine(v.GetComponent<SpaceShipPlayer>().StunPlayer(3));
                        v.GetComponent<SpaceShipPlayer>().Damage((uint)(max_damage / 50 * 5 * (Mathf.Round(50 - v.Distance(GetComponent<Collider2D>()).distance) / 5)));
                    }
                }
            }
        }
        GameObject.Destroy(gameObject);
    }
}

public class Electrizer : BehaviorScript
{
    private void Awake()
    {
        StartCoroutine(Damage_Closes());
    }

    private void Update()
    {
        if (Time.timeScale == 0)
        {
            if (GetComponent<AudioSource>().isPlaying) GetComponent<AudioSource>().Pause();
        }
        else if (!GetComponent<AudioSource>().isPlaying && health > 0) GetComponent<AudioSource>().Play();
    }

    IEnumerator Damage_Closes()
    {
        yield return new WaitForFixedUpdate();

        while(health > 0)
        {
            foreach(var v in close_entities)
            {
                if (v.GetComponentsInChildren<Transform>().Where(x => x.gameObject.layer == LayerMask.NameToLayer("Shield") && x.gameObject != v).Any()) continue;
                if(v.GetComponent<BehaviorScript>() != null)
                {
                    v.GetComponent<BehaviorScript>().onDamage(8, gameObject.layer);
                }
                else if(v.GetComponent<SpaceShipPlayer>() != null)
                {
                    v.GetComponent<SpaceShipPlayer>().Damage(8);
                }
                else if(v.layer == LayerMask.NameToLayer("Shield"))
                {
                    v.GetComponent<Collision>().Damage(8);
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    List<GameObject> close_entities = new();
    private void FixedUpdate()
    {
        if(health > 0)
        {
            close_entities = Physics2D.OverlapCircleAll(transform.position, 50).Select(x => x.gameObject).Where(x => x != gameObject && x.GetComponent<BehaviorScript>() != null || x.GetComponent<SpaceShipPlayer>() != null).ToList();
        }
        else if(GetComponent<AudioSource>().isPlaying && GetComponent<ParticleSystem>().isEmitting)
        {
            GetComponent<AudioSource>().Stop();
            GetComponent<ParticleSystem>().Stop();
        }
    }
}

public class Planet : BehaviorScript
{
    public bool killed_by_player;
    public void PlayerKilled()
    {
        if(!killed_by_player)
        {
            default_influence += 1;
            NotificationScript.AddNotification("TurretDestroyed");
            killed_by_player = false;
        }
    }

    private void Start()
    {
        _health = gameObject.GetComponent<SetUpScript>().health;
        basehealth = health;
        Hit = gameObject.GetComponent<SetUpScript>().HitSound;
        Destroyed = gameObject.GetComponent<SetUpScript>().DestroyedSound;
        influence = Random.Range(-5, 6) + default_influence;
        GetComponent<Rigidbody2D>().velocity = Vector2.left * SpaceShipPlayer.Speed / 2;
    }
    internal static int default_influence = 0;
    float influence;

    private void Awake()
    {
        GameObject base_obj = Resources.Load<GameObject>("Storage/Turret");
        if(Random.Range(0, 11) - 7 > 0)
        {
            GameObject first = new();
            first.transform.SetParent(transform, false);
            var obj = Object.Instantiate(base_obj, first.transform, true);
            obj.transform.localPosition = new Vector3(0, -20, 0);
            obj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            obj.GetComponent<SetUpScript>().Init();
            obj.GetComponent<PlanetDestroyer>().Activated = true;
        }
    }

    private bool used;

    public override void onDamage(ushort DamageAmount, LayerMask origin)
    {
        if (!GetComponent<SpriteRenderer>().isVisible) return;
        if (LayerMask.LayerToName(origin).Contains("Player"))
        {
            influence -= (float)DamageAmount/50;
        }

        health -= DamageAmount > health ? health : DamageAmount;
        if (health == 0)
        {
            if (origin == LayerMask.NameToLayer("PlayerObject") || origin == LayerMask.NameToLayer("CollidingPlayerObject"))
            {
                NotificationScript.AddNotification("PlanetDestroyed");
                default_influence -= 1;
            }

            int random_asteroid = Random.Range(4, 11);
            for(int i = 1; i <=random_asteroid; i++)
            {
                GameObject.Find("Storage").GetComponent<StorageManager>().create_asteroid(transform.position, Quaternion.Euler(0, 0, ((float)i / random_asteroid) * 360) * Vector3.up);
            }
            onDestroyed();
        }
        else
        {
            StartCoroutine(DamageEffect());
        }

    }

    private void FixedUpdate()
    {
        if(!used)
        {
            if(Vector2.Distance(SpaceShipPlayer.plr.transform.position, transform.position) <= GetComponent<SpriteRenderer>().bounds.size.x * 0.75f)
            {
                SpaceShipPlayer plr = SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>();

                if(influence >= 5)//good influence add health or energy
                {
                    GameObject.Find("Camera/SoundObjects/Bonus").GetComponent<AudioSource>().Play();
                    SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().AddRandomPowerUp();
                    NotificationScript.AddNotification("PowerUpScenario");
                }
                else if(influence >= 0)//small influence
                {
                    GameObject.Find("Camera/SoundObjects/Bonus").GetComponent<AudioSource>().Play();
                    plr.AddHealth((uint)(Random.Range(3, 6) * 5));
                    NotificationScript.AddNotification("HealthScenarioUp");
                }
                else if(influence >= -5)
                {
                    plr.Damage((uint)(Random.Range(1, 7) * 5));
                    NotificationScript.AddNotification("HealthScenarioDown");
                }
                else
                {
                    for(int i=0; i < 5; i++)
                    {
                        StorageManager.Storage.GetComponent<StorageManager>().create_portal(transform.position + Quaternion.Euler(0, 0, Random.Range(0, Random.Range(0, 360))) * transform.up * 40, 0.5f);
                    }
                    NotificationScript.AddNotification("EnemiesScenario");
                }
                used = true;
            }
        }
    }
}

public class Attacker : BehaviorScript
{
    protected bool AI = true;
    Coroutine current;

    public void DisableAI(float time)
    {
        if (!AI || time <= 0 || time > 10) return;

        if(current != null)
        {
            StopCoroutine(current);
        }
        current = StartCoroutine(ChangeAI(time));
    }

    private IEnumerator ChangeAI(float time)
    {
        AI = false;
        transform.Find("StunEffect").GetComponent<AudioSource>().Play();
        transform.Find("StunEffect").GetComponent<ParticleSystem>().Play();
        bool isFreezed = GetComponent<Rigidbody2D>().freezeRotation;
        GetComponent<Rigidbody2D>().freezeRotation = true;
        yield return new WaitForSeconds(time);
        AI = true;
        transform.Find("StunEffect").GetComponent<AudioSource>().Stop();
        transform.Find("StunEffect").GetComponent<ParticleSystem>().Stop();
        GetComponent<Rigidbody2D>().freezeRotation = isFreezed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            onDamage((ushort)collision.gameObject.GetComponent<ProjectileContainer>().Damage, collision.gameObject.layer);
            Object.Destroy(collision.gameObject);
            Search();
        }
        if(collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            changeGoal(collision.collider, true);
        }
    }

    protected (int min, int max) distances;
    protected bool aligned = false;
    protected bool locked = false;
    protected bool check_enemies = true;
    protected GameObject target = SpaceShipPlayer.plr;
    public Vector3 goal { get; protected set; }
    protected Vector3 goal_step;
    protected Vector3 base_goal;
    public bool changing_goal { get; protected set; } = true;

    Coroutine recharge;

    IEnumerator RestoreHealth(GameObject healer)
    {
        while (health != basehealth && AI && healer != null && Vector2.Distance(healer.transform.position, transform.position) <= 50)
        {
            health = health + basehealth / 20 > basehealth ? basehealth : (ushort)(health + basehealth / 20);
            yield return new WaitForSeconds(0.5f);
        }
    }

    void Back() // Go to the right fast
    {
        GetComponent<Rigidbody2D>().velocity = Vector2.right * SpaceShipPlayer.Speed * 2;
    }

    (bool, GameObject) check_front_enemy(int distance) // check if an enemy is in front of gameobject
    {
        Debug.DrawLine(transform.position + transform.up.normalized * (GetComponent<Collider2D>().bounds.size.y + 1), transform.position + transform.up.normalized * distance);
        Collider2D result = Physics2D.Raycast(transform.position + transform.up.normalized * (GetComponent<Collider2D>().bounds.size.y + 1), transform.up.normalized, distance, LayerMask.GetMask("Enemy")).collider;
        return (result != null && !result.gameObject.CompareTag("Projectile"), result != null ? result.gameObject : gameObject);
    }

    (bool, GameObject) check_front_enemy(Vector3 base_goal, int distance) // check if an enemy is in front of gameobject if it was at a specific coordinate
    {
        Debug.DrawLine(base_goal + transform.up * (GetComponent<Collider2D>().bounds.size.y + 1), transform.position + transform.up * distance);
        Collider2D result = Physics2D.Raycast(base_goal + transform.up * (GetComponent<Collider2D>().bounds.size.y + 1), target.transform.position - base_goal, distance, LayerMask.GetMask("Enemy")).collider;
        return (result != null && !result.gameObject.CompareTag("Projectile"), result != null ? result.gameObject : gameObject);
    }

    protected void changeGoal(Collider2D collider, bool back = false)
    {
        (bool left, bool right) = check_side_enemy(GetComponent<Collider2D>());
        (bool leftconfirm, bool rightconfirm) = GetComponent<Collider2D>() != collider ? check_side_enemy(collider) : (left, right);
        if ((leftconfirm && left) && (rightconfirm && right))
        {
            if (base_goal.y - goal.y > 0 && (!check_enemies || !check_front_enemy(target.transform.position + goal + new Vector3(0, 30, 0), distances.max).Item1)) // if going up is closer to base_goal
            {
                goal += new Vector3(0, 30, 0);
            }
            else if (base_goal.y - goal.y < 0 && (!check_enemies || !check_front_enemy(target.transform.position + goal + new Vector3(0, -30, 0), distances.max).Item1)) // if going down is closer to base_goal
            {
                goal += new Vector3(0, -30, 0);
            }
            else if (goal == base_goal)
            {
                goal += new Vector3(0, 30, 0) * (Random.Range(0, 2) * 2 - 1);
            }
            else if(collider.transform.position.y > transform.position.y)
            {
                goal += new Vector3(0, -30, 0);
            }
            else if (collider.transform.position.y < transform.position.y)
            {
                goal += new Vector3(0, 30, 0);
            }
            else if(back)
            {
                goal_step = goal + new Vector3(30, 0);
            }
        }
        else if ((leftconfirm && left) || (rightconfirm && right)) // if at least one of the direction is available
        {
            goal += new Vector3(0, 30, 0) * (System.Convert.ToInt32(rightconfirm && right) - System.Convert.ToInt32(leftconfirm && left));
        }
        else if (collider != GetComponent<Collider2D>() && back)
        {
            if(collider.transform.position.y > transform.position.y && right)
            {
                goal += new Vector3(0, 30, 0);
            }
            else if (collider.transform.position.y < transform.position.y && left)
            {
                goal += new Vector3(0, -30, 0);
            }
            else
            {
                goal_step = goal + new Vector3(30, 0);
            }
        }
    }

    (bool left, bool right) check_side_enemy(Collider2D collider)
    {
        Debug.DrawLine(collider.gameObject.transform.position + Vector3.down * (collider.bounds.size.x + 1), collider.gameObject.transform.position + Vector3.down * 40);
        Debug.DrawLine(collider.gameObject.transform.position + Vector3.up * (collider.bounds.size.x + 1), collider.gameObject.transform.position + Vector3.up * 40);
        return (Physics2D.Raycast(collider.gameObject.transform.position + Vector3.down * (collider.bounds.size.x + 1), Vector2.down, 50, LayerMask.GetMask("Enemy")).collider == null, Physics2D.Raycast(collider.gameObject.transform.position + Vector3.up * (collider.bounds.size.x + 1), Vector2.up, 50, LayerMask.GetMask("Enemy")).collider == null);
    }

    private void LateUpdate()
    {
        if (transform.Find("HealthBar"))
        {
            transform.Find("HealthBar").rotation = Quaternion.Euler(0, 0, gameObject.transform.rotation.eulerAngles.x);
        }
        if(!AI && Time.timeScale == 0 && transform.Find("StunEffect").GetComponent<AudioSource>().isPlaying)
        {
            transform.Find("StunEffect").GetComponent<AudioSource>().Pause();
        }
        else if(!AI && Time.timeScale != 0 && !transform.Find("StunEffect").GetComponent<AudioSource>().isPlaying)
        {
            transform.Find("StunEffect").GetComponent<AudioSource>().Play();
        }
    }

    void Goal() // Will go to the position it wants in priority
    {
        (bool result, GameObject instance) infos = check_front_enemy(30);
        if (infos.result && check_enemies)
        {
            if (infos.instance.GetComponent<Attacker>() != null)
            {
                if (Mathf.Abs(infos.instance.GetComponent<Attacker>().goal.y - goal.y) < 5 && !infos.instance.GetComponent<Attacker>().changing_goal)
                {
                    changeGoal(infos.instance.GetComponent<Collider2D>());
                }
            }
            else if (infos.instance.GetComponent<Boss>() != null && goal_step == Vector3.zero)
            {
                goal_step = transform.position + Vector3.up * 50 * (infos.instance.transform.position.y - transform.position.y < 0 ? -1 : 1) + Vector3.left * 20 - target.transform.position;
            }
        }
        Vector2 direction = (Vector2)(target.transform.position + (goal_step != Vector3.zero ? goal_step : goal) - transform.position);
        if(GetComponent<Rigidbody2D>().velocity != direction.normalized * (SpaceShipPlayer.Speed + 10))
        {
            GetComponent<Rigidbody2D>().velocity = direction.normalized * (SpaceShipPlayer.Speed + 10);
        }
        if(goal_step != Vector3.zero)
        {
            if((float)Vector2.Distance(transform.position, target.transform.position + goal_step) < 1)
            {
                goal_step = Vector3.zero;
            }
        }
        else if ((float)Vector2.Distance(transform.position, target.transform.position + goal) < 1)
        {
            changing_goal = false;
        }
    }

    internal void check_for_healers(GameObject healer)
    {
        if(recharge == null && health != basehealth && Vector2.Distance(healer.transform.position, transform.position) <= 50 && healer.GetComponent<Healer>() != null)
        {
            recharge = StartCoroutine(RestoreHealth(healer));
        }
    }

    protected void Search(bool remove_velocity = true)
    {
        if (((remove_velocity && !changing_goal) || !AI) && gameObject.GetComponent<Rigidbody2D>().velocity != Vector2.zero)
        {
            gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        int distance = (int)Vector2.Distance(transform.position, target.transform.position);
        if(!locked && AI)
        {
            Vector2 direction = target.transform.position - transform.position;
            transform.rotation = Quaternion.Euler(Vector3.forward * Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) * Quaternion.Euler(0, 0, -90);

            aligned = false;

            if (!gameObject.GetComponent<Renderer>().isVisible) // if enemy is not visible at camera
            {
                if(target.transform.position.x <= transform.position.x)
                {
                    goal = base_goal;
                    changing_goal = true;
                }
                else
                {
                    changing_goal = false;
                }
            }

            if (changing_goal) // when moving
            {
                Goal();
            }
            else if(distance < distances.min || target.transform.position.x > transform.position.x) // in case the enemy is too close to the player or behind him
            {
                Back();
            }
            else if(distance > distances.max) // if the player is too far away
            {
                changing_goal = true;
                Goal();
            }
            else // when the enemy is at a good distance from the player
            {
                (bool, GameObject) enemy = check_front_enemy(distances.max);
                if (Mathf.Abs(goal.y) > 50) // if the enemy is too hard to shoot for the player
                {
                    goal = base_goal;
                    changing_goal = true;
                }
                else if(enemy.Item1 && check_enemies) // if there's an enemy in front of the object
                {
                    changing_goal = true;
                    if(Vector2.Distance(SpaceShipPlayer.plr.transform.position + goal, transform.position + goal) > 1)
                    {
                        changeGoal(enemy.Item2.GetComponent<Collider2D>());
                    }
                }
                else
                {
                    Vector3 current_goal = goal;
                    changeGoal(GetComponent<Collider2D>());
                    if (current_goal != goal && Mathf.Abs(goal.y) < Mathf.Abs(current_goal.y) && current_goal != base_goal && !check_front_enemy(goal, distances.max).Item1) // check if the enemy can go closer to the player
                    {
                        changing_goal = true;
                    }
                    else
                    {
                        goal = current_goal;
                        if (Physics2D.Raycast(transform.position, transform.up, distances.max, LayerMask.GetMask("Player")).collider != null) // if the player is in front of him
                        {
                            aligned = true;
                        }
                    }
                }
            }
        }
    }
}

public class Laser : Attacker
{
    bool attacking = false;
    bool update = true;
    GameObject BaseLaser;

    private void FixedUpdate()
    {
        if(update)
        {
            BaseLaser = Resources.Load<GameObject>("Laser");
            update = false;
            distances = (70, 130);
            goal = base_goal = new Vector3(100, 0, 0);
        }
        Search();

        if(!attacking && aligned && AI)
        {
            locked = true;
            attacking = true;
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        for(int i = 1; i <= 5 + (SpaceShipPlayer.Speed/50)/5; i++)
        {
            transform.Find("FireSound").gameObject.GetComponent<AudioSource>().Play();
            GameObject Laser = Object.Instantiate(BaseLaser, transform.position + transform.up * 10, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90));
            Laser.transform.SetParent(StorageManager.Storage.transform);
            Laser.GetComponent<Rigidbody2D>().velocity = transform.up * SpaceShipPlayer.Speed * 2;
            Laser.AddComponent<ProjectileContainer>();
            Laser.GetComponent<ProjectileContainer>().Damage = 5 + (SpaceShipPlayer.Speed/50)/4;
            Laser.tag = "Projectile";
            Laser.layer = LayerMask.NameToLayer("EnemyObject");
            Object.Destroy(Laser, 5);
            yield return new WaitForSeconds(0.25f);
            if (!AI) break;
        }
        locked = false;
        if(AI)
        {
            yield return new WaitForSeconds(3);
            attacking = false;
        }
    }
}

public class Neutralizer : Attacker
{
    bool attacking = false;
    bool update = true;
    GameObject BaseStunLaser;
    GameObject BaseLaser;
    float t;

    private void FixedUpdate()
    {
        if (update)
        {
            BaseStunLaser = Resources.Load<GameObject>("MiniLaser");
            BaseLaser = Resources.Load<GameObject>("Laser");
            update = false;
            distances = (70, 130);
            goal = base_goal = new Vector3(100, 0, 0);
        }

        Search();

        if(!attacking && AI && aligned)
        {
            t += Time.fixedDeltaTime;
            if(t > 1.5f)
            {
                attacking = true;
                StartCoroutine(Attack());
            }
        }
    }
    public override void onAttackCollide(GameObject obj, Collider2D collision, Collision_Type c_type)
    {
        if(collision.gameObject == SpaceShipPlayer.plr)
        {
            collision.gameObject.GetComponent<SpaceShipPlayer>().Damage(10);
            collision.gameObject.GetComponent<SpaceShipPlayer>().StartCoroutine(collision.gameObject.GetComponent<SpaceShipPlayer>().StunPlayer(3));
        }
        Object.Destroy(obj);
    }

    IEnumerator Attack()
    {
        for(int i = 0; i < 3; i++)
        {
            if (!AI) break;
            GameObject clone = Object.Instantiate(BaseStunLaser, transform.position + transform.up * 10, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z));
            clone.transform.localScale = new Vector3(20, 40, 1);
            clone.GetComponent<SpriteRenderer>().color = Color.blue;
            clone.AddComponent<Collision>();
            clone.GetComponent<Collision>().init(onAttackCollide);
            clone.GetComponent<Rigidbody2D>().velocity = transform.up * SpaceShipPlayer.Speed * 2;
            Object.Destroy(clone, 10);

            yield return new WaitForSeconds(0.5f);
        }
        for (int i = 0; i < 6; i++)
        {
            if (!AI) break;
            transform.Find("FireSound").gameObject.GetComponent<AudioSource>().Play();
            GameObject Laser = Object.Instantiate(BaseLaser, transform.position + transform.up * 10, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90));
            Laser.transform.SetParent(StorageManager.Storage.transform);
            Laser.GetComponent<Rigidbody2D>().velocity = transform.up * SpaceShipPlayer.Speed * 3;
            Laser.AddComponent<ProjectileContainer>();
            Laser.GetComponent<ProjectileContainer>().Damage = 10 + (SpaceShipPlayer.Speed / 50) / 2;
            Laser.tag = "Projectile";
            Laser.layer = LayerMask.NameToLayer("EnemyObject");
            Object.Destroy(Laser, 5);
            yield return new WaitForSeconds(0.25f);
            if (!AI) break;
        }
        yield return new WaitForSeconds(1);
        attacking = false;
        t = 0;
    }
}

public class Astroyer : Attacker
{
    bool attacking = false;
    bool update = true;
    GameObject BaseAttack;

    private void FixedUpdate()
    {
        if (update)
        {
            BaseAttack = Resources.Load<GameObject>("LargeAttack");
            update = false;
            distances = (100, 150);
            goal = base_goal = new Vector3(120, 0, 0);
        }

        Search();

        if (aligned && !attacking && AI) StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
        attacking = true;

        GetComponent<AudioSource>().Play();
        GameObject clone = Object.Instantiate(BaseAttack, transform.position + transform.up * 10, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + 90));
        clone.transform.SetParent(StorageManager.Storage.transform);
        clone.GetComponent<Rigidbody2D>().velocity = transform.up * SpaceShipPlayer.Speed * 5;
        clone.AddComponent<Collision>();
        clone.GetComponent<Collision>().init(onAttackCollide, SignalType.Beginning);
        clone.transform.LeanScale(new Vector3(7.5f, 7.5f, 1), 0.2f);
        clone.layer = LayerMask.NameToLayer("EnemyObject");
        Object.Destroy(clone, 5);

        yield return new WaitForSeconds(1.5f);
        attacking = false;
    }

    public override void onAttackCollide(GameObject obj, Collider2D collision, Collision_Type c_type)
    {
        if(c_type == Collision_Type.Trigger)
        {
            if(collision.gameObject.layer == LayerMask.NameToLayer("Shield"))
            {
                collision.gameObject.GetComponent<Collision>().Damage(20 + SpaceShipPlayer.Speed / 100);
                Object.Destroy(obj);
                return;
            }

            if(collision.gameObject.GetComponent<SpaceShipPlayer>() != null)
            {
                collision.gameObject.GetComponent<SpaceShipPlayer>().Damage(20 + SpaceShipPlayer.Speed/100);
            }
            else if (collision.gameObject.GetComponent<BehaviorScript>() != null)
            {
                collision.gameObject.GetComponent<BehaviorScript>().onDamage((ushort)(20 + SpaceShipPlayer.Speed / 100), gameObject.layer);
            }
        }
    }
}

public class Interceptor : Attacker
{
    bool attacking = false;
    bool update = true;
    GameObject BaseLaser;
    float t = 0;

    private void FixedUpdate()
    {
        if (update)
        {
            BaseLaser = Resources.Load<GameObject>("MiniLaser");
            update = false;
            distances = (70, 130);
            goal = base_goal = new Vector3(100, 0, 0);
        }
        Search();

        if (!attacking && aligned && AI)
        {
            t += Time.fixedDeltaTime;
            if(t > 3)
            {
                attacking = true;
                StartCoroutine(Attack());
            }
        }
    }

    IEnumerator Attack()
    {
        Vector3 decalage = Vector3.up * 3;
        for (int i = 1; i <= 30 + (SpaceShipPlayer.Speed / 50) / 5; i++)
        {
            transform.Find("FireSound").gameObject.GetComponent<AudioSource>().Play();
            GameObject Laser = Object.Instantiate(BaseLaser, transform.position + transform.up * 10 + decalage, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z));
            Laser.transform.SetParent(StorageManager.Storage.transform);
            Laser.GetComponent<Rigidbody2D>().velocity = transform.up * SpaceShipPlayer.Speed * 4;
            Laser.AddComponent<ProjectileContainer>();
            Laser.GetComponent<ProjectileContainer>().Damage = 4 + (SpaceShipPlayer.Speed / 50) / 5;
            Laser.tag = "Projectile";
            Laser.layer = LayerMask.NameToLayer("EnemyObject");
            decalage = -decalage;
            Object.Destroy(Laser, 5);
            yield return new WaitForSeconds(0.1f);
            if (!AI) break;
        }
        if (AI)
        {
            yield return new WaitForSeconds(3);
            attacking = false;
        }
        t = 0;
    }
}

public class Bomber : Attacker
{
    float t = 0;
    Vector3 current_pos = Vector3.zero;
    bool update = true;
    static GameObject BaseMissile;

    private void FixedUpdate()
    {
        if (update)
        {
            BaseMissile = Resources.Load<GameObject>("Missile");
            update = false;
            distances = (100, 150);
            goal = base_goal = new Vector3(125, 0, 0);
        }
        Search();

        if (aligned)
        {
            if(!AI)
            {
                t = 0;
                return;
            }
            if(current_pos != Vector3.zero)
            {;
                t += Time.fixedDeltaTime;
                if(Vector2.Distance(target.transform.position, current_pos) > 20)
                {
                    current_pos = Vector3.zero;
                }
                else if(t > 2)
                {
                    current_pos = Vector3.zero;
                    Attack();
                }
            }
            else
            {
                current_pos = target.transform.position;
                t = 0;
            }
        }
    }


    void Attack()
    {
        transform.Find("FireSound").gameObject.GetComponent<AudioSource>().Play();
        Debug.Log(((GetComponent<Collider2D>().bounds.size.y + BaseMissile.GetComponent<Collider2D>().bounds.size.y) * 2));
        GameObject Missile = Object.Instantiate(BaseMissile, transform.position + transform.up * ((GetComponent<Collider2D>().bounds.size.y + BaseMissile.GetComponent<Collider2D>().bounds.size.y) *2), Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z));
        Missile.transform.SetParent(StorageManager.Storage.transform);
        Missile.GetComponent<Rigidbody2D>().velocity = transform.up * (SpaceShipPlayer.Speed * 2.5f);
        Missile.GetComponent<SetUpScript>().Init();
        Missile.GetComponent<Bomb>().max_damage += (SpaceShipPlayer.Speed - 50) * 2;
        Missile.GetComponent<Bomb>().target = target;
        Missile.GetComponent<Bomb>().current_pos = target.transform.position;
        Object.Destroy(Missile, 10);
    }
}

public class Destroyer : Attacker
{
    bool attacking;
    float t = 0;
    Vector3 current_pos = Vector3.zero;
    bool update = true;
    static GameObject BaseMissile;

    private void Update()
    {
        if (Time.timeScale == 0)
        {
            if (transform.Find("Beam").GetComponent<AudioSource>().isPlaying)
            {
                transform.Find("Beam").GetComponent<AudioSource>().Pause();
            }
        }
        else if (!transform.Find("Beam").GetComponent<AudioSource>().isPlaying && transform.Find("Beam").GetComponent<LineRenderer>().enabled)
        {
            transform.Find("Beam").GetComponent<AudioSource>().Play();
        }
    }

    private void FixedUpdate()
    {
        if (update)
        {
            BaseMissile = Resources.Load<GameObject>("Missile");
            update = false;
            distances = (100, 150);
            goal = base_goal = new Vector3(125, 0, 0);
        }

        Search();
        if(aligned)
        {
            t += Time.fixedDeltaTime;
            if(t > 1 && !attacking)
            {
                StartCoroutine(Attack());
            }
        }
    }


    List<GameObject> entities = new();
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.GetComponent<BehaviorScript>() != null || collision.GetComponent<SpaceShipPlayer>() != null || collision.gameObject.layer == LayerMask.NameToLayer("Shield")) && !collision.transform.GetComponentsInChildren<Transform>().Where(x => x.gameObject != collision.gameObject && x.gameObject.layer == LayerMask.NameToLayer("Shield")).Any())
        {
            entities.Add(collision.gameObject);
            StartCoroutine(LaserDamage(collision.gameObject));
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(entities.Contains(collision.gameObject))
        {
            entities.Remove(collision.gameObject);
        }
    }

    IEnumerator LaserDamage(GameObject obj)
    {
        while(entities.Contains(obj))
        {
            if(obj.GetComponent<SpaceShipPlayer>() != null)
            {
                obj.GetComponent<SpaceShipPlayer>().Damage(4);
            }
            else if(obj.GetComponent<BehaviorScript>() != null)
            {
                obj.GetComponent<BehaviorScript>().onDamage(4, gameObject.layer);
            }
            else if(obj.layer == LayerMask.NameToLayer("Shield"))
            {
                obj.GetComponent<Collision>().Damage(4);
            }
            yield return new WaitForSeconds(0.2f); // damage up to 20pts per seconds
        }
    }

    IEnumerator Attack()
    {
        attacking = true;
        locked = true;
        Vector3 current_target_pos = target.transform.position;
        yield return new WaitForSeconds(0.5f);
        for (int i=0; i < 2; i++)
        {
            if(AI)
            {
                Vector3 decalage = transform.right * GetComponent<Collider2D>().bounds.size.x / 4 * (i * 2 - 1);
                transform.Find("FireSound").GetComponent<AudioSource>().Play();
                GameObject Missile = Object.Instantiate(BaseMissile, transform.position + transform.up * ((GetComponent<Collider2D>().bounds.size.y * 0.4f + BaseMissile.GetComponent<Collider2D>().bounds.size.y) * 2) + decalage, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z));
                Missile.transform.SetParent(StorageManager.Storage.transform);
                Missile.GetComponent<Rigidbody2D>().velocity = transform.up * (SpaceShipPlayer.Speed * 2.5f);
                Missile.GetComponent<SetUpScript>().Init();
                Missile.GetComponent<Bomb>().max_damage = 50 + (SpaceShipPlayer.Speed - 50)/2;
                Missile.GetComponent<Bomb>().target = target;
                Missile.GetComponent<Bomb>().current_pos = current_target_pos + Quaternion.AngleAxis(-transform.rotation.z, transform.right) * decalage;
                Debug.Log(Quaternion.AngleAxis(-transform.rotation.z, transform.right) * decalage);
                Object.Destroy(Missile, 10);
                yield return new WaitForSeconds(0.75f);
            }
        }
        locked = false;
        yield return new WaitForSeconds(2);
        if (AI && aligned)
        {
            locked = true;
            Vector3 base_beam_position = new Vector3(0, 1.1f, 0);
            //activate laser
            Transform Beam = transform.Find("Beam");
            Beam.localPosition = base_beam_position;
            Beam.GetComponent<ParticleSystem>().Play();
            yield return new WaitForSeconds(1);

            Beam.GetComponent<LineRenderer>().enabled = true;
            Beam.GetComponent<AudioSource>().Play();
            Beam.GetComponent<PolygonCollider2D>().enabled = true;
            Vector3 decalage = transform.right * Beam.GetComponent<LineRenderer>().startWidth / transform.localScale.x;
            Beam.GetComponent<LineRenderer>().SetPositions(new Vector3[] { Vector3.zero, Vector3.up * 500 });
            Beam.GetComponent<PolygonCollider2D>().points = new Vector2[] { decalage, -decalage, -decalage + Vector3.up * 500, decalage + Vector3.up * 500 };

            float rate = 12f * Time.fixedDeltaTime;
            for (float i = 0; i < 4; i += Time.fixedDeltaTime)
            {
                if (!AI) break;
                float z_axis = Quaternion.LookRotation((Vector2)(target.transform.position - transform.position)).eulerAngles.x - transform.rotation.eulerAngles.z + 90;
                z_axis += Mathf.Abs(z_axis - transform.rotation.z) > 180 ? z_axis - transform.rotation.z > 0 ? -360 : 360: 0;
                transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + Mathf.Clamp(z_axis, -rate, rate));

                yield return new WaitForFixedUpdate();
            }

            entities = new();
            Beam.GetComponent<AudioSource>().Stop();
            Beam.GetComponent<PolygonCollider2D>().enabled = false;
            Beam.GetComponent<LineRenderer>().enabled = false;
            Beam.GetComponent<ParticleSystem>().Stop();
            locked = false;
            yield return new WaitForSeconds(2);
        }
        yield return new WaitForSeconds(2);
        t = 0;
        attacking = false;
    }
}

public class Drone : Attacker
{
    bool attacking;
    float max_damage = 100;
    float t = 0;
    Vector3 current_pos = Vector3.zero;
    bool update = true;

    private void Update()
    {
        if (Time.timeScale == 0)
        {
            if (transform.Find("Engine").GetComponent<AudioSource>().isPlaying)
            {
                transform.Find("Engine").GetComponent<AudioSource>().Pause();
            }

            if (transform.Find("Effects").GetComponent<AudioSource>().isPlaying) transform.Find("Effects").GetComponent<AudioSource>().Stop();
        }
        else if (!transform.Find("Engine").GetComponent<AudioSource>().isPlaying && attacking)
        {
            transform.Find("Engine").GetComponent<AudioSource>().Play();
        }
    }

    private void FixedUpdate()
    {
        if (update)
        {
            update = false;
            distances = (50, 100);
            goal = base_goal = new Vector3(75, 0, 0);
        }

        Search(!locked);

        if (!attacking)
        {
            if (Vector2.Distance(target.transform.position, transform.position) < 25)
            {
                attacking = true;
                locked = true;
                GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                transform.Find("Effects").GetComponent<AudioSource>().Play();
                Invoke("explode", 1);
            }
            else if (aligned)
            {
                t += Time.fixedDeltaTime;
                if(t > 1)
                {
                    StartCoroutine(go_to_target());
                }
            }
            else
            {
                t = 0;
            }
        }
        else if(t != 0)
        {
            t = 0;
        }
    }

    IEnumerator go_to_target()
    {
        attacking = true;
        locked = true;
        Vector3 goal_position = target.transform.position;
        float attack_t = 0;
        while(attack_t <= 5 && Vector2.Distance(transform.position, goal_position) > 25 && Vector2.Distance(target.transform.position, transform.position) > 25 && AI)
        {
            GetComponent<Rigidbody2D>().velocity = transform.up * SpaceShipPlayer.Speed * 6;

            attack_t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        locked = false;
        attacking = false;
        t = 0;
    }

    void explode()
    {
        if(!AI)
        {
            transform.Find("Effects").GetComponent<AudioSource>().Stop();
            locked = false;
            return;
        }
        if (GetComponent<Renderer>().isVisible && transform.Find("Effects") != null)
        {
            GameObject effects = transform.Find("Effects").gameObject;
            effects.transform.SetParent(GameObject.Find("Storage").transform, false);
            effects.transform.position = transform.position;
            effects.GetComponent<ParticleSystem>().Play();
            effects.GetComponent<AudioSource>().Stop();
            Destroyed.Play();
            GameObject.Destroy(effects, effects.GetComponent<ParticleSystem>().main.duration + effects.GetComponent<ParticleSystem>().main.startLifetime.constant);
            foreach (var v in Physics2D.OverlapCircleAll(transform.position, 50))
            {
                if (!v.Equals(GetComponent<Collider2D>()))
                {
                    Collider2D shield = Physics2D.Raycast(transform.position, v.transform.position - transform.position, Vector2.Distance(transform.position, v.transform.position), LayerMask.GetMask("Shield")).collider;
                    if (shield != null)
                    {
                        if (shield == v)
                        {
                            v.GetComponent<Collision>().Damage((uint)(max_damage / 50 * 5 * (Mathf.Round(50 - v.Distance(GetComponent<Collider2D>()).distance) / 5)));
                        }
                        continue;
                    }

                    if ((v.GetComponent<BehaviorScript>()))
                    {
                        v.GetComponent<BehaviorScript>().onDamage((ushort)(max_damage / 50 * 5 * (Mathf.Round(50 - v.Distance(GetComponent<Collider2D>()).distance) / 5)), gameObject.layer);
                    }
                    else if (v.GetComponent<SpaceShipPlayer>())
                    {
                        v.GetComponent<SpaceShipPlayer>().Damage((uint)(max_damage / 50 * 5 * (Mathf.Round(50 - v.Distance(GetComponent<Collider2D>()).distance) / 5)));
                    }
                }
            }
        }
        GameObject.Destroy(gameObject);
    }
}

public class Fighter : Attacker
{
    float t = 0;
    int t_goal;
    bool update = true;
    bool attacking = false;
    bool touched;

    private void reset_time()
    {
        t_goal = Random.Range(1, 4);
    }

    private void FixedUpdate()
    {
        if (update)
        {
            reset_time();
            update = false;
            distances = (50, 100);
            goal = base_goal = new Vector3(60, 0, 0);
        }
        Search(!locked);

        if (!attacking)
        {
            if (aligned)
            {
                t += Time.fixedDeltaTime;
                if (t >= t_goal)
                {
                    StartCoroutine(Attack());
                    reset_time();
                }
            }
            else if (t != 0)
            {
                t = 0;
            }
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0 && transform.Find("Engine").GetComponent<AudioSource>().isPlaying)
        {
            transform.Find("Engine").GetComponent<AudioSource>().Pause();
        }
        else if (Time.timeScale != 0 && transform.Find("Effect").GetComponent<ParticleSystem>().isEmitting && !transform.Find("Engine").GetComponent<AudioSource>().isPlaying)
        {
            transform.Find("Engine").GetComponent<AudioSource>().Play();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            onDamage((ushort)collision.gameObject.GetComponent<ProjectileContainer>().Damage, gameObject.layer);
            Object.Destroy(collision.gameObject);
        }
        if (attacking && collision.gameObject != null)
        {
            if(collision.gameObject == SpaceShipPlayer.plr)
            {
                collision.gameObject.GetComponent<SpaceShipPlayer>().Damage(50);
                onDamage(10, gameObject.layer);
                touched = true;
            }
            else if(collision.gameObject.layer == LayerMask.NameToLayer("Shield"))
            {
                collision.gameObject.GetComponent<Collision>().Damage(50);
                onDamage(10, gameObject.layer);
                touched = true;
            }
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            changeGoal(collision.collider, true);
        }
    }

    IEnumerator Attack()
    {
        attacking = true;
        transform.Find("Engine").GetComponent<AudioSource>().Play();
        transform.Find("Effect").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(1.5f);
        locked = true;
        transform.Find("Effect").GetComponent<TrailRenderer>().emitting = true;
        for(float i=0; i<1; i += Time.fixedDeltaTime)
        {
            GetComponent<Rigidbody2D>().velocity = transform.up * SpaceShipPlayer.Speed * 4;
            if(touched || !AI)
            {
                touched = false;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        transform.Find("Effect").GetComponent<ParticleSystem>().Stop();
        transform.Find("Effect").GetComponent<TrailRenderer>().emitting = false;
        transform.Find("Engine").GetComponent<AudioSource>().Stop();
        attacking = false;
        locked = false;
    }
}

public class Caller : Attacker
{
    int energyhealth = 100;
    int max_energyhealth = 100;
    bool shieldEnabled;
    Coroutine regen;
    GameObject base_shield;
    Coroutine enemy_call;

    private void Update()
    {
        if(Time.timeScale == 0)
        {
            if(shieldEnabled && transform.Find("Shield/Sound").GetComponent<AudioSource>().isPlaying) transform.Find("Shield/Sound").GetComponent<AudioSource>().Pause();
        }
        else if(shieldEnabled && !transform.Find("Shield/Sound").GetComponent<AudioSource>().isPlaying)
        {
            transform.Find("Shield/Sound").GetComponent<AudioSource>().UnPause();
        }
    }

    public override void onDamage(ushort DamageAmount, LayerMask origin)
    {
        if (regen != null) StopCoroutine(regen);
        regen = StartCoroutine(ShieldCharge());

        if(shieldEnabled)
        {
            int e = energyhealth;
            energyhealth -= DamageAmount > energyhealth ? energyhealth : DamageAmount;
            if(energyhealth == 0)
            {
                base.onDamage((ushort)((DamageAmount - e) / 2), origin);
                DisableShield();
            }
            Hit.Play();
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            return;
        }
        base.onDamage(DamageAmount, origin);
    }

    bool update = true;
    private void FixedUpdate()
    {
        if (update)
        {
            base_shield = Resources.Load<GameObject>("Shield");
            EnableShield();
            update = false;
            distances = (150, 200);
            goal = base_goal = new Vector3(175, 0, 0);
        }
        Search();
    }

    void EnableShield()
    {
        GameObject clone = Object.Instantiate(base_shield, transform);
        Object.Destroy(clone.GetComponent<Rigidbody2D>());
        clone.transform.localScale = new Vector3(0.5f, 0.5f, 1);
        clone.name = "Shield";
        shieldEnabled = true;
        enemy_call = StartCoroutine(SpawnEnemies());
    }
    void DisableShield()
    {
        Object.Destroy(transform.Find("Shield").gameObject);
        shieldEnabled = false;
        StopCoroutine(enemy_call);
    }

    IEnumerator ShieldCharge()
    {
        yield return new WaitForSeconds(4);
        while(max_energyhealth > energyhealth)
        {
            energyhealth += energyhealth + 20 > max_energyhealth ? max_energyhealth - energyhealth : 20;
            if(energyhealth > max_energyhealth / 4 && !shieldEnabled)
            {
                EnableShield();
            }
            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator SpawnEnemies()
    {
        while(true)
        {
            yield return new WaitForSeconds(12.5f);
            locked = true;
            for(int i= 0; i < 3; i++)
            {
                GameObject.Find("Storage").GetComponent<StorageManager>().create_portal(transform.position + Quaternion.Euler(0, 0, Random.Range(0, 360)) * Vector3.left * 50, 1.5f);
                yield return new WaitForSeconds(0.25f);
            }
            locked = false;
        }
    }
}

public class Healer : Attacker
{
    bool update = true;
    bool laser_enabled = false;

    private void FixedUpdate()
    {
        if(update)
        {
            check_enemies = false;
            distances = (10, 500);
            goal = base_goal = new(150, 0);
            update = false;
        }
        if (target == null)
        {
            target = SpaceShipPlayer.plr;
            goal = base_goal = new(200, 0);
            distances = (10, 500);
        }
        else Search();

        if(!AI && GetComponent<LineRenderer>().enabled)
        {
            GetComponent<LineRenderer>().enabled = false;
        }
        else if(AI && laser_enabled && !GetComponent<LineRenderer>().enabled)
        {
            GetComponent<LineRenderer>().enabled = true;
        }

        if (AI && aligned)
        {
            IEnumerable<GameObject> potential_help = Physics2D.OverlapCircleAll(transform.position, 500).Select(x => x.gameObject).Where(x => x.GetComponent<Attacker>() is not null && x.GetComponent<Healer>() == null).OrderBy(x => (float)x.GetComponent<BehaviorScript>().health/x.GetComponent<BehaviorScript>().basehealth);

            if (potential_help.Any() && (target == SpaceShipPlayer.plr || (target != null || (float)target.GetComponent<BehaviorScript>().health/target.GetComponent<BehaviorScript>().basehealth > 0.8f)) && potential_help.First() != target)
            {
                target = potential_help.First();
                goal = base_goal = new(30, 0);
                distances = (10, 50);
                goal_step = transform.position.x > target.transform.position.x ? goal_step : new(0, 30, 0);
                changing_goal = true;
            }
        }

        if(Vector2.Distance(target.transform.position, transform.position) <= 50)
        {
            if(target != SpaceShipPlayer.plr)
            {
                if (!laser_enabled)
                {
                    laser_enabled = true;
                    GetComponent<LineRenderer>().enabled = true;
                    target.GetComponent<Attacker>().check_for_healers(gameObject);
                }
                GetComponent<LineRenderer>().SetPositions(new Vector3[] { transform.position, target.transform.position });
            }
            else
            {
                goal = base_goal = new(200, 0);
            }
        }
        else if(laser_enabled)
        {
            laser_enabled = false;
            GetComponent<LineRenderer>().enabled = false;
        }
    }
}

public class Boss : BehaviorScript
{
    protected bool rotate = true;
    protected bool moove = true;
    protected float speed_multiplier = 1;
    protected Transform HealthBar;
    public Transform goalElement;
    protected Vector2 goal;
    protected float cooldown;
    protected float destroytime = 2;
    protected Transform rotation_goal;
    protected uint bossLevel;
    protected AudioSource music;
    LTDescr first_lean;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            onDamage((ushort)collision.gameObject.GetComponent<ProjectileContainer>().Damage, gameObject.layer);
            Object.Destroy(collision.gameObject);
        }
    }

    private void Start()
    {
        reward = gameObject.GetComponent<SetUpScript>().reward;
        health = gameObject.GetComponent<SetUpScript>().health;
        basehealth = health;
        Hit = gameObject.GetComponent<SetUpScript>().HitSound;
        Destroyed = gameObject.GetComponent<SetUpScript>().DestroyedSound;
        MetalSpawnRate = (uint)(gameObject.GetComponent<SetUpScript>().MetalSpawnRate * 10);
        music = GetComponent<BossSetUpScript>().music;

        cooldown = gameObject.GetComponent<BossSetUpScript>().cooldown;
        destroytime = gameObject.GetComponent<BossSetUpScript>().DestroyTime;
        goalElement = GameObject.Find("Camera/Canvas/Image/BorderRight").transform;
        goal = new Vector2(-GetComponent<SpriteRenderer>().bounds.size.x, 0);
        bossLevel = GetComponent<BossSetUpScript>().bossLevel;
        HealthBar = SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().UI.transform.Find("BossBar");
        HealthBar.gameObject.SetActive(true);

        HealthBar.Find("HealthText").GetComponent<TMPro.TMP_Text>().text = $"{transform.name} : {health}/{basehealth}";
        HealthBar.Find("HealthBar").localScale = new Vector3(0, 1, 1);
        first_lean = HealthBar.Find("HealthBar").LeanScale(new Vector3((float)health / (float)basehealth, 1, 1), 3);
        StorageManager.Storage.GetComponent<StorageManager>().Boss(true);

        music.Play();
        StartCoroutine(TweenMusic(0, 1, 5));
    }

    IEnumerator TweenMusic(float v1, float v2, float t)
    {
        for(float i = 0; i < t; i+=Time.fixedDeltaTime)
        {
            music.volume = v1 + (v2 - v1) * (i/t);
            yield return new WaitForFixedUpdate();
        }
    }

    public override void onDamage(ushort DamageAmount, LayerMask origin)
    {
        if (health == 0 || !GetComponent<SpriteRenderer>().isVisible) return;
        if (first_lean != null && first_lean.time != first_lean.passed) first_lean.pause();
        first_lean = null;
        health -= DamageAmount > health ? health : DamageAmount;
        if (health == 0)
        {
            if (rotation_goal) rotation_goal = null;
            StopCoroutine(loop);
            if(attack != null)
            {
                StopCoroutine(attack);
            }
            StartCoroutine(DestroyEffect());
            onDestroyed(destroytime + 0.1f);
            HealthBar.Find("HealthText").GetComponent<TMPro.TMP_Text>().text = $"{transform.name} : 0/{basehealth}";
            HealthBar.Find("HealthBar").localScale = new Vector3(0, 1, 1);
        }
        else
        {
            HealthBar.Find("HealthText").GetComponent<TMPro.TMP_Text>().text = $"{transform.name} : {health}/{basehealth}";
            HealthBar.Find("HealthBar").localScale = new Vector3((float)health / (float)basehealth, 1, 1);
        }
    }

    IEnumerable<AudioSource> playing_audios;
    private void Update()
    {
        if (Time.timeScale == 0)
        {
            if (music is not null && music.isPlaying)
            {
                music.Pause();
            }
            if(playing_audios is null)
            {
                playing_audios = GetComponentsInChildren<AudioSource>().Where(x => x.isPlaying);
                foreach(var audio in playing_audios)
                {
                    audio.Pause();
                }
            }
        }
        else
        {
            if(music is not null && !music.isPlaying)
            {
                music.Play();
            }
            if(playing_audios is not null)
            {
                foreach(var v in playing_audios)
                {
                    v.Play();
                }
                playing_audios = null;
            }
        }
    }

    private void FixedUpdate()
    {
        if (rotation_goal != null)
        {
            //if (transform.rotation != Quaternion.Euler(0, 0, Quaternion.LookRotation((Vector2)rotation_goal.position - (Vector2)transform.position).eulerAngles.x - 180)) transform.rotation = Quaternion.Euler(0, 0, Quaternion.LookRotation((Vector2)rotation_goal.position - (Vector2)transform.position).eulerAngles.x - 180);
            Vector2 direction = ((Vector2)rotation_goal.position - (Vector2)transform.position).normalized;
            if (transform.rotation != Quaternion.Euler(Vector3.forward * Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)) transform.rotation = Quaternion.Euler(Vector3.forward * Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        }
        else if(rotate && transform.rotation != Quaternion.Euler(0, 0, 180))
        {
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }

        if (moove && Vector2.Distance((Vector2)goalElement.position + goal, transform.position) > 1 && health > 0)
        {
            if (GetComponent<Rigidbody2D>().velocity != ((((Vector2)goalElement.position + goal) - (Vector2)transform.position).normalized * SpaceShipPlayer.Speed) * speed_multiplier)
            {
                GetComponent<Rigidbody2D>().velocity = (((Vector2)goalElement.position + goal) - (Vector2)transform.position).normalized * SpaceShipPlayer.Speed * speed_multiplier;
            }
        }
        else if(GetComponent<Rigidbody2D>().velocity != Vector2.zero)
        {
            
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }
        else if (health > 0 && loop == null)
        {
            loop = StartCoroutine(PlayerLoop());
        }
    }

    protected virtual IEnumerator DestroyEffect()
    {
        DatasScript.save.boss_defeated[GameManager.worldindex - 1] = true;
        var parameter = transform.Find("Explosion").GetComponent<ParticleSystem>().main;
        parameter.startSpeed = 10;
        StartCoroutine(TweenMusic(1, 0, destroytime));
        yield return new WaitForSeconds(destroytime);
        parameter.startSpeed = 100;
        StorageManager.Storage.GetComponent<StorageManager>().Boss(false);
        SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().AddRandomPowerUp();
        NotificationScript.AddNotification("BossDefeated", 6);
        Planet.default_influence += 1 + (int)bossLevel;
        SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().AddHealth(SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().maxhealth / 4);
        music.Stop();
        HealthBar.gameObject.SetActive(false);
    }

    Coroutine loop;
    protected Coroutine attack;
    protected bool isCoroutineRunning;
    protected virtual void Attack() { }

    IEnumerator PlayerLoop()
    {
        while (health > 0)
        {
            yield return new WaitForSeconds(cooldown);
            Attack();
            yield return new WaitUntil(() => !isCoroutineRunning);
        }
    }
}

public class FirstTraveler : Boss
{
    bool hasRegen = false;
    //basically use most offensive weapons of the player
    protected override void Attack()
    {
        if(health < basehealth/2)
        {
            if(!hasRegen)
            {
                hasRegen = true;
                SpawnEnemies();
                attack = StartCoroutine(Health());
            }
            else
            {
                hasRegen = false;
                attack = StartCoroutine(Bombs());
            }
        }
        else 
        {
            hasRegen = false;
            attack = StartCoroutine(Laser());
        }
    }
    GameObject BaseLaser;
    GameObject BaseMissile;

    IEnumerator Laser()
    {
        isCoroutineRunning = true;
        if (BaseLaser == null) BaseLaser = Resources.Load<GameObject>("Laser");
        Vector2 memory_goal = goal;
        int ydecalage = 7;

        for(int i = 0; i < (10 + bossLevel * 15); i++)
        {
            goal = new Vector2(goal.x, SpaceShipPlayer.plr.transform.position.y);
            yield return new WaitForSeconds(bossLevel > 1 ? 0.1f : 0.2f);
            transform.Find("FireSound").gameObject.GetComponent<AudioSource>().Play();
            GameObject Laser = Object.Instantiate(BaseLaser, transform.position + transform.right * 10 - transform.up * ydecalage, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z));
            Laser.transform.SetParent(StorageManager.Storage.transform);
            ydecalage = -ydecalage;
            Laser.GetComponent<Rigidbody2D>().velocity = transform.right * (SpaceShipPlayer.Speed * 3);
            Laser.AddComponent<ProjectileContainer>();
            Laser.GetComponent<ProjectileContainer>().Damage = 10;
            Laser.tag = "Projectile";
            Laser.layer = LayerMask.NameToLayer("EnemyObject");
            Object.Destroy(Laser, 5);
        }

        goal = memory_goal;
        isCoroutineRunning = false;
    }

    void SpawnEnemies()
    {
        for(int i = 0; i < (bossLevel > 2 ? 5 : 4); i++)
        {
            GameObject portal = StorageManager.Storage.GetComponent<StorageManager>().create_portal(transform.position - Quaternion.Euler(0, 0, Random.Range(0, 180)) * transform.up * 50, false);
            portal.GetComponent<Portal>().excluded_enemies = new int[] { 1, 2, 3 };
            StartCoroutine(portal.GetComponent<Portal>().Action());
        }
    }

    IEnumerator Health()
    {
        isCoroutineRunning = true;
        int currenthealth = health;
        for(int i=0; i < 10 && currenthealth == health; i++)
        {
            health += 20;
            currenthealth = health;
            HealthBar.Find("HealthText").GetComponent<TMPro.TMP_Text>().text = $"{transform.name} : {health}/{basehealth}";
            HealthBar.Find("HealthBar").localScale = new Vector3((float)health / (float)basehealth, 1, 1);
            yield return new WaitForSeconds(1.5f);
        }
        isCoroutineRunning = false;
    }

    IEnumerator Bombs()
    {
        isCoroutineRunning = true;
        if (BaseMissile == null) BaseMissile = Resources.Load<GameObject>("Missile");
        rotation_goal = SpaceShipPlayer.plr.transform;
        Vector2 currentgoal = goal;
        for(int i = 0; i < 10; i++)
        {
            List<int> ypossibilities = Enumerable.Range(-9, 9).Select(w => w * 10).Where(x => (x >= SpaceShipPlayer.plr.transform.position.y + 20 || x <= SpaceShipPlayer.plr.transform.position.y - 20) &&
            Physics2D.Raycast(transform.position + transform.right * (GetComponent<Collider2D>().bounds.size.x + BaseMissile.GetComponent<Collider2D>().bounds.size.y), SpaceShipPlayer.plr.transform.position - (transform.position + new Vector3(0, - transform.position.y + x)), Vector2.Distance(SpaceShipPlayer.plr.transform.position, transform.position + new Vector3(0, -transform.position.y + x)), LayerMask.GetMask("Enemy")).collider == null).ToList();

            if (Mathf.Abs(transform.position.y - SpaceShipPlayer.plr.transform.position.y) < 20 || Physics2D.Raycast(transform.position + transform.right * (GetComponent<Collider2D>().bounds.size.x + BaseMissile.GetComponent<Collider2D>().bounds.size.y), SpaceShipPlayer.plr.transform.position - transform.position, Vector2.Distance(SpaceShipPlayer.plr.transform.position, transform.position), LayerMask.GetMask("Enemy")).collider != null)
            {
                if(ypossibilities.Any())
                {
                    goal = new Vector2(goal.x, ypossibilities[Random.Range(0, ypossibilities.Count)]);
                }
            }
            yield return new WaitForSeconds(1.5f);
            GameObject Missile = Object.Instantiate(BaseMissile, transform.position + transform.right * (GetComponent<Collider2D>().bounds.size.x + BaseMissile.GetComponent<Collider2D>().bounds.size.y), Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90));
            Missile.transform.SetParent(StorageManager.Storage.transform);
            Missile.GetComponent<Rigidbody2D>().velocity = transform.right * (SpaceShipPlayer.Speed * 2.5f);
            Missile.GetComponent<SetUpScript>().Init();
            Missile.GetComponent<Bomb>().max_damage += bossLevel * 5;
            Missile.GetComponent<Bomb>().target = SpaceShipPlayer.plr;
            Missile.GetComponent<Bomb>().current_pos = SpaceShipPlayer.plr.transform.position;
        }
        goal = currentgoal;
        rotation_goal = null;
        isCoroutineRunning = false;
    }
}


public class StarKiller : Boss
{
    ushort rotation_rate = 10;
    ushort Damage = 5;
    bool isLaserEnabled;
    List<GameObject> touched_entities;
    string Latest_Action = "";
    List<GameObject> enemies_sent;
    float ShieldUsed = -120;
    bool shieldEnabled;

    GameObject BaseMissile;
    /*List<AudioSource> playing_audios = new();

    private void LateUpdate()
    {
        if(Time.timeScale == 0)
        {
            if(!playing_audios.Any())
            {
                playing_audios = GetComponentsInChildren<AudioSource>().Where(x => x.isPlaying).ToList();
                foreach(var v in playing_audios)
                {
                    v.Pause();
                }
            }
        }
        else if(playing_audios.Any())
        {
            foreach (var v in playing_audios)
            {
                v.Play();
            }
            playing_audios = new();
        }
    }*/

    public override void onDamage(ushort DamageAmount, LayerMask origin)
    {
        if (shieldEnabled) return;
        base.onDamage(DamageAmount, origin);
    }

    IEnumerator LaserCollision()
    {
        isCoroutineRunning = true;
        while (isLaserEnabled)
        {
            touched_entities = touched_entities.Where(x => x != null).ToList();

            foreach (GameObject touched in touched_entities)
            {
                if (touched.GetComponent<SpaceShipPlayer>())
                {
                    touched.GetComponent<SpaceShipPlayer>().Damage(Damage);
                }
                else if (touched.GetComponent<BehaviorScript>())
                {
                    touched.GetComponent<BehaviorScript>().onDamage(Damage, gameObject.layer);
                }
            }
            yield return new WaitForSeconds(0.2f);
        }

        isCoroutineRunning = false;
    }

    protected override void Attack()
    {
        if((float)health/(float)basehealth <= 0.5f && Time.timeSinceLevelLoad - ShieldUsed > 120 && Latest_Action != "Shield" && !shieldEnabled)
        {
            Latest_Action = "Shield";
            attack = StartCoroutine(Shield());
        }
        else if(Vector2.Distance(SpaceShipPlayer.plr.transform.position, transform.position) < 75)
        {
            if(Latest_Action != "Laser_follower" && Mathf.Abs(Quaternion.LookRotation(((Vector2)SpaceShipPlayer.plr.transform.position - (Vector2)transform.position).normalized).eulerAngles.x) <= 840)
            {
                Latest_Action = "Laser_follower";
                attack = StartCoroutine(Laser_follower());
            }
            else
            {
                Latest_Action = "Bombs";
                attack = StartCoroutine(Bombs());
            }
        }
        else if (Mathf.Abs(Quaternion.LookRotation(((Vector2)SpaceShipPlayer.plr.transform.position - (Vector2)transform.position).normalized).eulerAngles.x) <= 80 && Latest_Action != "Laser_follower")
        {
            Latest_Action = "Laser_follower";
            attack = StartCoroutine(Laser_follower());
        }
        else
        {
            if(Latest_Action != "LaserMap")
            {
                Latest_Action = "LaserMap";
                attack = StartCoroutine(LaserMap());
            }
            else
            {
                Latest_Action = "Bombs";
                attack = StartCoroutine(Bombs());
            }
        }
    }

    IEnumerator LaserMap()
    {
        isCoroutineRunning = true;

        Vector2 base_goal = goal;
        speed_multiplier = 1.5f;
        transform.Find("Beam").GetComponents<AudioSource>()[0].Play();
        int direction = SpaceShipPlayer.plr.transform.position.y > transform.position.y ? 90 : -90;
        yield return new WaitForSeconds(1);
        transform.Find("Beam").GetComponents<AudioSource>()[0].Stop();
        goal = new Vector2(goal.x, direction);
        yield return StartCoroutine(Laser(2));
        goal = new Vector2(goal.x, -direction * 0.8f);
        yield return StartCoroutine(Laser(3.5f));
        goal = base_goal;
        yield return new WaitForSeconds(1);
        speed_multiplier = 1;

        isCoroutineRunning = false;
    }

    IEnumerator Laser_follower()
    {
        isCoroutineRunning = true;

        transform.Find("Beam").GetComponents<AudioSource>()[0].Play();
        yield return new WaitForSeconds(1);

        transform.Find("Beam").GetComponents<AudioSource>()[0].Stop();
        yield return StartCoroutine(Laser(5, true));

        isCoroutineRunning = false;
    }

    IEnumerator Shield()
    {
        enemies_sent = new();
        ShieldUsed = Time.timeSinceLevelLoad;
        StorageManager.Storage.GetComponent<StorageManager>().signal += Add_enemy_list;
        for(int i = 0; i < 4; i++)
        {
            StorageManager.Storage.GetComponent<StorageManager>().create_portal(transform.position - Quaternion.Euler(0, 0, Random.Range(0, 180)) * transform.up * 50, 1.5f);
        }

        shieldEnabled = true;
        transform.Find("Shield").gameObject.SetActive(true);

        yield return new WaitForSeconds(2);
        StorageManager.Storage.GetComponent<StorageManager>().signal -= Add_enemy_list;

        StartCoroutine(DisableShield());
    }

    IEnumerator Bombs()
    {
        isCoroutineRunning = true;

        if (BaseMissile == null) BaseMissile = Resources.Load<GameObject>("Missile");
        rotation_goal = SpaceShipPlayer.plr.transform;
        yield return new WaitForSeconds(0.5f);
        int bomb_number = Random.Range(3, 6);
        for(int i = 0; i < bomb_number; i++)
        {
            GameObject Missile = Object.Instantiate(BaseMissile, transform.position + transform.right * (GetComponent<Collider2D>().bounds.size.x), Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90));
            Missile.transform.SetParent(StorageManager.Storage.transform);
            Missile.GetComponent<Rigidbody2D>().velocity = transform.right * (SpaceShipPlayer.Speed * 2.5f);
            Missile.GetComponent<SetUpScript>().Init();
            Missile.GetComponent<Bomb>().max_damage += bossLevel * 10;
            Missile.GetComponent<Bomb>().target = SpaceShipPlayer.plr;
            Missile.GetComponent<Bomb>().current_pos = SpaceShipPlayer.plr.transform.position;
            yield return new WaitForSeconds(1);
        }
        rotation_goal = null;
        isCoroutineRunning = false;
    }

    IEnumerator DisableShield()
    {
        while (enemies_sent.Any(x => x != null))
        {
            yield return new WaitForFixedUpdate();
        }
        enemies_sent = new();
        shieldEnabled = false;
        transform.Find("Shield").gameObject.SetActive(false);
    }

    void Add_enemy_list(GameObject enemy)
    {
        enemies_sent.Add(enemy);
    }

    public override void onAttackCollide(GameObject obj, Collider2D collision, Collision_Type c_type)
    {
        Debug.Log(collision.gameObject.name);
        if(c_type == Collision_Type.Trigger && isLaserEnabled && obj != null)
        {
            if(touched_entities.Any(x => x == collision.gameObject))
            {
                touched_entities.Remove(collision.gameObject);
            }
            else if ((collision.GetComponent<BehaviorScript>() != null && collision.gameObject != gameObject) || collision.gameObject == SpaceShipPlayer.plr)
            {
                touched_entities.Add(collision.gameObject);
            }
            else if(collision.GetComponent<ProjectileContainer>() != null)
            {
                Object.Destroy(collision.gameObject);
            }
        }
        else if(c_type == Collision_Type.Collision)
        {
            if(collision.GetComponent<Bomb>())
            {
                onDamage((ushort)(collision.GetComponent<Bomb>().max_damage * 2), collision.gameObject.layer);
                Object.Destroy(collision.gameObject);
            }
            else if(collision.GetComponent<ProjectileContainer>())
            {
                onDamage((ushort)(collision.GetComponent<ProjectileContainer>().Damage * 2), collision.gameObject.layer);
                Object.Destroy(collision.gameObject);
            }
        }
    }

    IEnumerator Laser(float time = 1, bool follow_player = false)
    {
        Transform beam = transform.Find("Beam");
        if(beam.GetComponent<Collision>() == null)
        {
            beam.gameObject.AddComponent<Collision>();
            beam.gameObject.GetComponent<Collision>().init(onAttackCollide, SignalType.All);
        }
        if(isLaserEnabled)
        {
            yield return new WaitForSeconds(time);
            beam.GetComponent<LineRenderer>().enabled = false;
            beam.GetComponent<BoxCollider2D>().enabled = true;
            beam.GetComponent<PolygonCollider2D>().enabled = false;
            isLaserEnabled = false;
        }
        else
        {
            isLaserEnabled = true;
            touched_entities = new();
            beam.GetComponent<BoxCollider2D>().enabled = false;
            beam.GetComponent<PolygonCollider2D>().enabled = true;
            StartCoroutine(LaserCollision());
            StartCoroutine(Laser(time));
            beam.localPosition = new Vector3(-0.7f, 0, 0);
            beam.GetComponent<LineRenderer>().enabled = true;
            Vector3 decalage = -transform.up * beam.GetComponent<LineRenderer>().startWidth / transform.localScale.x;

            beam.GetComponents<AudioSource>()[1].Play();
            beam.GetComponent<PolygonCollider2D>().points = new Vector2[] { decalage, -decalage, -decalage - transform.right * 100, decalage - transform.right * 100 };
            beam.GetComponent<LineRenderer>().SetPositions(new Vector3[] { beam.GetComponent<BoxCollider2D>().offset, beam.GetComponent<BoxCollider2D>().offset - (Vector2)transform.right * 100 });
            beam.rotation = transform.rotation;

            Debug.Log((float)rotation_rate * Time.fixedDeltaTime);
            if (follow_player) rotate = false;
            while (isLaserEnabled)
            {
                if (follow_player)
                {
                    float z_axis = Quaternion.LookRotation((Vector2)(SpaceShipPlayer.plr.transform.position - transform.position)).eulerAngles.x - transform.rotation.eulerAngles.z + 180;
                    z_axis += Mathf.Abs(z_axis - transform.rotation.z) > 180 ? z_axis - transform.rotation.z > 0 ? -360 : 360 : 0;
                    transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + Mathf.Clamp(z_axis, -(float)rotation_rate * Time.fixedDeltaTime, (float)rotation_rate * Time.fixedDeltaTime));
                }
                
                yield return new WaitForFixedUpdate();
            }
            beam.GetComponents<AudioSource>()[1].Stop();
            rotate = true;
        }
    }
}

public class GalaxyAmiral : Boss
{
    bool transformed;
    bool transforming;

    string Latest_Action = "";
    GameObject BaseMissile;
    GameObject BaseGlitch;
    GameObject BaseAttack;
    GameObject BaseLaser;

    LTDescr position_tween;

    private void Awake()
    {
        StartCoroutine(Begin());
    }

    IEnumerator Begin()
    {
        yield return new WaitForFixedUpdate();
        transform.Find("Hitbox").AddComponent<Collision>().init(onAttackCollide);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(front_attacking)
        {
            if(collision.gameObject == SpaceShipPlayer.plr)
            {
                SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().Damage(50);
                front_attacking = false;
            }
        }
        
    }

    public override void onDamage(ushort DamageAmount, LayerMask origin)
    {
        if (transforming) return;

        if(DamageAmount >= health && !transformed)
        {
            transforming = true;
            if(attack is not null)
            {
                StopCoroutine(attack);
            }
            attack = StartCoroutine(Transformation());
        }
        else
        {
            base.onDamage(DamageAmount, origin);
        }
    }

    IEnumerator Transformation()
    {
        isCoroutineRunning = true;

        rotation_goal = null;
        GetComponent<LineRenderer>().enabled = false;
        transform.Find("Teleportation").GetComponent<AudioSource>().Stop();
        transform.Find("Teleportation").GetComponent<Light2D>().enabled = false;
        speed_multiplier = 1;

        moove = false;
        HealthBar.Find("HealthText").GetComponent<TMPro.TMP_Text>().text = $"{transform.name} : 0/{basehealth}";
        HealthBar.Find("HealthBar").localScale = new Vector3(0 / (float)basehealth, 1, 1);
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        var old_audio = music;
        old_audio.Stop();
        music = null;
        transform.Find("Transformation").GetComponent<AudioSource>().Play();
        transform.Find("Transformation").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(5);
        goal = new Vector2(-GetComponent<SpriteRenderer>().bounds.size.x, 0);
        GetComponent<SpriteRenderer>().material = Resources.Load<Material>("GlitchMaterial");
        yield return new WaitForSeconds(4.25f);
        music = old_audio.transform.parent.Find(old_audio.gameObject.name + "_2").GetComponent<AudioSource>();
        music.Play();
        Object.Destroy(transform.Find("Transformation").gameObject);
        health = basehealth;
        transform.name = "Corrupted " + transform.name;
        HealthBar.Find("HealthText").GetComponent<TMPro.TMP_Text>().text = $"{transform.name} : {health}/{basehealth}";
        HealthBar.Find("HealthBar").localScale = new Vector3((float)health / (float)basehealth, 1, 1);
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
        moove = true;
        transforming = false;
        transformed = true;
        isCoroutineRunning = false;
    }

    protected override void Attack()
    {
        if(!transforming)
        {
            if(Vector2.Distance(transform.position, SpaceShipPlayer.plr.transform.position) < (transformed ? 100 : 75))
            {
                if(Latest_Action != "TakeEnergy")
                {
                    Latest_Action = "TakeEnergy";
                    attack = StartCoroutine(TakeEnergy());
                }
                else
                {
                    Latest_Action = "FrontAttack";
                    attack = StartCoroutine(FrontAttack());
                }
            }
            else if(((transformed && Vector2.Distance(transform.position, SpaceShipPlayer.plr.transform.position) > 500) || Mathf.Abs(Quaternion.LookRotation(((Vector2)SpaceShipPlayer.plr.transform.position - (Vector2)transform.position).normalized).eulerAngles.x) >= 60) && Latest_Action != "BehindAttack")
            {
                Latest_Action = "BehindAttack";
                attack = StartCoroutine(BehindAttack());
            }
            else
            {
                if(Latest_Action != "ProjectionAttack")
                {
                    Latest_Action = "ProjectionAttack";
                    attack = StartCoroutine(ProjectionAttack());
                }
                else
                {
                    Latest_Action = "Bombs";
                    attack = StartCoroutine(Bombs());
                }
            }
        }
    }

    IEnumerator ProjectionAttack()
    {
        isCoroutineRunning = true;

        if (BaseAttack is null)
        {
            BaseAttack = Resources.Load<GameObject>("LargeAttack");
            BaseGlitch = Resources.Load<GameObject>("GlitchObject");
        }

        Vector2 memory_goal = goal;
        for(int i = 0; i < 6; i++)
        {
            goal = new(goal.x, SpaceShipPlayer.plr.transform.position.y);

            GameObject clone = Object.Instantiate(transformed ? BaseGlitch : BaseAttack, transform.position + transform.right * (GetComponent<SpriteRenderer>().bounds.size.x/2 + 1), transform.rotation);
            clone.transform.SetParent(StorageManager.Storage.transform);
            clone.GetComponent<Rigidbody2D>().velocity = transform.right * (SpaceShipPlayer.Speed * (transformed ? 3 : 6));
            clone.AddComponent<Collision>();
            GetComponent<AudioSource>().Play();
            
            if(transformed)
            {
                clone.GetComponent<Rigidbody2D>().angularVelocity = 45;
                clone.AddComponent<GlitchScript>().react_mooving = true;
                clone.GetComponent<Collision>().init(onAttackCollide, SignalType.All);
            }
            else
            {
                clone.transform.LeanScale(new Vector3(7.5f, 7.5f, 1), 0.2f);
                clone.layer = LayerMask.NameToLayer("EnemyObject");
                clone.GetComponent<Collision>().init(onAttackCollide, SignalType.Beginning);
            }

            yield return new WaitForSeconds(1.5f);
        }

        goal = memory_goal;
        isCoroutineRunning = false;
    }

    bool front_attacking;
    IEnumerator FrontAttack()
    {
        isCoroutineRunning = true;

        rotation_goal = SpaceShipPlayer.plr.transform;
        Vector2 basepos = transform.position;
        Vector2 direction = (Vector2)(rotation_goal.position - transform.position).normalized;
        goal -= direction * 20;
        yield return new WaitForSeconds(0.5f);
        rotate = false;
        rotation_goal = null;
        GetComponent<LineRenderer>().enabled = true;
        GetComponent<LineRenderer>().SetPositions(new Vector3[] { transform.position, transform.position + transform.right * 300 });
        yield return new WaitForSeconds(1);
        front_attacking = true;
        GetComponent<LineRenderer>().enabled = false;
        position_tween = transform.LeanMove(transform.position + transform.right * 300, 0.75f);
        yield return new WaitForSeconds(0.75f);
        position_tween = null;
        front_attacking = false;
        moove = false;
        transform.LeanMove(basepos, 0.75f);
        yield return new WaitForSeconds(0.75f);
        rotate = true;
        moove = true;
        speed_multiplier = 1;
        isCoroutineRunning = false;
    }

    IEnumerator TakeEnergy()
    {
        isCoroutineRunning = true;

        GetComponent<LineRenderer>().enabled = true;
        Coroutine Damage = StartCoroutine(EnergyDamage());
        for(float i = 0; i < 5; i += Time.fixedDeltaTime)
        {
            if (Vector2.Distance(transform.position, SpaceShipPlayer.plr.transform.position) >= (transformed ? 150 : 100)) break;
            GetComponent<LineRenderer>().SetPositions(new Vector3[] { transform.position, SpaceShipPlayer.plr.transform.position });
            yield return new WaitForFixedUpdate();
        }
        StopCoroutine(Damage);
        GetComponent<LineRenderer>().enabled = false;

        isCoroutineRunning = false;
    }
    IEnumerator EnergyDamage()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.2f);
            SpaceShipPlayer.plr.GetComponent<SpaceShipPlayer>().Damage(5);
            health += 10;
            HealthBar.Find("HealthText").GetComponent<TMPro.TMP_Text>().text = $"{transform.name} : {health}/{basehealth}";
            HealthBar.Find("HealthBar").localScale = new Vector3((float)health / (float)basehealth, 1, 1);
        }
    }

    IEnumerator BehindAttack()
    {
        isCoroutineRunning = true;

        if (BaseLaser is null) BaseLaser = Resources.Load<GameObject>("MiniLaser");

        transform.Find("Teleportation").GetComponent<Light2D>().enabled = true;
        yield return new WaitForSeconds(0.75f);
        Vector3 first_position = transform.position;
        moove = false;
        transform.Find("Teleportation").GetComponents<AudioSource>()[0].Play();
        transform.position = SpaceShipPlayer.plr.transform.position - new Vector3(GetComponent<SpriteRenderer>().bounds.size.x * 1.5f, 0);
        yield return new WaitForSeconds(0.5f);
        transform.Find("Teleportation").GetComponents<AudioSource>()[0].Stop();
        transform.Find("Teleportation").GetComponent<Light2D>().enabled = false;
        rotation_goal = SpaceShipPlayer.plr.transform;

        int decalage = 10;
        for(int i=0; i < 30; i++)
        {
            GameObject clone = Object.Instantiate(BaseLaser, transform.position + transform.right * GetComponent<SpriteRenderer>().bounds.size.x/2 - transform.up * decalage, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90));
            clone.transform.SetParent(StorageManager.Storage.transform);
            clone.GetComponent<Rigidbody2D>().velocity = transform.right * SpaceShipPlayer.Speed * 4;
            clone.AddComponent<ProjectileContainer>();
            clone.GetComponent<ProjectileContainer>().Damage = 6;
            clone.tag = "Projectile";
            clone.layer = LayerMask.NameToLayer("EnemyObject");
            transform.Find("Teleportation").GetComponents<AudioSource>()[1].Play();
            decalage = -decalage;
            Object.Destroy(clone, 5);
            yield return new WaitForSeconds(0.1f);
        }
        rotation_goal = null;
        moove = true;
        transform.position = first_position;
        isCoroutineRunning = false;
    }

    IEnumerator Bombs()
    {
        isCoroutineRunning = true;

        if (BaseMissile is null) BaseMissile = Resources.Load<GameObject>("Missile");

        rotation_goal = SpaceShipPlayer.plr.transform;
        Vector3 first_pos = goal;
        yield return new WaitForSeconds(0.5f);
        int bomb_number = Random.Range(3, 6);
        for (int i = 0; i < bomb_number; i++)
        {
            GameObject Missile = Object.Instantiate(BaseMissile, transform.position + transform.right * (GetComponent<Collider2D>().bounds.size.x/2 + BaseMissile.GetComponent<Collider2D>().bounds.size.y + 1), Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90));
            Missile.transform.SetParent(StorageManager.Storage.transform);
            Missile.GetComponent<Rigidbody2D>().velocity = transform.right * (SpaceShipPlayer.Speed * 2.5f);
            Missile.GetComponent<SetUpScript>().Init();
            Missile.GetComponent<Bomb>().max_damage += bossLevel * 15;
            Missile.GetComponent<Bomb>().target = SpaceShipPlayer.plr;
            Missile.GetComponent<Bomb>().current_pos = SpaceShipPlayer.plr.transform.position;

            if(transformed)
            {
                Missile.GetComponent<SpriteRenderer>().material = GlitchScript.GlitchMaterial != null ? GlitchScript.GlitchMaterial : Resources.Load<Material>("GlitchMaterial");
                Missile.AddComponent<GlitchScript>().react_destroying = true;
            }

            List<int> ypossibilities = Enumerable.Range(-9, 9).Select(w => w * 10).Where(x => (x >= SpaceShipPlayer.plr.transform.position.y + 20 || x <= SpaceShipPlayer.plr.transform.position.y - 20) &&
            Physics2D.Raycast(transform.position + transform.right * (GetComponent<Collider2D>().bounds.size.x + BaseMissile.GetComponent<Collider2D>().bounds.size.y), SpaceShipPlayer.plr.transform.position - (transform.position + new Vector3(0, -transform.position.y + x)), Vector2.Distance(SpaceShipPlayer.plr.transform.position, transform.position + new Vector3(0, -transform.position.y + x)), LayerMask.GetMask("Enemy")).collider == null).ToList();

            if (Mathf.Abs(transform.position.y - SpaceShipPlayer.plr.transform.position.y) < 20 || Physics2D.Raycast(transform.position + transform.right * (GetComponent<Collider2D>().bounds.size.x + BaseMissile.GetComponent<Collider2D>().bounds.size.y), SpaceShipPlayer.plr.transform.position - transform.position, Vector2.Distance(SpaceShipPlayer.plr.transform.position, transform.position), LayerMask.GetMask("Enemy")).collider != null)
            {
                if (ypossibilities.Any())
                {
                    goal = new Vector2(goal.x, ypossibilities[Random.Range(0, ypossibilities.Count)]);
                }
            }
            yield return new WaitForSeconds(0.75f);
        }
        rotation_goal = null;
        goal = first_pos;
        isCoroutineRunning = false;
    }

    public override void onAttackCollide(GameObject obj, Collider2D collision, Collision_Type c_type)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            collision.gameObject.GetComponent<Collision>().Damage(transformed ? 10u : 25u);
            Object.Destroy(obj);
            return;
        }

        if(obj.transform.parent != transform)
        {

            if (collision.gameObject.GetComponent<SpaceShipPlayer>() != null)
            {
                collision.gameObject.GetComponent<SpaceShipPlayer>().Damage(25);
            }
            else if (collision.gameObject.GetComponent<BehaviorScript>() != null)
            {
                collision.gameObject.GetComponent<BehaviorScript>().onDamage(25, gameObject.layer);
            }
        }
        else
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("PlayerBorder") && position_tween != null)
            {
                position_tween.pause();
                Debug.Log("tween paused");
            }
        }
    }
}