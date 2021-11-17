using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Context Steering Agent utillatizing Context maps
 * 
 * -------------------------------------------------
 * Details
 * -------------------------------------------------
 * 
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
 * In Pro Game AI 3 (book) it sugest that each target or detected object shoud have its
 * own damage or intresst map respectivly. Which are then combine using the max values.
 * However, whats the point, when we can just test if the new value is higher than the last.
 * 
 * Apprently suming or averaging doent imporve it.
 * 
 * Once we have the final danage and intresst maps,
 * We find the lowest danage value and mask out higher values.
 * We then take the mask and apply it to the intress maps, zeroing out the masked values.
 * We then find the higest remaing value for the intress map and move in that direction.
 * 
 * -------------------------------------------------
 * Flaws and potential solutions
 * -------------------------------------------------
 * 
 * This works really well to avoid other agents,
 * However, its no so good at navagating a scene of static objects  to
 * locate a target. Its ok with smaller objects (3x3 ish) but it  offten reaches
 * a deadlock for larger objects (15x15). This is becase the direction of the
 * intress target can change as it navagates around (witch is expected behaviour
 * for a static target).
 * 
 * In this case we need a method to manipulate the target position or have a moving target.
 * In F1 2014 Context streeing was used for the driver AI. In there case they mapped the 
 * context maps to the lanes, which where positioned around a spline. The target was x% ahead
 * of there current position along the spline.
 * 
 * Another approch could be to use A* whit a navGrid or navMesh to navigate the scene.
 * The target would be the path points.
 * 
 * Another aproch could be to generate a flow-feild for the scene (ie. a cell based map 
 * witch contains the direction to the target), then the target would be the agents position +
 * the agents current cell direction. 
 * Dijkstra’s shortest path algorithm could be a good choose for generating a flow-feild.
 * Or A* Could also be another choose.
 * 
 * I think the flow-feild could make some really intressing crowed simulations. 
 * Or somethink like 1000 agents all tring to escape a office ...
 * 
 * -------------------------------------------------
 * Resources
 * -------------------------------------------------
 * 
 * Context Streeing:
 * Game AI Pro 3 - Context Steering, Behaviour-Driven Streeing, By Andrew Fray 
 * 
 * Flow-Feilds:
 * Gane AI Pro - Efficient Crowd Simulation For Mobile Games, By Graham
 * Dijkstra’s shortest path algorithm - https://www.geeksforgeeks.org/dijkstras-shortest-path-algorithm-greedy-algo-7/
 * 
 */

public class CSAgent : MonoBehaviour
{

    const int cm_slotsPer90Deg = 3;
    const int cm_slots = cm_slotsPer90Deg * 4;

	private Vector3 Forwards => transform.up;
	private Vector3 Right => transform.right;

	

	[Header( "Agent" )]
	public Color agent_colour;
	[SerializeField] private float agent_moveSpeed = 5f; // units per second
	[SerializeField] private float agent_radius = 0.5f;
	[SerializeField] private float agent_avoidRadius = 0; // the amount of distance that we should maintain between agents.
	[SerializeField] private float agent_avoidMaxRange = 4;	// this should be above 0 and not exceed the detect radius 
	private float agent_avoidDistance => agent_radius + agent_avoidRadius;

	[Header( "Obstacle detection" )]
	const int detect_objectCount = 6;
	[SerializeField] private RaycastHit2D[] detect_hits = new RaycastHit2D[ detect_objectCount + 1 ];   // +1 since we detect ourself.
	[SerializeField] private float detect_radius = 5;

	// context maps.
	// a value of -1 (or <0) is masked out
	private Vector2 map_keyStartVector = Vector2.up;
	private float[] map_danager  = new float[ cm_slots ];
    private float[] map_intress = new float[ cm_slots ];

	[Header( "target" )]
	public Transform target;
	protected virtual Vector3 TargetPosition => target.position;

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

		UpdateAgent();

		// ..
		ClearMaps();

		// fire our ray into the scene to find if any objects are near
		int rayHits = Physics2D.CircleCastNonAlloc( transform.position, detect_radius, Vector2.zero, detect_hits );

		// Set the intress map (We only have 1 desternation atm.)
		int map_intrSlotId = GetMapSlotID( TargetPosition );
		SetMapSlot( ref map_intress, map_intrSlotId, cm_slots/2, 1 );


