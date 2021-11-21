using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSpawn_FlowFeild : MonoBehaviour
{

    public static AgentSpawn_FlowFeild inst;

    public FlowFeild flowFeild;
    public CSAgent_FlowFeild agent;

    public int agentsToSpwan = 100;
    public float spwanTime = 0.5f;
    private float timeRemaining = 0.5f;

    public float minMoveSpeed = 2;
    public float maxMoveSpeed = 5;

    int spawnedCount = 0;

    [Header( "Raycast Check" )]
    [Tooltip( "Should it check if an object is at the spawn location. Ie an agent." )]
    public bool rayTest = false;
    public LayerMask rayMask;
    public Vector2 rayBoxSize;
    RaycastHit2D[] rayHits = new RaycastHit2D[1];

	private void Awake()
	{
        inst = this;
	}

    void Update()
    {

        timeRemaining -= Time.deltaTime;

        if ( timeRemaining > 0f || agentsToSpwan < 1)
            return;

        timeRemaining = spwanTime;

        CSAgent_FlowFeild agen = Instantiate( agent, Vector3.zero, Quaternion.identity );
        agen.name = agen.name + " - " + ++spawnedCount;
        RespwanAgent( agen );

        agentsToSpwan--;

    }

    public void RespwanAgent( CSAgent_FlowFeild agen )
    {

        Vector3 location = flowFeild.GetRandomCell();

        if ( rayTest )
        {

            // only spwan agent if there is nothing at location.

            int hitCount = int.MaxValue;

            while ( hitCount > 0 )
            {
                hitCount = Physics2D.BoxCastNonAlloc( location, rayBoxSize, 0, Vector2.zero, rayHits, 0, rayMask );

                if ( hitCount > 0 )
                    location = flowFeild.GetRandomCell();
            }

		}

        agen.transform.position = location;
        agen.flowFeild = flowFeild;
        agen.agent_moveSpeed = Random.Range( minMoveSpeed, maxMoveSpeed );

    }

}
