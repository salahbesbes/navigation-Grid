using GridNameSpace;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


class CoverNode : Node
{
	public CoverTransform CoverTransform;
	public CoverNode(Vector3 localCoord, int x, int y, FloorGrid grid, CoverTransform CoverTransform) : base(localCoord, x, y, grid)
	{
		this.CoverTransform = CoverTransform;
	}
}

class CoverTransform : Node
{
	public List<Node> coverList = new List<Node>();
	public string name = "default";
	public int id = 0;
	public Transform CoverPosition;


	public CoverTransform(Vector3 localCoord, int x, int y, FloorGrid grid, Transform transform) : base(localCoord, x, y, grid)
	{
		name = transform.name;
		id = transform.GetInstanceID();
		CoverPosition = transform;
	}


	public void updatePotentialCover(Node StartNode)
	{

		Vector3 offset = Vector3.up * 0.2f;
		if (StartNode.isObstacle)
		{

			foreach (Node node in StartNode.neighbours)
			{
				if (!coverList.Contains(node) && node.isObstacle == false)
				{
					updatePotentialCover(node);

				}
			}
			return;
		}

		bool potentialcoverExist = false;

		Vector3[] directions = new Vector3[4] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
		foreach (Vector3 dir in directions)
		{
			Ray ray = new Ray(StartNode.LocalCoord + offset, dir);
			if (Physics.SphereCast(ray, 0.25f, out RaycastHit hit, 1f, grid.floor.ObstacleLayer))
			{
				if (hit.transform.GetInstanceID() == id)
				{
					//Debug.Log($" {StartNode} is added");
					coverList.Add(StartNode);
					potentialcoverExist = true;
				}
			}

		}




		if (potentialcoverExist == false)
		{
			return;
		}


		foreach (Node node in StartNode.neighbours)
		{
			if (!coverList.Contains(node))
			{
				updatePotentialCover(node);
			}
		}


	}
}



[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
	[HideInInspector]
	public Transform Player;
	public LayerMask HidableLayers;
	public EnemyLineOfSightChecker LineOfSightChecker;
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
	public Transform Target;
	public Vector3 offset;
	private void Awake()
	{
		Agent = GetComponent<NavMeshAgent>();

		LineOfSightChecker.OnGainSight += HandleGainSight;
		LineOfSightChecker.OnLoseSight += HandleLoseSight;

	}

	public void Start()
	{
		Player = Target;
		GetCoverTransformInRange();
	}


	HashSet<CoverTransform> coverTransforms = new HashSet<CoverTransform>();
	void GetCoverTransformInRange()
	{

		int Hits = Physics.OverlapSphereNonAlloc(Agent.transform.position, LineOfSightChecker.Collider.radius, Colliders, HidableLayers);

		for (int i = 0; i < Hits; i++)
		{
			if (NavMesh.SamplePosition(Colliders[i].transform.position - (Player.position - Colliders[i].transform.position).normalized, out NavMeshHit hit, 2f, Agent.areaMask))
			{


				if (NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
				{
					// cover point 

					Node CoverNode = controller.floor.grid.GetNode(null, hit.position);
					CoverTransform obj = new CoverTransform(CoverNode.LocalCoord, CoverNode.X, CoverNode.Y, CoverNode.grid, Colliders[i].transform);
					//CoverNode newCover = new CoverNode(CoverNode.LocalCoord, CoverNode.X, CoverNode.Y, CoverNode.grid, obj);
					obj.updatePotentialCover(CoverNode);
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
		transform.LookAt(Target);
	}




















	public List<Node> covers = new List<Node>();



	public int ColliderArraySortComparer(Collider A, Collider B)
	{
		if (A == null && B != null)
		{
			return 1;
		}
		else if (A != null && B == null)
		{
			return -1;
		}
		else if (A == null && B == null)
		{
			return 0;
		}
		else
		{
			return Vector3.Distance(Agent.transform.position, A.transform.position).CompareTo(Vector3.Distance(Agent.transform.position, B.transform.position));
		}
	}


	private void OnDrawGizmos()
	{
		Player = Target;
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
			Gizmos.DrawLine(Player.position, CoverPos.CoverPosition.position);

			//Debug.Log($" {CoverPos.name} {CoverPos}");
			foreach (Node node in CoverPos.coverList)
			{

				if (CoverPos.CoverPosition.localScale.x <= 0.4f)
				{
					float playerDot = Vector3.Dot(dir2, dir1);
					if (playerDot < 0)
					{
						dir2 *= -1;
						Debug.Log($" player on the left");
					}
					else
					{
						Debug.Log($" player on the right");
					}

				}


				Vector3 dir3 = (node.LocalCoord - CoverPos.CoverPosition.position).normalized;
				dir3.y = dir2.y;
				float dot = Vector3.Dot(dir2, dir3);
				Gizmos.color = Color.black;
				Gizmos.DrawRay(CoverPos.CoverPosition.position, dir2 * 1.5f);

				Gizmos.DrawLine(node.LocalCoord, CoverPos.CoverPosition.position);

				if (dot > 0 && dot <= 0.01f)
					dot *= -1;
				else if (dot < 0 && dot >= -0.01f)
					dot *= -1;
				if (dot < 0)
				{
					//Debug.Log($" dot{node} {dot} greed");
					Gizmos.color = Color.green;
					Gizmos.DrawLine(node.LocalCoord, node.LocalCoord + Vector3.up * 2);
				}
				else
				{
					//Debug.Log($" dot{node} {dot} red");
					Gizmos.color = Color.red;
					Gizmos.DrawLine(node.LocalCoord, node.LocalCoord + Vector3.up * 2);
				}
			}
		}
	}
}