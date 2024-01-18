using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetUpScript : MonoBehaviour
{
    public ushort reward;
    public ushort health;
    [SerializeField] ushort typeid;
    [SerializeField] protected string HitId;
    [SerializeField] protected string DestroyedId;
    public AudioSource HitSound { get; protected set; }
    public AudioSource DestroyedSound { get; protected set; }
    enum EnemyType { BehaviorScript, Laser, Bomber, Bomb, Fighter, Planet, Portal, Caller, Destroyer, Interceptor, Neutralizer, Electrizer, Drone, Astroyer, Healer, BigBomb, PlanetDestroyer };

    [Range(0, 100)] public float MetalSpawnRate;

    public virtual void Init()
    {
        HitSound = HitId != String.Empty ? GameObject.Find("SoundObjects").transform.Find(HitId).GetComponent<AudioSource>() : null;
        DestroyedSound = DestroyedId != String.Empty ? GameObject.Find("SoundObjects").transform.Find(DestroyedId).GetComponent<AudioSource>() : null;
        //0 : normal, 1 : attacker, 2 : bomber, 3 : bomb, 4 : fighter, 5 : planet, 6 : portal, 7 : Caller, 8 : destroyer, 9 : interceptor, 10 : neutralizer,11 : electrizer, 12 : drone, 13 : astroyer, 14 : healer, 15 : BigBomb
        gameObject.AddComponent(Type.GetType(((EnemyType)typeid).ToString()));
    }
}