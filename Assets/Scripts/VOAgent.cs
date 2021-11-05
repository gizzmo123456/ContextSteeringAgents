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
 [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class VOAgent : MonoBehaviour
{

    private Rigidbody2D rb;

    private Vector3 Forwards => transform.up;

    // Agent
    [Header("Agent Config")]
    [SerializeField]
    private float agentRadius = 0.5f;
    [SerializeField]
    private float agentMoveSpeed = 5f; // units per second.

    // Avoid
    [Header("Avoid Config")]
    [SerializeField]
    private float avoidRadius = 2f;

    [SerializeField]
    private int maxDetectAgents = 1;
    private RaycastHit2D[] detectedAgents;

    private Vector2 currentVelocity = Vector2.zero;

    public Transform targetPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<CircleCollider2D>().radius = agentRadius;

        transform.localScale = new Vector3(agentRadius*2f, agentRadius*2f, 1f);

        detectedAgents = new RaycastHit2D[ maxDetectAgents ];

    }

    public void SetStartVelocity(Vector2 startVelocity)
    {
        // Theres no need to set the velocity as it always moves forwards.
        SetRotationFromVelocity(currentVelocity);

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        Vector3 dir = ( targetPosition.position - transform.position ).normalized;

        // move forwards.
        currentVelocity = dir * agentMoveSpeed * Time.deltaTime;
        rb.velocity = currentVelocity;

        SetRotationFromVelocity(currentVelocity);

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="velocity">Velocity relevent to this agent</param>
    /// <returns></returns>
    private void SetRotationFromVelocity( Vector2 velocity )
    {
        // find the vector between us and the target velocity.
        float rotation = Mathf.Atan2(-velocity.x, velocity.y) * Mathf.Rad2Deg;
        transform.eulerAngles = new Vector3(0, 0, rotation);

    }
}
