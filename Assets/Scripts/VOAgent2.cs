using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VOAgent2 : MonoBehaviour
{

	// Genral
	private Rigidbody2D rb;

	// Helpers
	private Vector3 Forwards => transform.up;
	private Vector3 Right => transform.right;

	// Tick Stores. (im not sure i need all of this)
	[SerializeField] private TickData nextTick;
	[SerializeField] private TickData currentTick;
	[SerializeField] private TickData previousTick;

	[Header( "Agent Setup" )]
	[SerializeField] private Color agentColour = Color.red;
	[SerializeField] private float agentRadius = 0.5f;

	[Header( "Agent Detect Setup" )]
	private const int maxDetectAgents = 3;  // define as const so its the same for all agents. ( i could define it in VOAgentTick, so it can be changed in the inspector)
	private RaycastHit2D[] detectedAgents = new RaycastHit2D[ maxDetectAgents ];
	[SerializeField] private float detectRadius = 3f;

	private void Start()
	{
		// Regiester the agent in to the Ticker.
		// This MUST be called in start as the VOAgentTick instance is created in Awake.

		VOAgentTick.Inst.RegisterAgent( this );

	}

	private void FixedUpdate()
	{
		
	}

	/// <summary>
	/// Called via VOAgentTick affter all agents have updated.
	/// This is when the new values can take effect.
	/// </summary>
	/// <param name="tempStep"></param>
	public void Tick( float tempStep )
    {

		previousTick = currentTick;
		currentTick = nextTick;
		nextTick = default;

	}

	/// <summary>
	/// Data that needs to take effect once all of the agents have been updated in Fixed update.
	/// </summary>
	[System.Serializable]
	struct TickData
	{

	}

}
