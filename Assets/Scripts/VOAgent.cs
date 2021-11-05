using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * Note: While this is intended to be a VO Agent, its not a direct version of any known VO algorithms
 * However, this takes ideas from RVO and ORCA (as presented in: GameAIPro3 ch19 RVO and ORCA)
 * 
 * I want to see if the ideas in my head will work, and to help me better understand what is talked about in
 * the book.
 * 
 */
 [RequireComponent(typeof(Rigidbody2D))]
public class VOAgent : MonoBehaviour
{

    private Rigidbody2D rb;

    // Agent
    [Header("Agent Config")]
    [SerializeField]
    private float agentRadius = 0.5f;
    private float agentMoveSpeed = 5f; // units per second.

    // Avoid
    [Header("Avoid Config")]
    [SerializeField]
    private float avoidRadius = 2f;

    [SerializeField]
    private int maxDetectAgents = 1;
    RaycastHit2D[] detectedAgents;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        transform.localScale = new Vector3(agentRadius, agentRadius, 1f);

        detectedAgents = new RaycastHit2D[ maxDetectAgents ];

    }

    // Update is called once per frame
    void Update()
    {

        // move forwards.
        Vector2 moveVelocity = transform.forward * agentMoveSpeed * Time.deltaTime;
        rb.velocity = moveVelocity;

    }
}
