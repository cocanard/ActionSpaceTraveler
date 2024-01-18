using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GlitchScript : MonoBehaviour
{
    public static Material GlitchMaterial;
    public static Material ConstantGlitchMaterial;
    static GameObject glitch_part;
    public bool react_destroying;
    Coroutine mooving;
    bool _react_mooving;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ConnectToEvent()
    {
        BehaviorScript.OnGlitchCreated += GlitchCreated;
    }

    public bool react_mooving
    {
        get
        {
            return _react_mooving;
        }
        set
        {
            _react_mooving = value;
            if (value && mooving is null) mooving = StartCoroutine(EmitParticles());
            else if (!value && mooving != null) StopCoroutine(mooving);
        }
    }

    public bool react_awake;

    IEnumerator EmitParticles()
    {
        yield return new WaitForSeconds(10 / Vector2.Distance(transform.position, transform.position + (Vector3)GetComponent<Rigidbody2D>().velocity));
        while (gameObject != null)
        {
            createGlitchPart(transform.position);

            yield return new WaitForSeconds(10 / Vector2.Distance(transform.position, transform.position + (Vector3)GetComponent<Rigidbody2D>().velocity));
        }
    }

    private void Awake()
    {
        if(GlitchMaterial == null)
        {
            GlitchMaterial = Resources.Load<Material>("GlitchMaterial");
            ConstantGlitchMaterial = Resources.Load<Material>("ConstantGlitchMaterial");
            glitch_part = Resources.Load<GameObject>("GlitchObject");
        }
        if(react_awake)
        {
            for(int i = 0; i < 20; i++)
            {
                createGlitchPart(new Vector3(Random.Range(-30, 30), Random.Range(-30, 30)), transform, false);
            }
        }
    }

    private void OnDestroy()
    {
        if(react_destroying)
        {
            for(int i = 0; i < 6; i++)
            {
                createGlitchPart(transform.position + new Vector3(Random.Range(-15, 15), Random.Range(-15, 15)));
            }
        }
    }

    void createGlitchPart(Vector3 position)
    {
        GameObject glitch = Object.Instantiate(glitch_part, position, new());
        glitch.transform.localScale = new Vector3(Random.Range(10, 21), Random.Range(10, 21));
        glitch.transform.SetParent(StorageManager.Storage.transform);
        BehaviorScript.OnGlitchCreated.Invoke(glitch);
        Object.Destroy(glitch, 4);
    }
    void createGlitchPart(Vector3 position, Transform parent, bool destroy)
    {
        GameObject glitch = Object.Instantiate(glitch_part, position, new());
        glitch.transform.localScale = new Vector3(Random.Range(10, 21), Random.Range(10, 21));
        glitch.transform.SetParent(parent, false);
        BehaviorScript.OnGlitchCreated.Invoke(glitch);
        if(destroy)
        {
            Object.Destroy(glitch, 4);
        }
    }

    public static void OnCollision(GameObject obj, Collider2D collision, Collision_Type c_type)
    {
        if (collision.gameObject.GetComponent<SpaceShipPlayer>() != null || collision.gameObject.GetComponent<BehaviorScript>() != null)
        {

            if (touched_objects.ContainsKey(collision.gameObject))
            {
                if (touched_objects[collision.gameObject].Contains(obj))
                {
                    touched_objects[collision.gameObject].Remove(obj);
                }
                else
                {
                    touched_objects[collision.gameObject].Add(obj);
                }
            }
            else StorageManager.Storage.GetComponent<StorageManager>().StartCoroutine(GlitchDamage(collision.gameObject, obj));
        }
    }

    static void GlitchCreated(GameObject glitch)
    {
        glitch.AddComponent<Collision>();
        glitch.GetComponent<Collision>().init(OnCollision, SignalType.All);
    }

    static Dictionary<GameObject, List<GameObject>> touched_objects = new();

    static IEnumerator GlitchDamage(GameObject target, GameObject collision)
    {
        touched_objects.Add(target, new() { collision });
        Material current = target.GetComponent<SpriteRenderer>().material;
        target.GetComponent<SpriteRenderer>().material = GlitchScript.GlitchMaterial;
        int i = 0;
        while (i < 5)
        {
            if (target.GetComponent<SpaceShipPlayer>() != null)
            {
                target.GetComponent<SpaceShipPlayer>().Damage(5);
            }
            else
            {
                target.GetComponent<BehaviorScript>().onDamage(5, LayerMask.NameToLayer("Enemy"));
            }

            yield return new WaitForSeconds(1);
            touched_objects[target] = touched_objects[target].Where(x => x != null).ToList();
            if (!touched_objects[target].Any()) i++;
            else i = 0;
        }
        target.GetComponent<SpriteRenderer>().material = current;
        touched_objects.Remove(target);
    }
}

