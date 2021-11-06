using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeTarget : MonoBehaviour
{

	public Transform[] targets;

	private void OnTriggerEnter2D(Collider2D collision)
	{
		print("BoomBoom");
		collision.gameObject.GetComponent<VOAgent>().targetPosition = targets[ Random.Range(0, targets.Length) ];
	}
}
