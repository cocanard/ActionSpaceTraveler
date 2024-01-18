using UnityEngine;
using System.Collections;

public class PlanetDestroyer : BehaviorScript
{
    public bool Activated;
    bool initialized;
    Transform Target;
    // Use this for initialization

    public override void onDamage(ushort DamageAmount, LayerMask origin)
    {
        ushort real_health = DamageAmount;
        if (origin == LayerMask.NameToLayer("Planet")) real_health = (ushort)_health;
        else if(real_health >= _health && (LayerMask.LayerToName(origin).Contains("Player")))
        {
            Target.GetComponent<Planet>().PlayerKilled();
        }
        base.onDamage(DamageAmount, origin);
    }

    // Update is called once per frame
    void Update()
    {
        if(Activated)
        {
            if(!initialized)
            {
                initialized = true;
                Target = transform.parent.parent;
                transform.parent.LeanRotateZ(1440, 30);
                StartCoroutine(DealDamage());
            }
            else
            {
                if(Target is not null)
                {

                }
                else
                {

                }
            }
        }
    }

    IEnumerator DealDamage()
    {
        while(_health > 0)
        {
            Target.GetComponent<BehaviorScript>().onDamage(1, gameObject.layer);
            yield return new WaitForSeconds(0.75f);
        }
    }
}

