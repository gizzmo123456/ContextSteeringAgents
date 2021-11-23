using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{

	private void Start()
	{

		// make sure there is an agent present.
		// and insure that the collider setting match.

		CSAgent agent = GetComponentInParent<CSAgent>();

		if ( agent == null )
		{
			Debug.LogError( "Error in collsionDetector, CSAgent not found. Removing script." );
			Destroy( this );
			return;
		}

		Collider2D collider = GetComponent<Collider2D>();

		if ( !(collider is CircleCollider2D) )
		{
			Destroy( collider );
			collider = gameObject.AddComponent<CircleCollider2D>();
		}

		( collider as CircleCollider2D ).isTrigger = true;
		( collider as CircleCollider2D ).radius = agent.AgentRadius;

	}

	private void OnTriggerEnter2D( Collider2D collision )
	{
		if ( collision.gameObject.GetComponent<CSAgent>() != null )
			SceneManagement.inst.CountCollsion("agent");
		else
			SceneManagement.inst.CountCollsion("other");

	}

}
