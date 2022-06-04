using GridNameSpace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;



[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
	private static readonly int sSpeedHash = Animator.StringToHash("Speed");
	private Animator mAnimator;
	LineRenderer lr;
	[HideInInspector]
	public Transform Player;
	public LayerMask HidableLayers;
	//public EnemyLineOfSightChecker LineOfSightChecker;
	public NavMeshAgent Agent;
	[Range(-1, 1)]
	[Tooltip("Lower is a better hiding spot")]
	public float HideSensitivity = 0;
	[Range(1, 10)]
	public float MinPlayerDistance = 5f;
	[Range(0, 5f)]
	public float MinObstacleHeight = 1.25f;
	[Range(0.01f, 1f)]
	public float UpdateFrequency = 0.25f;

	private Coroutine MovementCoroutine;
	private Collider[] Colliders = new Collider[10]; // more is less performant, but more options
	public MoveController controller;
	public MoveController Target;
	public Vector3 offset;
	HashSet<CoverTransform> coverTransforms = new HashSet<CoverTransform>();
	private Coroutine PatrolCoroutine;
	public Transform PatrolPointHolder;
	public List<Node> PatrolPoints = new List<Node>();
	private void Awake()
	{
		lr = GetComponent<LineRenderer>();
		Agent = GetComponent<NavMeshAgent>();
		mAnimator = GetComponent<Animator>();

	}

	public void Start()
	{
		Player = Target.transform;
		GetCoverTransformInRange();
		UpdateAvailableCover();
		CoverNode perfectCover = GetPerfectCover(coverTransforms);
		if (perfectCover != null)
		{
			Debug.Log($"perfect node {perfectCover?.node}");
			Instantiate(controller.ActiveFloor.prefab, perfectCover.node.LocalCoord + Vector3.up, Quaternion.identity).GetComponent<Renderer>().material.color = Color.yellow;
		}

		foreach (Transform point in PatrolPointHolder)
		{
			// for each point get the Floor under them, then get the node they sit on
			if (Physics.Raycast(point.position, Vector3.down, out RaycastHit hit, controller.ActiveFloor.floorLayer))
			{
				hit.transform.TryGetComponent<Floor>(out Floor FloorPoint);
				if (FloorPoint == null) continue;
				PatrolPoints.Add(FloorPoint.grid.GetNode(point));
			}
		}


		StartPatrol();

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
			controller.StartMoving(patrolPoints[0]);
			Debug.Log($" Only One Patrol Point :/ ...");
		}
		else
		{

			while (true)
			{
				i = i % patrolPoints.Count;

				// set the Final Destination
				controller.FinalDestination = patrolPoints[i];

				// patrolpoint on an other platform
				if (patrolPoints[i].grid.floor != controller.ActiveFloor)
				{
					// moving the the closest node Link leading the taht floor
					NodeLink nodeLink = controller.ClosestNodeLinkAvailable(patrolPoints[i].grid.floor);
					if (nodeLink == null)
					{
						Debug.Log($" cant Find a NodeLink To Cross to {patrolPoints[i].grid.floor}");
						i++;
						continue;
					}
					controller.StartMoving(nodeLink.node);

				}
				else
				{
					controller.StartMoving(patrolPoints[i]);
				}


				// we dont move to the next Patrol Point until we reach the Final Destination
				yield return new WaitUntil(() =>
				{
					return controller.curentPositon == controller.FinalDestination;
				});
				// we can make him look around him for a while then fo to the next point ( code here )

				yield return new WaitForSeconds(1f);
				mAnimator.SetFloat(sSpeedHash, Agent.speed);

				i++;
			}
		}
	}




	private CoverNode GetPerfectCover(HashSet<CoverTransform> coverTransforms)
	{
		if (coverTransforms?.Count == 0) return null;


		//Array.Sort(coverTransforms.ToArray(), new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });
		var SortedCoverTransforms = coverTransforms.OrderByDescending(cover => cover.height)
							.OrderBy(cover => cover.type).ToArray();



		HashSet<CoverNode> bestNodesOfTtransforms = new HashSet<CoverNode>();
		foreach (CoverTransform cover in SortedCoverTransforms)
		{
			bestNodesOfTtransforms.Add(cover.BestCoverAvailable);
			if (cover.BestCoverAvailable != null)
			{
				Instantiate(controller.ActiveFloor.prefab, cover.BestCoverAvailable.node.LocalCoord + Vector3.up * 0.5f, Quaternion.identity);

			}

		}





		CoverNode[] BestAvailableCovers = (from cover in bestNodesOfTtransforms
						   where cover != null && cover.Available && cover.InMovementRange
						   select cover).ToArray();

		//Array.Sort(BestAvailableCovers, new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });


		BestAvailableCovers = BestAvailableCovers.OrderByDescending(cover => cover.Value)
							.OrderBy(cover => cover.DistanceToPlayer)
							.OrderByDescending(cover => cover.CoverTransform.type).ToArray();

		// retrurn 
		return BestAvailableCovers[0];

	}

	void GetCoverTransformInRange()
	{

		int Hits = Physics.OverlapSphereNonAlloc(Agent.transform.position, 10, Colliders, HidableLayers);

		for (int i = 0; i < Hits; i++)
		{
			if (NavMesh.SamplePosition(Colliders[i].transform.position - (Player.position - Colliders[i].transform.position).normalized, out NavMeshHit hit, 2f, Agent.areaMask))
			{
				if (NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
				{
					// cover first Cover than create all potential Cover aroyn the CoverTransform
					Node nodeCov = controller.ActiveFloor.grid.GetNode(hit.position);
					CoverTransform obj = new CoverTransform(nodeCov.LocalCoord, nodeCov.X, nodeCov.Y, nodeCov.grid, Colliders[i].transform);
					CoverNode newCover = new CoverNode(nodeCov, obj);
					obj.CreatePotentialCover(newCover, null);
					coverTransforms.Add(obj);
				}
			}
		}

	}
	public void HandleGainSight(Transform Target)
	{
		if (MovementCoroutine != null)
		{
			StopCoroutine(MovementCoroutine);
		}
		Player = Target;
		//MovementCoroutine = StartCoroutine(TakeCover());
	}

	private void HandleLoseSight(Transform Target)
	{
		if (MovementCoroutine != null)
		{
			StopCoroutine(MovementCoroutine);
		}
		Player = null;
	}

	private void Update()
	{
		transform.LookAt(Player);

		if (Input.GetMouseButtonDown(0))
		{
			StopCoroutine(PatrolCoroutine);
		}
	}


	public void UpdateAvailableCover()
	{
		foreach (CoverTransform CoverPos in coverTransforms)
		{

			Vector3 dir1 = (Player.position - CoverPos.CoverPosition.position).normalized + Vector3.up * 0;

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

				if (dot > 0 && dot <= 0.01f)
					dot *= -1;
				else if (dot < 0 && dot >= -0.01f)
					dot *= -1;
				if (dot < 0)
				{
					//Debug.Log($" dot{coverNode.node} {dot} greed");

					Node PlayerPos = Target.curentPositon;
					coverNode.CalculateDistanceToPlayer(Target);
					coverNode.Available = true;

				}
				else
				{
					coverNode.Available = false;

					//Debug.Log($" dot{coverNode.node} {dot} red");
				}

				//GameObject.Instantiate(coverNode.node.grid.floor.prefab, coverNode.node.LocalCoord + Vector3.up, Quaternion.identity);
			}
			CoverPos.UpdateBestCover();
		}
	}

	private void OnDrawGizmos()
	{
		//return;



		if (controller.FinalDestination != null)
		{
			lr = GetComponent<LineRenderer>();


		}






		foreach (Transform point in PatrolPointHolder)
		{
			Gizmos.color = Color.black;
			Gizmos.DrawRay(point.position, Vector3.down);
		}

		Player = Target.transform;
		foreach (CoverTransform CoverPos in coverTransforms)
		{
			Gizmos.color = Color.black;

			Vector3 dir1 = (Player.position - CoverPos.CoverPosition.position).normalized + Vector3.up * 0;

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
			//Gizmos.DrawLine(Player.position, CoverPos.CoverPosition.position);

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
}