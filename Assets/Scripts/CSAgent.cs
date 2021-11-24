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

	private const bool COUNT_AGENT_COLLISIONS = true;

    private const int cm_slotsPer90Deg = 3;
    protected const int cm_slots = cm_slotsPer90Deg * 4;
	protected const float rotation_step = 360f / cm_slots;

	protected Vector3 Forwards => transform.up;
	protected Vector3 Right => transform.right;

	

	[Header( "Agent" )]
	public Color agent_colour;
	[SerializeField] public float agent_moveSpeed = 5f; // units per second
	[SerializeField] private float agent_radius = 0.5f;
	public float AgentRadius => agent_radius;
	[SerializeField] private float agent_avoidRadius = 0; // the amount of distance that we should maintain between agents.
	[SerializeField] private float agent_objectAvoidRadius = 0.5f; // the amount of distance that we should maintain between agents.

	[SerializeField] private float agent_avoidMaxRange = 4; // this should be above 0 and not exceed the detect radius
	[SerializeField] private float agent_objectAvoidMaxRange = 1;

	private float agent_avoidDistance => agent_radius + agent_avoidRadius;

	[Header( "Obstacle detection" )]
	const int detect_objectCount = 10;
	[SerializeField] private RaycastHit2D[] detect_hits = new RaycastHit2D[ detect_objectCount + 1 ];   // +1 since we detect ourself.
	[SerializeField] private float detect_radius = 5;

	// context maps.
	// a value of -1 (or <0) is masked out
	private Vector2 map_keyStartVector = Vector2.up;
	private float[] map_danager = new float[ cm_slots ];
	private int[]   map_mask    = new int  [ cm_slots ];
	private float[] map_intress = new float[ cm_slots ];

	[Header( "target" )]
	public Transform target;
	protected virtual Vector3 TargetPosition => target.position;

	protected int currentSlotId = 0;	// mostly for debuging :)
	protected float currentRotation = 0;
	protected float targetRotation = 0;
	protected float rotStep = 0;
	[SerializeField] protected float maxRotateStep = 90f;

	// DEBUGING
	public bool DEBUG = false;
	public bool DEBUG_PRINT_MAP = false;
	public bool DEBUG_STOP_MOVE = false;
	public bool DEBUG_DRAW_AGENT = false;
	public const bool DEBUG_DRAW_ALL = false;
	public bool DEBUG_DRAW => DEBUG_DRAW_AGENT || DEBUG_DRAW_ALL;
	public bool DEBUG_DRAW_MAP = false;
	public bool DEBUG_GRADIENT_2 = false;
	public bool DEBUG_GRADIENT = false;

	private void Awake()
	{

		agent_colour = new Color( Random.value, Random.value, Random.value, 1f );

		transform.localScale = new Vector3( agent_radius*2f, agent_radius*2f, 1f );

		// Spawn the collisionCounter.
		if ( COUNT_AGENT_COLLISIONS )
		{
			GameObject collisionCount = new GameObject();
			collisionCount.AddComponent<CollisionDetector>();
			collisionCount.transform.parent = transform;
			collisionCount.transform.localPosition = Vector3.zero;
			collisionCount.transform.localRotation = Quaternion.identity;
			
		}

	}

	protected virtual void Start()
	{

		if ( target )
			GetComponent<SpriteRenderer>().color = target.GetComponent<target>().col;
		else
			GetComponent<SpriteRenderer>().color = agent_colour;

		currentRotation = transform.eulerAngles.z;
	}

	private void FixedUpdate()
	{

		if ( !SceneManagement.started )
			return;

		UpdateAgent();

		ClearMaps();

		// fire our ray into the scene to find if any objects are near
		int rayHits = Physics2D.CircleCastNonAlloc( transform.position, detect_radius, Vector2.zero, detect_hits );

		// Set the intress map (We only have 1 desternation atm.)
		int map_intrSlotId = GetMapSlotID( TargetPosition );
		SetMapSlot( ref map_intress, map_intrSlotId, 0, 0, 0, 1 );

		if ( rayHits > 1 )
		{

			for ( int i = 0; i < rayHits; i++ )
			{
				RaycastHit2D hit = detect_hits[i];

				if ( hit.transform == transform )
					continue;

				CSAgent otherAgent;

				if ( hit.transform.TryGetComponent( out otherAgent) )
				{
					AvoidObject( otherAgent.transform.position, agent_avoidDistance + otherAgent.agent_avoidDistance, agent_avoidMaxRange, 2, "Agent" );
				}
				else // avoid static objects
				{
					Vector2 cloestPos = hit.collider.ClosestPoint( transform.position );
					AvoidObject( cloestPos, agent_objectAvoidRadius, agent_objectAvoidMaxRange, 2, "Obs" );   // this does not really work for large objects

					if ( DEBUG_DRAW )
					{
						Debug.DrawLine( cloestPos + new Vector2( -0.25f, 0 ), cloestPos + new Vector2( 0.25f, 0 ), Color.magenta, Time.deltaTime );
						Debug.DrawLine( cloestPos + new Vector2( 0, -0.25f ), cloestPos + new Vector2( 0, 0.25f ), Color.magenta, Time.deltaTime );
					}

					if ( DEBUG )
						Debug.Log( "cloest hit :: " + cloestPos, hit.transform );
				}
			}

			CreateMaskMap();
			int moveTo_slotID = GetBestIntresstSlotId();
			float moveTo_heading = GetIntressGradentSlot( moveTo_slotID, 1 );

			float mtheading = GetIntressGradentSlot( moveTo_slotID, 1 );
			mtheading = (mtheading * rotation_step + 90f) * Mathf.Deg2Rad;
			Vector3 pos = new Vector3( Mathf.Cos( mtheading ), Mathf.Sin( mtheading ), 0 ) * 4f;

			if ( DEBUG_DRAW_MAP )
				Debug.DrawLine( transform.position, transform.position + pos, Color.green, Time.deltaTime );

			targetRotation = moveTo_heading * rotation_step;
			targetRotation = ClampRotation( targetRotation );

			float rotUpdate = targetRotation - currentRotation;
			rotUpdate = ClampRotation( rotUpdate );

			if ( rotUpdate > 180f )
				rotUpdate = -(360 - rotUpdate);	// why is this negative ?

			rotUpdate *= 0.5f;

			//if ( DEBUG )
			//	print( $"{moveTo_slotID} Slot move to id: {moveTo_slotID} -> {moveTo_heading}" );

			if ( DEBUG )
				print( $"{name} :: move to slot id -> {moveTo_slotID} = {moveTo_slotID * rotation_step} (Gradent Heading: {moveTo_heading} = {moveTo_heading * rotation_step})" );

			if ( !DEBUG_STOP_MOVE )
			{
				// rotate the agent find how much danager we are moving into
				// slowing down the agent to reduce the risk :)
				RotateDelta( rotUpdate );

				currentSlotId = GetMapSlotID( targetRotation ); // currentRotation );
				float danager = Mathf.Clamp01( map_danager[currentSlotId] ) ;
				 
				Move( agent_moveSpeed );

			}
			else
			{
				PRINT_DEBUG_STOP_MOVE();
				
			}
		}
		else if ( ! DEBUG_STOP_MOVE )
		{
			// head to the target.
			RotateDelta( GetAngleFromVectors(Forwards, ( TargetPosition - transform.position).normalized) );
			Move( agent_moveSpeed );

		}
		else
		{
			PRINT_DEBUG_STOP_MOVE( "To Target. " );
		}

		PrintMaps();

		if ( DEBUG_DRAW_MAP )
			DrawMap();
	}

	protected virtual void UpdateAgent()
	{
		// TEMP (change target if close)
		if ( Vector2.Distance( transform.position, TargetPosition ) < 15f )
		{
			target = AgentSpawn.GetTraget();
			GetComponent<SpriteRenderer>().color = target.GetComponent<target>().col;
		}
	}

	private void AvoidObject( Vector3 avoidPosition, float avoidDistance, float maxAvoidDistance, int rollOffValues=2, string debugName="NONE")
	{

		int map_damSlotID = GetMapSlotID( avoidPosition );
		float dist = Vector2.Distance( transform.position, avoidPosition ) - avoidDistance;
		float danagerValue = 1f - dist / maxAvoidDistance;

		SetMapSlot( ref map_danager, map_damSlotID, rollOffValues, 0.5f, 0.5f, danagerValue );

		if ( DEBUG )
			print( $"{name} -> {debugName} :: { map_damSlotID } :: {dist} :: {danagerValue}" );

	}

	private void Move( float moveSpeed)
	{
		transform.position = transform.position + Forwards * moveSpeed * Time.deltaTime;
	}

	private void RotateDelta( float rotateDelta )
	{

		if ( Mathf.Abs( rotateDelta ) > maxRotateStep )
			rotateDelta = maxRotateStep * ( rotateDelta > 0 ? 1f : -1f );

		rotStep = rotateDelta;

		currentRotation = ClampRotation( currentRotation + rotateDelta );

		transform.eulerAngles = new Vector3( 0, 0, targetRotation );// currentRotation );
	}

	private void SetMapSlot( ref float[] map, int slotID, int gradientSlots, float lhsGradientMultiplier, float rhsGradientMultiplier, float value )
	{

		if ( map == map_intress && DEBUG_GRADIENT )
		{
			print( $"Set intress map {name} :: {slotID} -> {value}" );
		}

		if ( value > map[slotID] )
			map[slotID] = value;

		float lhs_value = value;
		float rhs_value = value;

		// compute the rolloff values.
		for ( int i = 1; i <= gradientSlots; i++ )
		{
			int rhs = slotID + i;
			int lhs = slotID - i;

			if ( rhs >= cm_slots )
				rhs -= cm_slots;

			if ( lhs < 0 )
				lhs += cm_slots;

			rhs_value *= rhsGradientMultiplier;// * ( 1f - i / gradientSlots );
			lhs_value *= lhsGradientMultiplier;// * ( 1f - i / gradientSlots );

			if ( rhs_value > map[rhs] )
				map[rhs] = rhs_value;

			//print( $"{name} - {lhs}" );

			if ( lhs_value > map[lhs] )
				map[lhs] = lhs_value;

		}
		

	}

	/// <summary>
	/// Creates the mask map from the danager map, using the highest values.
	/// Find the lowest danager value and mask the rest (with -1).
	/// </summary>
	private void CreateMaskMap()
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
				map_mask[i] = -1;
			}

		}

		// mask all values that come befor the lowest value.
		for ( int i = 0; i < lowestStartIdx; i++ )
		{
			map_mask[i] = -1;
		}

		if ( DEBUG )
			PrintDanagerMap( "Post" );


	}

	private int GetBestIntresstSlotId()
	{
		
		float agentCurrentSlot = currentRotation / rotation_step;
		float maxDistance = cm_slots / 2f;

		float maxIntress = -1;
		int maxIntressId = -1;

		for ( int i = 0; i < cm_slots; i++ )
		{

			if ( map_mask[i] == -1 )
			{
				map_intress[i] = -1;
			}
			else
			{
				
				// Get the min distance between this slot and the agents current slot
				float slotsFromPlayer = Mathf.Min( Mathf.Abs( agentCurrentSlot - i ), cm_slots - Mathf.Abs( agentCurrentSlot - i ) );
				float intress = ( 1f - slotsFromPlayer / maxDistance ) * 0.9f; // * by 0.9 so its always less than the intresst slot.

				if ( intress > map_intress[i] )
					map_intress[i] = intress;
				
				if ( map_intress[i] > maxIntress )
				{
					maxIntress = map_intress[i];
					maxIntressId = i;
				}

			}
			
		}

		return maxIntressId;

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

			int lhs = slotId - i;
			int rhs = slotId + i;

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
			map_mask[i]    = 0;
			map_intress[i] = 0;
		}

	}

	// Helpers
	/// <summary>
	/// Gets the shortest angle between vectA and vectB
	/// </summary>
	protected float GetAngleFromVectors( Vector2 vectA, Vector2 vectB, bool signed = true )
	{

		return signed ? Vector2.SignedAngle( vectA, vectB ) : Vector2.Angle( vectA, vectB );

	}

	private float Get360AngleFromVectors( Vector2 vectA, Vector2 vectB )
	{
		float angle = GetAngleFromVectors( vectA, vectB );

		return angle >= 0 ? angle : 360 + angle;
	}

	private int GetMapSlotID( Vector3 objectPosition )
	{
		float map_angle = Get360AngleFromVectors( map_keyStartVector, ( objectPosition - transform.position ).normalized );
		int id = Round( map_angle / rotation_step );

		if ( id == cm_slots )
			id = 0;

		return id;

	}

	private int GetMapSlotID( float rotation )
	{

		int id = Round( rotation / rotation_step );

		if ( id == cm_slots )
			id = 0;

		return id;

	}

	/// <summary>
	/// Rounds half up
	/// </summary>
	/// <returns></returns>
	private int Round( float value )
	{
		return value % 1 < 0.5f ? Mathf.FloorToInt( value ) : Mathf.CeilToInt( value );
	}

	private float ClampRotation( float angle )
	{
		if ( angle < 0 )
			angle += 360;
		else if ( angle > 360 )
			angle -= 360;

		return angle;
	}

	// Debuging

	private void PrintMaps()
	{

		if ( !DEBUG_PRINT_MAP )
			return;

		string intrStr = "| ";
		string danStr  = "| ";
		string maskStr = "| ";

		for ( int i = 0; i < cm_slots; i++ )
		{
			string current = "";
			if ( i == currentSlotId )
				current = "<";

			intrStr +=  $"{i}: {map_intress[i]} :: {current} | ";
			danStr +=   $"{i}: {map_danager[i]} :: {current} | ";
			maskStr +=  $"{i}: {map_mask[i]} :: {current} | ";
		}

		print( $"Intresst Map: {intrStr}" );
		print( $"Danager Map: {danStr}" );
		print( $"Danager Map: {maskStr}" );

		DEBUG_PRINT_MAP = false;
	}

	private void DrawMap()
	{
		// Draw the map around the player

		for ( int i = 0; i < map_intress.Length; i++ )
		{

			float angle = (i * rotation_step + 90f) * Mathf.Deg2Rad;
			Vector3 pos = new Vector3( Mathf.Cos( angle ), Mathf.Sin( angle ), 0 ) * 4f;// * map_intress[i];

			if ( map_intress[i] >= 0f )
				Debug.DrawLine( transform.position, transform.position + pos * map_intress[i], Color.magenta, Time.deltaTime );
			else if ( map_mask[i] < 0f )
			{
				Debug.DrawLine( transform.position, transform.position + pos * map_danager[i], Color.cyan, Time.deltaTime );
			}
		}
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

	private void PrintIntresstMap( string msg )
	{

		string intStr = "|";

		for ( int i = 0; i < cm_slots; i++ )
		{
			intStr += map_intress[i] + " | ";
		}

		print( $"{name} :: {msg} Intress Map: {intStr}" );

	}

	protected virtual void PRINT_DEBUG_STOP_MOVE( string msg="" ) { }
}
