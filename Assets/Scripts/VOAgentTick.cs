using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VOAgentTick : MonoBehaviour
{

	public static VOAgentTick Inst { get; private set; }

	private List<VOAgent2> agents;

	private void Awake()
	{
		if ( Inst == null )
			Inst = this;
	}

	public void RegisterAgent( VOAgent2 agent )
	{
		agents.Add( agent );
	}

	private void FixedUpdate()
	{

		// Ticket all agents
		foreach ( VOAgent2 agent in agents )
		{
			agent.Tick(Time.deltaTime);
		}

	}

}
