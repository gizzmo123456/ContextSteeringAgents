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

        agen.transform.position = flowFeild.GetRandomCell();
        agen.flowFeild = flowFeild;
        agen.agent_moveSpeed = Random.Range( minMoveSpeed, maxMoveSpeed );

    }

}
