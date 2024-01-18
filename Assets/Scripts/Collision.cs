using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Collision_Type { Collision, Trigger, All }
public enum SignalType { Beginning, End, All }

public class Collision : MonoBehaviour
{
    SignalType s_type;

    WeaponInfo Player;
    Action<GameObject, Collider2D, Collision_Type> function;

    public void init(Action<GameObject, Collider2D, Collision_Type> f, SignalType signal = SignalType.Beginning)
    {
        function = f;
        s_type = signal;
    }
    public void init(Action<GameObject, Collider2D, Collision_Type> f, WeaponInfo p, SignalType signal = SignalType.Beginning)
    {
        Player = p;
        init(f, signal);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (s_type != SignalType.Beginning && s_type != SignalType.All) return;
        if (function != null)
        {
            function(gameObject, collision, Collision_Type.Trigger);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (s_type != SignalType.Beginning && s_type != SignalType.All) return;
        if (function != null)
        {
            function(gameObject, collision.collider, Collision_Type.Collision);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (s_type != SignalType.End && s_type != SignalType.All) return;
        if (function != null)
        {
            function(gameObject, collision, Collision_Type.Trigger);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (s_type != SignalType.End && s_type != SignalType.All) return;
        if (function != null)
        {
            function(gameObject, collision.collider, Collision_Type.Collision);
        }
    }

    public void Damage(uint damage)
    {
        if(Player is ShieldWeapon)
        {
            ((ShieldWeapon)Player).Damage(damage);
        }
    }
}
