using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManagement : MonoBehaviour
{
    
    public static bool started = false;
    public static SceneManagement inst;

	public float startInTime = 5f;
	public float endInTime = 120f;

    private int agentCollisionCount = 0;  // this value should of counted both collision from both agents, therefor the values is collsionCount / 2;
    private int collisionCount = 0; 
    public int CollisionCount => collisionCount;
    public int AgentCollisionCount => Mathf.FloorToInt(agentCollisionCount / 2f);
	public int AllAgentCount => CollisionCount + AgentCollisionCount;

	private void Awake()
	{
        inst = this;
	}

	private void Start()
	{
		Invoke( "StartScene", startInTime );
	}

	private void StartScene()
	{
		started = true;

		if ( endInTime > 0f)
			Invoke( "EndScene", endInTime );

	}

	private void EndScene()
	{
		Time.timeScale = 0;
	}

	public void CountCollsion( string counter )
    {
		if ( counter == "agent" )
			agentCollisionCount++;
		else
			collisionCount++;
	}

}
