using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Vector3 decalage;
    public Transform Camera;
    public int Speed;
    private bool isJumping = false;
    Rigidbody2D rb2D;
    // Start is called before the first frame update
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        Camera.position = transform.position + decalage;
    }

    // Update is called once per frame
    void Update()
    {
        if((Input.GetButtonDown("Jump") || Input.GetMouseButtonDown(0)) && !isJumping)
        {
            Jump(10);
        }
        transform.position = transform.position + Vector3.right * Time.deltaTime * Speed;
        Camera.position = transform.position + decalage;
    }

    void Jump(int jumpForce)
    {
        isJumping = true;
        rb2D.velocity += Vector2.up * jumpForce;
    }

    private void OnCollisionEnter2D(Collision2D collided)
    {
        if(collided.gameObject.CompareTag("Death"))
        {
            Destroy(gameObject);
        }
        else if(collided.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
        }
    }
}
