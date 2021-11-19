using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class target : MonoBehaviour
{
    public Color col;

	private void OnDrawGizmos()
	{
		Gizmos.color = col;

		Gizmos.DrawCube( transform.position, Vector3.one * 1.5f );
		
	}

}
