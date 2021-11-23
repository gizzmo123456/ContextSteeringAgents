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

	protected override (float lhs, float rhs) GetGradients( int intressSlotId )
	{

		float slotId = currentRotation / rotation_step;//*/ GetAngleFromVectors( Forwards, direction, false );

		float lhsGrad = 0.5f;
		float rhsGrad = 0.5f;

		float lhsSteps = -1;
		float rhsSteps = -1;

		if ( intressSlotId > slotId )
		{
			lhsSteps = ( cm_slots - intressSlotId + slotId );
			rhsSteps = ( intressSlotId - slotId );
			lhsGrad = 1f - lhsSteps / (float)cm_slots;
			rhsGrad = 1f - rhsSteps / (float)cm_slots;
		}
		else if ( intressSlotId < slotId )
		{
			lhsSteps = ( cm_slots - intressSlotId + slotId - cm_slots );
			rhsSteps = ( cm_slots + intressSlotId - slotId );
			lhsGrad = 1f - lhsSteps / (float)cm_slots;
			rhsGrad = 1f - rhsSteps / (float)cm_slots;
		}

		//float rhsRot = Mathf.Abs(currentSlotId - intressSlotId);// * rotation_step - angle;				// move right through the context map (++)
		//float lhsRot = Mathf.Abs(currentSlotId - ( cm_slots - intressSlotId ) );// * rotation_step + angle;    // move left through the context map (--)

		//float rhsGrad = rhsRot / cm_slots; //360f;
		//float lhsGrad = lhsRot / cm_slots; //360f;
		
		if ( DEBUG_GRADIENT )
			print( $"{name} :: a: {currentSlotId} ## lhs: {lhsSteps}/{lhsGrad} ## rhs: {rhsSteps}/{rhsGrad} ||");// ## fwr fwr vect: {Forwards} ## dir: {direction}" );

		return (rhsGrad, lhsGrad);
	}

	protected override void PRINT_DEBUG_STOP_MOVE( string msg)
	{
		//print( $"{name} :: {msg} -> {GetAngleFromVectors( Forwards, direction )}" );
	}

}
