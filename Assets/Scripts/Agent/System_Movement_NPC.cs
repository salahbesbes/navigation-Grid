using GridNameSpace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class System_Movement_NPC : System_Movement
{

	public Transform PatrolPointHolder;
	public List<Node> PatrolPoints = new List<Node>();


	Coroutine PatrolCoroutine;
	public override void awake(AgentManager agent)
	{
		lr = GetComponent<LineRenderer>();
		mAnimator = GetComponent<Animator>();
		AiAgent = agent;
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
				// for each point get the Floor under them, then get the node they sit on
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


				// we dont move to the next Patrol Point until we reach the Final Destination
				yield return new WaitUntil(() =>
				{
					return curentPositon == FinalDestination;
				});
				// we can make him look around him for a while then fo to the next point ( code here )

				yield return new WaitForSeconds(1f);
				mAnimator.SetFloat(sSpeedHash, AiAgent.agent.speed);

				i++;
			}
		}
	}




	private void OnDrawGizmos()
	{
		//return;







		if (PatrolPointHolder != null)
		{

			foreach (Transform point in PatrolPointHolder)
			{
				Gizmos.color = Color.black;
				Gizmos.DrawRay(point.position, Vector3.down);
			}
		}


		if (AiAgent?.coverSystem?.coverTransforms == null) return;
		//AiAgent.Target = Target.transform;
		foreach (CoverTransform CoverPos in AiAgent.coverSystem.coverTransforms)
		{
			Gizmos.color = Color.black;

			Vector3 dir1 = (AiAgent.Target.position - CoverPos.CoverPosition.position).normalized + Vector3.up * 0;

			Vector3 dir2 = Vector3.zero;
			if (CoverPos.CoverPosition.localScale.x <= 0.4f)
			{
				// dir2 is the horizental line of the object its fixed becasue the cover does not have so much width
				Vector3 left = CoverPos.CoverPosition.position - new Vector3(CoverPos.CoverPosition.localScale.z / 2, 0, 0);
				Vector3 right = CoverPos.CoverPosition.position + new Vector3(CoverPos.CoverPosition.localScale.z / 2, 0, 0);
				dir2 = left - right;
				dir2 = Quaternion.Euler(0, Mathf.Abs(CoverPos.CoverPosition.eulerAngles.y % 360), 0) * dir2;
			}
			else
			{
				dir2 = Quaternion.Euler(0, 360 - Vector3.Angle(dir2, dir1), 0) * dir1;

			}
			//Debug.Log($"{Vector3.Angle(dir2, dir1)}");
			Gizmos.color = Color.grey;

			Gizmos.color = Color.magenta;
			//Gizmos.DrawLine(AiAgent.Target.position, CoverPos.CoverPosition.position);

			//Debug.Log($" {CoverPos.name} {CoverPos}");
			foreach (CoverNode coverNode in CoverPos.CoverList)
			{

				if (CoverPos.CoverPosition.localScale.x <= 0.4f)
				{
					float playerDot = Vector3.Dot(dir2, dir1);
					if (playerDot < 0)
					{
						dir2 *= -1;
						//Debug.Log($" player on the left");
					}
					else
					{
						//Debug.Log($" player on the right");
					}

				}


				Vector3 dir3 = (coverNode.node.LocalCoord - CoverPos.CoverPosition.position).normalized;
				dir3.y = dir2.y;
				float dot = Vector3.Dot(dir2, dir3);
				Gizmos.color = Color.black;

				if (dot > 0 && dot <= 0.01f)
					dot *= -1;
				else if (dot < 0 && dot >= -0.01f)
					dot *= -1;
				if (dot < 0)
				{
					//Debug.Log($" dot{coverNode.node} {dot} greed");
					Gizmos.color = Color.green;
					Gizmos.DrawLine(coverNode.node.LocalCoord, coverNode.node.LocalCoord + Vector3.up * 2);
				}
				else
				{
					//Debug.Log($" dot{coverNode.node} {dot} red");
					Gizmos.color = Color.red;
					Gizmos.DrawLine(coverNode.node.LocalCoord, coverNode.node.LocalCoord + Vector3.up * 2);
				}
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