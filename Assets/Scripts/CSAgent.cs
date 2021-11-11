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
	[SerializeField] private float anget_avoidDistance = 0;
	private float agent_avoidDistance => agent_radius + anget_avoidDistance;

	[Header( "Obstacle detection" )]
	const int detect_objectCount = 3;
	[SerializeField] private RaycastHit2D[] detect_hits = new RaycastHit2D[ detect_objectCount + 1 ];   // +1 since we detect ourself.
	[SerializeField] private float detect_radius = 3;

	// context maps.
	// a value of -1 (or <0) is masked out
	private float[] danageMap  = new float[ cm_slots ];
    private float[] intressMap = new float[ cm_slots ];

	[Header( "target" )]
	[SerializeField] private Transform target;

	private void Awake()
	{

		GetComponent<SpriteRenderer>().color = agent_colour;
		transform.localScale = new Vector3( agent_radius, agent_radius, 1f );

	}

	private void Start()
	{
		
	}

	private void FixedUpdate()
	{
	
		ClearMaps();

		// fire our ray into the scene to find if any objects are near
		int rayHits = Physics2D.CircleCastNonAlloc( transform.position, detect_radius, Vector2.zero, detect_hits );

		if ( rayHits > 111 )
		{
			// do avoid things
		}
		else
		{
			// head to the target.
			RotateDelta( GetAngleFromVectors(Forwards, (target.position - transform.position).normalized) );
			Move( agent_moveSpeed );
		}
	}

	private void Move( float moveSpeed)
	{
		transform.position = transform.position + Forwards * moveSpeed;
	}

	private void RotateDelta( float rotateDelta )
	{
		transform.eulerAngles = transform.eulerAngles + new Vector3( 0, 0, rotateDelta );
	}

	private void ClearMaps()
	{

		for ( int i = 0; i < cm_slots; i++ )
		{
			danageMap[i]  = -1;
			intressMap[i] = -1;
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

}
