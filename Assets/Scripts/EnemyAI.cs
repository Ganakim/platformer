using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    Rigidbody2D rb;
    GameObject player;
    // Start is called before the first frame update

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
    }
    void Start()
    {
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update(){
        rb.AddForce(new Vector2(player.transform.position.x - transform.position.x, 0).normalized * 15f);

        rb.velocity = new Vector2(rb.velocity.x * .8f, rb.velocity.y);
    }
}
