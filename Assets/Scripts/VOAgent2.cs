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
	[SerializeField] private float agentSpeed = 5f;

	[SerializeField] private float fov = 45f;   // degrees
	//private 

	[Header( "Agent Detect Setup" )]
	private const int maxDetectAgents = 3;  // define as const so its the same for all agents. ( i could define it in VOAgentTick, so it can be changed in the inspector)
	private RaycastHit2D[] detectedAgents = new RaycastHit2D[ maxDetectAgents + 1 ];	// We must initalize with an extra element since it will detect self.
	[SerializeField] private float detectRadius = 3f;
	[SerializeField] private LayerMask detectAgentLayerMask;

	// Desired path / Current avoid Agent (if any)

	private Vector3 desiredVectorNorm;

	private VOAgent2 currentAvoidAgent;
	private float currentAvoidAgentDistance = Mathf.Infinity;
	private Vector2[] currentAvoidTargets = new Vector2[2];		//The possible position that we can move towards.

	[Header( "Target Config" )]
	[SerializeField] private Transform target;
	private Vector3 TargetPosition => target != null ? target.position : Vector3.zero;

	public bool DEBUG = false;
	public bool DEBUG_DRAW = true;
	public bool move = false;
	public Transform TEMPMarker;
	private Transform[] markers;

	private void Awake()
	{
		GetComponent<SpriteRenderer>().color = agentColour;
	}

	private void Start()
	{
		// Regiester the agent in to the Ticker.
		// This MUST be called in start as the VOAgentTick instance is created in Awake.

		VOAgentTick.Inst.RegisterAgent( this );

		// NOTE we could just do this in DrawGizzmo
		markers = new Transform[2];
		for ( int i = 0; i < markers.Length; i++ )
		{
			markers[i] = Instantiate( TEMPMarker ); // new GameObject(name+"_marker_" + i).transform;
			markers[i].name = $"{name}-MARKER-{i}";
			markers[i].GetComponent<SpriteRenderer>().color = agentColour;
		}

	}

	private void FixedUpdate()
	{

		// Cast a circel into the scene at out current location, to find if any other NPCs are in range.
		// Ourself will be in the result, and must be ignored.

		int rayHitCount = Physics2D.CircleCastNonAlloc( transform.position, detectRadius, Vector3.zero, detectedAgents, 0, detectAgentLayerMask );


		desiredVectorNorm = ( target.position - transform.position ).normalized;
		float desiredAngleOffset = GetAngleFromVectors( Forwards, desiredVectorNorm );  // the angle required to be facing the target.

		//DEBUGING 
		
		Vector3 e = transform.eulerAngles;
		e.z += desiredAngleOffset;
		transform.eulerAngles = e;
		
		if ( DEBUG )
			print( $"{name} Angle to target: {desiredAngleOffset}" );

		// EOF DEBUGING

		if ( rayHitCount > 1 )
		{
			CalculateAvoidAgent( rayHitCount );

			if ( currentAvoidAgent != null )
			{

				float agentVelocityAngle = GetAngleFromVectors( Forwards, currentAvoidAgent.Forwards );

				// DEBUGING

				if ( DEBUG )
					print( $"{name}: agent vector angle {agentVelocityAngle}" );

				markers[0].transform.position = currentAvoidTargets[0];
				markers[1].transform.position = currentAvoidTargets[1];

			}

		}
		else
		{

			// clear the current avoid agent if present
			currentAvoidAgent = null;
			currentAvoidAgentDistance = Mathf.Infinity;

			// go to target things.
		}

		if ( move )
			transform.position = transform.position + ( Forwards * agentSpeed * Time.deltaTime );

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

			if ( GetAxisTimeTillAlligned( currentAvoidAgent ) < Mathf.Infinity )
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

		}

		for ( int i = 0; i < Mathf.Min(rayHitCount, detectedAgents.Length); i++ )
		{

			// ignore ourself.
			if ( detectedAgents[i].transform == transform )
				continue;

			VOAgent2 otherAgent = detectedAgents[i].transform.GetComponent<VOAgent2>();

			if ( otherAgent == currentAvoidAgent )
				continue;

			if ( GetAxisTimeTillAlligned( otherAgent ) == Mathf.Infinity )
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

		// update the possible targets
		if ( currentAvoidAgent != null )
		{
			
			Vector3 desiredPerend = Vector2.Perpendicular( desiredVectorNorm ).normalized;

			currentAvoidTargets[0] = currentAvoidAgent.transform.position - desiredPerend * ( agentRadius + currentAvoidAgent.agentRadius );
			currentAvoidTargets[1] = currentAvoidAgent.transform.position + desiredPerend * ( agentRadius + currentAvoidAgent.agentRadius );

		}

	}

	/// <summary>
	/// Gets the shortest angle between vectA and vectB
	/// </summary>
	private float GetAngleFromVectors( Vector2 vectA, Vector2 vectB, bool signed=true )
	{

		return signed ? Vector2.SignedAngle( vectA, vectB ) : Vector2.Angle( vectA, vectB );

	}

	/// <summary>
	/// Gets the sortest time to align along any unaligned axis in world space based on the agents current move forwards vector
	/// If the two agents never meet, Mathf.Infinity is returned
	/// </summary>
	private float GetAxisTimeTillAlligned( VOAgent2 otherAgent )
	{

		// This does not take the radius into account . TODO: <<

		Vector2 posDiff  = transform.position - otherAgent.transform.position;
		Vector2 vectDiff = otherAgent.Forwards * otherAgent.agentSpeed - Forwards * agentSpeed;

		Vector2 ttc = posDiff / vectDiff;
		float out_ttc = Mathf.Min( ttc.x, ttc.y );

		if ( out_ttc == 0 )
			out_ttc = Mathf.Max( ttc.x, ttc.y );
		
		if (DEBUG)
			print( $"{name} :: {posDiff} ## {vectDiff} ## {ttc} ## {out_ttc}" );

		if ( out_ttc <= 0 || ttc == Vector2.zero )
			return Mathf.Infinity;
		else
			return out_ttc;

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

		if ( target != null )
		{
			Gizmos.color = new Color(0,0,0, 0.25f);
			Gizmos.DrawCube( TargetPosition, new Vector3( 0.25f, 0.25f, 0.1f ) );

			Gizmos.color = new Color( 0, 0, 0, 1f );
			Gizmos.DrawLine( TargetPosition, transform.position );
		}

	}

}
