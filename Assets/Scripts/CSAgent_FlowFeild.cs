using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSAgent_FlowFeild : CSAgent
{

	public bool respwanOnStart = false;

	[Header( "FlowFeid" )]
	public FlowFeild flowFeild;


	[Header( "Target" )]
	public float targetLength = 2;
	private Vector3 direction = Vector3.zero;
	protected override Vector3 TargetPosition => (direction * targetLength) + transform.position;

	public bool DEBUG_GRADIENT = false;

	protected override void Start()
	{
		base.Start();

		if ( respwanOnStart )
			AgentSpawn_FlowFeild.inst.RespwanAgent( this );

	}

	protected override void UpdateAgent()
	{
		Vector2 dir;

		if( flowFeild.GetFlowFeildDirectionVector( transform.position, out dir ) )
		{

			direction = dir;
			if ( DEBUG )
				print("set " + dir);
		}
		else if ( DEBUG )
			print( "not set" );

		if ( DEBUG_DRAW )
			Debug.DrawLine( transform.position, TargetPosition, Color.blue, Time.deltaTime );

		// TEMP Respwan agent.
		if ( flowFeild.InRangeOfSourceCell( transform.position, 1.25f ) )
		{
			AgentSpawn_FlowFeild.inst.RespwanAgent( this );
		}

	}

	protected override (float lhs, float rhs) GetGradients()
	{

		float angle = GetAngleFromVectors( Forwards, direction );
		
		float lhs = 0.9f;
		float rhs = 0.9f;

		if ( angle < 0 )
		{
			angle = Mathf.Abs( angle );
			lhs = ( 360f - angle ) / 360f;
			rhs = ( 360f - ( 360f - angle ) ) / 360f;

			//Debug.DrawLine( transform.position, transform.position + transform.right * -2f, Color.blue );
		}
		else if ( angle > 0 )
		{
			
			rhs = ( 360f - angle ) / 360f;
			lhs = ( 360f - ( 360f - angle ) ) / 360f;

			//Debug.DrawLine( transform.position, transform.position + transform.right * 2f, Color.red );

		}
		else
		{
			//Debug.DrawLine( transform.position, transform.position + transform.forward * 2f, Color.green );
		}

		if ( DEBUG_GRADIENT )
			print( $"{name} :: {angle} ## {lhs} ## {rhs} ## fwr {Forwards} ## dir {direction}" );

		return (lhs, rhs);
	}

	protected override void PRINT_DEBUG_STOP_MOVE( string msg)
	{
		//print( $"{name} :: {msg} -> {GetAngleFromVectors( Forwards, direction )}" );
	}

}
