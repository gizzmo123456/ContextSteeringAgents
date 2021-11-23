using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSpawn : MonoBehaviour
{

    private static AgentSpawn inst;

    [SerializeField]
    private CSAgent agentPrefab;
    [SerializeField] float time;
    private float remainingTime;

    [SerializeField] Transform[] targets;

    public Color col;

    int count = 0;
    public int maxSpawn = 250;
	private void Start()
	{
        remainingTime = time;
        inst = this;
	}

	// Update is called once per frame
	void Update()
    {

        if ( !SceneManagement.started )
            return;

        remainingTime -= Time.deltaTime;

        if ( remainingTime > 0f || count > maxSpawn)
            return;

        CSAgent agent = Instantiate( agentPrefab, transform.position, Quaternion.identity);
        agent.target = targets[ Random.Range(0, targets.Length) ];
        //agent.agent_colour = col; 
        //agent.GetComponent<SpriteRenderer>().color = col;
        agent.name = $"[{name}] - agent ({++count})";
        
        remainingTime = time;


    }

    public static Transform GetTraget()
    {
        return inst.targets[Random.Range( 0, inst.targets.Length )];
    }
}
