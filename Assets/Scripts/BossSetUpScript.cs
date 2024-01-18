using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class BossSetUpScript : SetUpScript
{
    public float cooldown;
    [SerializeField] string BossClass;
    public float DestroyTime;
    public uint bossLevel;
    public AudioSource music { get; private set; }
    [SerializeField] string BossMusic;

    public override void Init()
    {
        HitSound = GameObject.Find("SoundObjects").transform.Find(HitId).GetComponent<AudioSource>();
        DestroyedSound = GameObject.Find("SoundObjects").transform.Find(DestroyedId).GetComponent<AudioSource>();
        music = GameObject.Find("SoundObjects").transform.Find(BossMusic).GetComponent<AudioSource>();
        Debug.Log(music);

        gameObject.AddComponent(Type.GetType(BossClass));
    }
}
