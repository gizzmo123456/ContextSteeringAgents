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

	protected override void PRINT_DEBUG_STOP_MOVE( string msg)
	{
		//print( $"{name} :: {msg} -> {GetAngleFromVectors( Forwards, direction )}" );
	}

}
