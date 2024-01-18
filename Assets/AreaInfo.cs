using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "AreaInfo", menuName = "AreaInfo", order = 2)]
public class AreaInfo : ScriptableObject
{
    [Header("Stars")]
    public GameObject star;
    public bool star_enabled;
    public int star_startingspeed;
    public int star_delay;

    [Header("Obstacles")]
    public GameObject[] obstacles;
    public int obstacles_startingspeed;
    public int[] obstacles_delay;

    [Header("Enemies")]
    public GameObject[] enemy_object;
    public int[] enemy_probabilities;
    public int[] enemy_startingspeed;
    public int enemy_delay;

    [Header("Bosses")]
    public GameObject Boss_Object;
    public int[] base_spawn;
    public int boss_delay;
}

