using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Context Steering Agent that utillatizes Context maps
 * There are 2 context maps (1 danage and 1 intresst)
 * The context maps are 1D and represent the basic directions
 * that the agent can move in.
 * 
 * for example if we have 8 slots in our context map where 
 * element 0 represents forwards
 * 
 *        7 0 1
 *         \|/
 *      6 --A-- 2
 *         /|\
 *        5 4 3
 *        
 * Each cell is given a score repective of the context map type.
 * 
 * Scoring
 * 
 * The Intress behaviour prefers closer targets (higher score) over targets far away
 * While the Danage behaviour prefers targets further away (lower score) and wants 
 * to keep a minimal distance. (Any objects beond the min distance can be ignored.)
 * 
 * Each agent should have it own danage and intress map, which should be combined using
 * the max values. However whats the point we should set the values into a the map if 
 * it a better (higher) score
 * 
 * Apprently suming or averaging doent imporve it.
 * 
 * Once we have the final danage and intresst maps,
 * We find the lowest danage value and mask out higher values.
 * We then take the mask and apply it the intress maps, zeroing out the values.
 * We then find the higest remaing value for the intress map and move in that direction.
 * [I wonder what happens if we subtract the danage from the intress]
 */

public class CSAgent : MonoBehaviour
{

    const int cm_slotsPer90Deg = 3;
    const int cm_slots = cm_slotsPer90Deg * 4;

	private Vector3 Forwards => transform.up;
	private Vector3 Right => transform.right;

	

	[Header( "Agent" )]
	[SerializeField] private Color agent_colour;
	[SerializeField] private float agent_moveSpeed = 5f; // units per second
	[SerializeField] private float agent_radius = 0.5f;
	[SerializeField] private float agent_avoidRadius = 0; // the amount of distance that we should maintain between agents.
	[SerializeField] private float agent_avoidMaxRange = 2;	// this should be above 0 and not exceed the detect radius 
	private float agent_avoidDistance => agent_radius + agent_avoidRadius;

	[Header( "Obstacle detection" )]
	const int detect_objectCount = 3;
	[SerializeField] private RaycastHit2D[] detect_hits = new RaycastHit2D[ detect_objectCount + 1 ];   // +1 since we detect ourself.
	[SerializeField] private float detect_radius = 3;

	// context maps.
	// a value of -1 (or <0) is masked out
	private Vector2 map_keyStartVector = Vector2.up;
	private float[] map_danager  = new float[ cm_slots ];
    private float[] map_intress = new float[ cm_slots ];

	[Header( "target" )]
	[SerializeField] private Transform target;


	// DEBUGING
	public bool DEBUG = false;
	public bool DEBUG_PRINT_MAP = false;

	private void Awake()
	{

		GetComponent<SpriteRenderer>().color = agent_colour;
		transform.localScale = new Vector3( agent_radius*2f, agent_radius*2f, 1f );

	}

	private void Start()
	{
		
	}

	private void FixedUpdate()
	{
	
		ClearMaps();

		// fire our ray into the scene to find if any objects are near
		int rayHits = Physics2D.CircleCastNonAlloc( transform.position, detect_radius, Vector2.zero, detect_hits );

		// Set the intress map (We only have 1 desternation atm.)
		int map_intrSlotId = GetMapSlotID( target.position );
		SetMapSlot( ref map_intress, map_intrSlotId, cm_slots/2, 1 );


		if ( rayHits > 1 )
		{

			for ( int i = 0; i < rayHits; i++ )
			{
				RaycastHit2D hit = detect_hits[i];

				CSAgent otherAgent = hit.transform.GetComponent<CSAgent>();
				Transform otherAgentTrans = hit.transform;

				if ( otherAgentTrans == transform )
					continue;
				
				int map_damSlotID = GetMapSlotID( otherAgentTrans.position );
				float dist = Vector2.Distance( transform.position, otherAgentTrans.position ) - agent_avoidDistance - otherAgent.agent_avoidDistance;
				float danagerValue = 1f - dist / agent_avoidMaxRange;

				SetMapSlot( ref map_danager, map_damSlotID, 2, danagerValue );

				// do avoid things
				if ( DEBUG )
					print( $"{name} :: { map_damSlotID } :: {dist} :: {danagerValue}" );
				
			}

		}
		else
		{
			// head to the target.
			RotateDelta( GetAngleFromVectors(Forwards, (target.position - transform.position).normalized) );
			Move( agent_moveSpeed );
		}

		PrintMaps();
	}

	private void Move( float moveSpeed)
	{
		transform.position = transform.position + Forwards * moveSpeed * Time.deltaTime;
	}

	private void RotateDelta( float rotateDelta )
	{
		transform.eulerAngles = transform.eulerAngles + new Vector3( 0, 0, rotateDelta );
	}

	private void SetMapSlot( ref float[] map, int slotID, int rolloffSlots, float value )
	{

		rolloffSlots++; // add on so we get at least 'rolloffSlots' above 0

		if ( value > map[slotID] )
			map[slotID] = value;
		
		// compute the rolloff values.
		for ( int i = 1; i <= rolloffSlots; i++ )
		{
			int rhs = slotID + i;
			int lhs = slotID - i;

			if ( rhs >= cm_slots )
				rhs -= cm_slots;

			if ( lhs < 0 )
				lhs += cm_slots;

			float valueMultiplier = 1f - (float)i / (float)rolloffSlots;
			float val = value * valueMultiplier;

			if ( val > map[rhs] )
				map[rhs] = val;

			if ( val > map[lhs] )
				map[lhs] = val;

		}
		

	}

	private void ClearMaps()
	{

		for ( int i = 0; i < cm_slots; i++ )
		{
			map_danager[i]  = -1;
			map_intress[i] = -1;
		}

	}

	// Helpers
	/// <summary>
	/// Gets the shortest angle between vectA and vectB
	/// </summary>
	private float GetAngleFromVectors( Vector2 vectA, Vector2 vectB, bool signed = true )
	{

		return signed ? Vector2.SignedAngle( vectA, vectB ) : Vector2.Angle( vectA, vectB );

	}

	private float Get360AngleFromVectors( Vector2 vectA, Vector2 vectB )
	{
		float angle = GetAngleFromVectors( vectA, vectB );

		return angle >= 0 ? angle : 360 + angle;
	}

	/// <summary>
	/// Rounds half up
	/// </summary>
	/// <returns></returns>
	private int Round( float value )
	{
		return value % 1 < 0.5f ? Mathf.FloorToInt( value ) : Mathf.CeilToInt( value );
	}

	private int GetMapSlotID( Vector3 objectPosition )
	{
		float map_angle = Get360AngleFromVectors( map_keyStartVector, ( objectPosition - transform.position ).normalized );

		return Round( map_angle / ( 360 / cm_slots ) );

	}

	// Debuging

	private void PrintMaps()
	{

		if ( !DEBUG_PRINT_MAP )
			return;

		string intrStr = "| ";
		string danStr = "|";

		for ( int i = 0; i < cm_slots; i++ )
		{
			intrStr += map_intress[i] + " | ";
			danStr += map_danager[i] + " | ";
		}

		print( $"Intresst Map: {intrStr}" );
		print( $"Danager Map: {danStr}" );

		DEBUG_PRINT_MAP = false;
	}
}
