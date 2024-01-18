using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemScript : MonoBehaviour
{
    public string v;
    public int value;
    void Start()
    {
        Object.Destroy(gameObject, 10);
        float rnd1 = Random.Range(-1.0f, 1.0f);
        float rnd2 = Random.Range(-1.0f, 1.0f);
        gameObject.GetComponent<Rigidbody2D>().velocity = new Vector3(rnd1 * 20, rnd1 * 20, 0);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject == SpaceShipPlayer.plr)
        {
            collision.gameObject.GetComponent<SpaceShipPlayer>().PickupItem(gameObject);
        }
    }
}
