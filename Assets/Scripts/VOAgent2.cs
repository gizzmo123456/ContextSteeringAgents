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
	private RaycastHit2D[] detectedAgents = new RaycastHit2D[ maxDetectAgents + 1 ];	// We must initalize with an extra element since it will detect self.
	[SerializeField] private float detectRadius = 3f;
	[SerializeField] private LayerMask detectAgentLayerMask;

	// Current avoid Agent (if any)

	private VOAgent2 currentAvoidAgent;
	private float currentAvoidAgentDistance = Mathf.Infinity;

	[Header( "Target Config" )]
	[SerializeField] private Transform target;
	private Vector3 TargetPosition => target != null ? target.position : Vector3.zero;

	public bool DEBUG_DRAW = true;

	private void Start()
	{
		// Regiester the agent in to the Ticker.
		// This MUST be called in start as the VOAgentTick instance is created in Awake.

		VOAgentTick.Inst.RegisterAgent( this );

	}

	private void FixedUpdate()
	{

		// Cast a circel into the scene at out current location, to find if any other NPCs are in range.
		// Ourself will be in the result, and must be ignored.

		int rayHitCount = Physics2D.CircleCastNonAlloc( transform.position, detectRadius, Vector3.zero, detectedAgents, 0, detectAgentLayerMask );

		if ( rayHitCount > 1 )
		{
			CalculateAvoidAgent( rayHitCount );
		}
		else
		{

			// clear the current avoid agent if present
			currentAvoidAgent = null;
			currentAvoidAgentDistance = Mathf.Infinity;

			// go to target things.
		}

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

	private void CalculateAvoidAgent( int rayHitCount )
	{

		VOAgent2 closestAgent = null;
		float distance = Mathf.Infinity;
		float newAvoidDistance = -1;	// for the current avoid agent.

		// if we are currently tracking an agent, caculate its distance befor the other.
		// then we should skip it when we iterate other all detected agents.
		if ( currentAvoidAgent != null )
		{

			float dist = Vector2.Distance( transform.position, currentAvoidAgent.transform.position ) - agentRadius - currentAvoidAgent.agentRadius;

			// make sure its in the detect radius otherwises it will remain set when out of range.
			if ( dist <= detectRadius )
			{
				closestAgent = currentAvoidAgent;
				newAvoidDistance = distance = dist;
			}
			else
			{
				print( $"{name} Nope, {dist}" );
			}

		}

		for ( int i = 0; i < rayHitCount; i++ )
		{

			// ignore ourself.
			if ( detectedAgents[i].transform == transform )
				continue;

			VOAgent2 otherAgent = detectedAgents[i].transform.GetComponent<VOAgent2>();

			if ( otherAgent == currentAvoidAgent )
				continue;

			float dist = Vector2.Distance( transform.position, otherAgent.transform.position ) - agentRadius - otherAgent.agentRadius;

			if ( dist < 0f )
				print($"{name} Your in my space {otherAgent.name}");

			// track the cloest agent if not current tracking; or
			// start tracking the cloest agent that is at least 50% closer than the current agent.
			if ( closestAgent == null || ( currentAvoidAgent != null && dist < newAvoidDistance / 2f && dist < distance ) )
			{
				closestAgent = otherAgent;
				distance = dist;
			}

		}

		if ( closestAgent != currentAvoidAgent )
		{

			string caaName = currentAvoidAgent == null ? "null" : currentAvoidAgent.name;
			string caName = closestAgent == null ? null : closestAgent.name;

			print( $"{name} has changed avoid agents (f: {caaName} [{newAvoidDistance}], t: {caName} [{distance}])" );
		}

		currentAvoidAgent = closestAgent;
		currentAvoidAgentDistance = distance;

	}

	/// <summary>
	/// Data that needs to take effect once all of the agents have been updated in Fixed update.
	/// </summary>
	[System.Serializable]
	struct TickData
	{

	}


	// Debug

	private void OnDrawGizmos()
	{
		if ( !DEBUG_DRAW )
			return;

		

		if ( currentAvoidAgent == null )
			Gizmos.color = new Color( 0, 1, 0, 0.05f );
		else
			Gizmos.color = new Color( 1, 0, 0, 0.05f );

		Gizmos.DrawSphere( transform.position, detectRadius );

	}

}
