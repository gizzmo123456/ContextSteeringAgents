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
    [SerializeField]
    [Header("Agent Config")]
    private Color agentColour;
    [SerializeField]
    private float agentRadius = 0.5f;
    [SerializeField]
    private float agentMoveSpeed = 5f; // units per second.

    // Avoid
    [Header("Avoid Config")]
    [SerializeField]
    private float detectRadius = 2f;

    [SerializeField]
    private int maxDetectAgents = 1;
    private RaycastHit2D[] detectedAgents;

    private Vector2 currentVelocity = Vector2.zero;

    public Transform targetPosition;

    public Transform TEMPMarker;
    private Transform[] markers;
    public bool DEBUG = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<CircleCollider2D>().radius = agentRadius;
        GetComponent<SpriteRenderer>().color = agentColour;

        transform.localScale = new Vector3(agentRadius*2f, agentRadius*2f, 1f);

        detectedAgents = new RaycastHit2D[ maxDetectAgents+1 ]; // +1 because we will detect ourself :(, i'll find a better solution at some point.

        markers = new Transform[2];
        for (int i = 0; i < markers.Length; i++)
        {
            markers[i] = Instantiate(TEMPMarker); // new GameObject(name+"_marker_" + i).transform;
            markers[i].name = $"{name}-MARKER-{i}";
            markers[i].GetComponent<SpriteRenderer>().color = agentColour;
        }
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

        // Cast a circel cast into world at our current location to find if any NPC are in radius.
        int rayHitCount = Physics2D.CircleCastNonAlloc( transform.position, detectRadius, Vector2.zero, detectedAgents, 0f );

        if ( rayHitCount > 1 )
        {
            // Avoid things.
            foreach (RaycastHit2D rh in detectedAgents)
                if (rh.transform != transform)
                {

                    VOAgent otherAgent = rh.transform.GetComponent<VOAgent>();
                    Vector2 vector = (rh.transform.position - transform.position).normalized;
                    float dot = Vector2.Dot(Forwards, vector);

                    // make sure the agent is infront of us with upto around 90deg
                    if (dot > 0.5f) // this can be higher...
                    {
                        print($"{name}: Attempting to avoid {rh.transform.name} ({dot})");

                        // Get the amount of distance that is required between the two agents
                        float avoidDistance = agentRadius + otherAgent.agentRadius;

                        // Find the shortest Perpendicular angle between us and the other agent, along the vector
                        Vector3 perpendicularVector = Vector2.Perpendicular( vector ) * avoidDistance;

                        Vector2 target_0 = otherAgent.transform.position + perpendicularVector;// Left along the agents forwards (i think)
                        Vector2 target_1 = otherAgent.transform.position - perpendicularVector;// Right along the agents forwards

                        markers[0].position = target_0;
                        markers[1].position = target_1;

                        float angle_0 = GetAngleFormWorldPosition( target_0 );
                        float angle_1 = GetAngleFormWorldPosition( target_1 );

                        print($"a_0: {angle_0}, a_1: {angle_1} ");
                        //float angle_0 = GetAngleForReleventPosition()

                    }
                    else
                    {
                        print($"{name}: No need to avoid {rh.transform.name} ({dot})");
                    }
                }
		}


        // move forwards.
        currentVelocity = Forwards * agentMoveSpeed * Time.deltaTime;
        // rb.velocity = currentVelocity;

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


    private float GetAngleFormWorldPosition(Vector3 worldPosition)
    {
        return GetAngleFormReleventPosition( transform.position - worldPosition );
    }

    private float GetAngleFormReleventPosition( Vector2 releventPosition )
    {
        return Mathf.Atan2(-releventPosition.x, releventPosition.y) * Mathf.Rad2Deg;
    }
}
