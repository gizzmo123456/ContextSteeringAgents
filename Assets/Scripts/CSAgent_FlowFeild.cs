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

	protected override (float lhs, float rhs) GetGradients()
	{

		float angle = GetAngleFromVectors( Forwards, direction );
		
		float lhs = 0.9f;
		float rhs = 0.9f;

		if ( angle > 0 )
		{
			lhs = ( 360f - angle ) / 360f;
			rhs = ( 360f - ( 360f - angle ) ) / 360f;
		}
		else if ( angle < 0 )
		{
			angle = Mathf.Abs( angle );
			rhs = ( 360f - angle ) / 360f;
			lhs = ( 360f - ( 360f - angle ) ) / 360f;
		}

		return (lhs, rhs);
	}

	protected override void PRINT_DEBUG_STOP_MOVE( string msg)
	{
		print( $"{name} :: {msg} -> {GetAngleFromVectors( Forwards, direction )}" );
	}

}
