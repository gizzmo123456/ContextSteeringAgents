using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSAgent_FlowFeild : CSAgent
{

	[Header( "FlowFeid" )]
	[SerializeField] private FlowFeild flowFeild;


	[Header( "Target" )]
	public float targetLength = 2;
	private Vector3 direction = Vector3.zero;
	protected override Vector3 TargetPosition => (direction * targetLength) + transform.position;

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

		Debug.DrawLine( transform.position, TargetPosition, Color.blue, Time.deltaTime );

	}

	protected override void PRINT_DEBUG_STOP_MOVE( string msg)
	{
		print( $"{name} :: {msg} -> {GetAngleFromVectors( Forwards, direction )}" );
	}

}