		if ( rayHits > 1 )
		{

			for ( int i = 0; i < rayHits; i++ )
			{
				RaycastHit2D hit = detect_hits[i];

				if ( hit.transform == transform )
					continue;

				CSAgent otherAgent = hit.transform.GetComponent<CSAgent>();

				if ( otherAgent != null ) // avoid agents.
					AvoidObject( otherAgent.transform.position, agent_avoidDistance + otherAgent.agent_avoidDistance, "Agent" );
				else // avoid static objects
					AvoidObject( hit.point, agent_avoidDistance*2f, "Obs" );	// this does not really work for large objects
 
			} 

			MaskDanagerMap();
			int moveTo_slotID = MaskIntrestMap();
			float moveTo_heading = GetIntressGradentSlot( moveTo_slotID, 2 );

			//if ( DEBUG )
			//	print( $"{moveTo_slotID} Slot move to id: {moveTo_slotID} -> {moveTo_heading}" );

			if ( DEBUG_PRINT_MAP )
				print( $"{name} :: move to slot id -> {moveTo_slotID} = {moveTo_slotID * ( 360f / cm_slots )}" );

			transform.eulerAngles = new Vector3( 0, 0, moveTo_heading * ( 360f / cm_slots ) );
			Move( agent_moveSpeed );

		}
		else
		{
			// head to the target.
			RotateDelta( GetAngleFromVectors(Forwards, ( TargetPosition - transform.position).normalized) );
			Move( agent_moveSpeed );
		}

		PrintMaps();
	}

	protected virtual void UpdateAgent()
	{
		// TEMP (change target if close)
		if ( Vector2.Distance( transform.position, TargetPosition ) < 10f )
			target = AgentSpawn.GetTraget();
	}

	private void AvoidObject( Vector3 avoidPosition, float avoidDistance, string debugName="NONE")
	{

		int map_damSlotID = GetMapSlotID( avoidPosition );
		float dist = Vector2.Distance( transform.position, avoidPosition ) - avoidDistance;
		float danagerValue = 1f - dist / agent_avoidMaxRange;

		SetMapSlot( ref map_danager, map_damSlotID, 2, danagerValue );

		if ( DEBUG )
			print( $"{name} -> {debugName} :: { map_damSlotID } :: {dist} :: {danagerValue}" );

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

	/// <summary>
	/// Find the lowest danager value and mask the rest (with -1).
	/// </summary>
	private void MaskDanagerMap()
	{

		float lowestValue = 9999;
		float lowestStartIdx = -1;  // remember the lowest start index so we can mask the values that come before.

		if ( DEBUG )
			PrintDanagerMap( "Pre" );

		for ( int i = 0; i < cm_slots; i++ )
		{
			if ( map_danager[i] < lowestValue )
			{
				lowestValue = map_danager[i];
				lowestStartIdx = i;
			}
			else if ( map_danager[i] > lowestValue )
			{	
				// mask values above the lowest
				map_danager[i] = -1;
			}

		}

		// mask all values that come befor the lowest value.
		for ( int i = 0; i < lowestStartIdx; i++ )
		{
			map_danager[i] = -1;
		}

		if ( DEBUG )
			PrintDanagerMap( "Post" );


	}


	/// <summary>
	/// Applies the damager mask and return the highest slot id
	/// </summary>
	private int MaskIntrestMap()
	{

		float highestValue = -1;
		int highestSlotID = -1;

		for ( int i = 0; i < cm_slots; i++ )
		{
			
			if ( map_danager[i] == -1 )
				map_intress[i] = -1;

			if ( map_intress[i] > highestValue )
			{
				highestValue = map_intress[i];
				highestSlotID = i;
			}

		}

		return highestSlotID;

	}

	/// <summary>
	/// Finds the gradent value either side of slotId and returns the modified slotID where the two gradents meet.
	/// </summary>
	private float GetIntressGradentSlot( int slotId, int slots, float valueRange=1)
	{

		float sum = 0;
		float count = 0;

		bool  lhsCounting = true;
		bool  rhsCounting = true;

		for ( int i = 1; i <= slots; i++ )
		{

			int lhs = i - 1;
			int rhs = i + 1;

			if ( lhs < 0 )
				lhs += cm_slots;

			if ( rhs >= cm_slots )
				rhs -= cm_slots;

			if ( map_intress[lhs] <= 0)
			{
				lhsCounting = false;
			}
			else if ( lhsCounting )
			{
				sum -= map_intress[lhs];
				count++;
			}

			if ( map_intress[rhs] <= 0 )
			{
				rhsCounting = false;
			}
			else if ( rhsCounting )
			{
				sum += map_intress[rhs];
				count++;
			}

			if ( !lhsCounting && !rhsCounting )
				break;

		}

		if ( count == 0 )
			return slotId;

		float avg = sum / count;

		return slotId + avg / valueRange;

	}

	private void ClearMaps()
	{

		for ( int i = 0; i < cm_slots; i++ )
		{
			map_danager[i] = 0;
			map_intress[i] = 0;
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
		int id = Round( map_angle / ( 360F / cm_slots ) );

		if ( id == cm_slots )
			id = 0;

		return id;

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

	private void PrintDanagerMap( string msg )
	{

		string danStr = "|";

		for ( int i = 0; i < cm_slots; i++ )
		{
			danStr += map_danager[i] + " | ";
		}

		print( $"{name} :: {msg} Danager Map: {danStr}" );

	}
}
