using GridNameSpace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class System_Movement_NPC : System_Movement, IBehaviour
{
	public Transform PatrolPointHolder;
	public List<Node> PatrolPoints = new List<Node>();

	private Coroutine PatrolCoroutine;

	public override void awake(AgentManager agent)
	{
		lr = GetComponent<LineRenderer>();
		mAnimator = GetComponent<Animator>();
		AiAgent = agent;
	}

	public override void update()
	{
		updateProperties();
		//AgentInputSystem();
		//NodeInRange = GetRangeOfMevement(CurentPositon, 8);
	}

	public override void start()
	{
		foreach (var nodeLink in ActiveFloor.nodeLinks)
		{
			nodeLink.AddObservable(this);
			nodeLink.Destiation.AddObservable(this);
		}

		if (PatrolPointHolder != null)
		{
			foreach (Transform point in PatrolPointHolder)
			{
				// for each point get the Floor under them, then get the node they
				// sit on
				if (Physics.Raycast(point.position, Vector3.down, out RaycastHit hit, FloorLayer))
				{
					hit.transform.TryGetComponent<Floor>(out Floor FloorPoint);
					if (FloorPoint == null) continue;
					PatrolPoints.Add(FloorPoint.grid.GetNode(point));
				}
			}
		}

		//StartPatrol();
	}

	private void StartPatrol()
	{
		PatrolCoroutine = StartCoroutine(Patrol(PatrolPoints));
	}

	private IEnumerator Patrol(List<Node> patrolPoints)
	{
		int i = 0;

		if (patrolPoints.Count == 0)
		{
			Debug.Log($" PATROL Points IS EMPTY  ...");

			yield break;
		}
		if (patrolPoints.Count == 1)
		{
			StartMoving(patrolPoints[0]);
			Debug.Log($" Only One Patrol Point :/ ...");
		}
		else
		{
			while (true)
			{
				i = i % patrolPoints.Count;

				// set the Final Destination
				FinalDestination = patrolPoints[i];

				// patrolpoint on an other platform
				if (patrolPoints[i].grid.floor != ActiveFloor)
				{
					// moving the the closest node Link leading the taht floor
					NodeLink nodeLink = ClosestNodeLinkAvailable(patrolPoints[i].grid.floor);
					if (nodeLink == null)
					{
						Debug.Log($" cant Find a NodeLink To Cross to {patrolPoints[i].grid.floor}");
						i++;
						continue;
					}
					StartMoving(nodeLink.node);
				}
				else
				{
					StartMoving(patrolPoints[i]);
				}

				// we dont move to the next Patrol Point until we reach the Final
				// Destination
				yield return new WaitUntil(() =>
				{
					return CurentPositon == FinalDestination;
				});
				// we can make him look around him for a while then fo to the next
				// point ( code here )

				yield return new WaitForSeconds(1f);
				mAnimator.SetFloat(sSpeedHash, AiAgent.agent.speed);

				i++;
			}
		}
	}

	private void OnDrawGizmos()
	{
		//return;

		if (AiAgent == null) return;
		NodeInRange = GetRangeOfMevement(CurentPositon, 8);
		foreach (var item in NodeInRange.Where(el => el.firstRange))
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(item.node.LocalCoord, 0.1f);
		}
		foreach (var item in NodeInRange.Where(el => el.SecondRange))
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(item.node.LocalCoord, 0.1f);
		}

		if (PatrolPointHolder != null)
		{
			foreach (Transform point in PatrolPointHolder)
			{
				Gizmos.color = Color.black;
				Gizmos.DrawRay(point.position, Vector3.down);
			}
		}


	}


	public override void AgentInputSystem()
	{
		if (Input.GetMouseButtonDown(1) && PatrolCoroutine == null && AiAgent.agent.name != "player")
		{
			StartPatrol();
		}
	}
}