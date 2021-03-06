using GridNameSpace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

internal interface IBehaviour
{
	public void start();

	public void awake(AgentManager agent);

	public void update();
}

[RequireComponent(typeof(AgentManager))]
public abstract class System_Movement : MonoBehaviour, IBehaviour
{
	[HideInInspector]
	public AgentManager AiAgent;
	public static readonly int sSpeedHash = Animator.StringToHash("Speed");
	public HashSet<RangeNode> NodeInRange { get; set; } = new HashSet<RangeNode>();

	public void updateProperties()
	{
		CurentPositon = ActiveFloor.grid.GetNode(AiAgent.transform);
		if (CurentPositon == null)
		{
			if (Physics.Raycast(AiAgent.transform.position, Vector3.down, out RaycastHit hit, FloorLayer))
			{
				Floor newFloor = hit.transform.GetComponent<Floor>();
				ActiveFloor = newFloor;
				CurentPositon = ActiveFloor.grid.GetNode(AiAgent.transform);

			}
		}
	}

	protected Animator mAnimator;
	protected LineRenderer lr;
	protected List<Node> path = new List<Node>();
	public Floor ActiveFloor = null;
	public Node FinalDestination { get; set; }

	public Node CurentPositon
	{ get { return ActiveFloor.grid.GetNode(AiAgent.transform); } private set { } }

	protected int destinationX;
	protected int destinationY;

	[HideInInspector]
	public NodeLink ActiveNodeLink;

	public Coroutine crossing = null;

	[HideInInspector]
	public bool pressedOnDifferentFloor;

	public LayerMask FloorLayer;

	public void StartMoving(Node destination)
	{
		CurentPositon = ActiveFloor.grid.GetNode(transform);

		if (destination == null)
		{
			//Debug.Log($"cant move,  Destination is null");
			return;
		}
		if (CurentPositon == null)
		{
			Debug.Log($"cant move, CurentPos is null");
			return;


		}
		NavMeshPath navMeshPath = new NavMeshPath();

		AiAgent.agent.CalculatePath(destination.LocalCoord, navMeshPath);

		lr.endColor = Color.red;
		lr.positionCount = navMeshPath.corners.Length;
		lr.SetPositions(navMeshPath.corners);


		path = FindPath.getPathToDestination(navMeshPath.corners, ActiveFloor, destination.grid.floor);
		if (path.Count == 0)
		{
			//Debug.Log($"  path from {curentPositon} to {destination} is 0 we wont move");
			return;


		}
		List<Node> optimizedPath = FindPath.createWayPointOriginal(path);
		lr.endColor = Color.white;

		//lr.positionCount = optimizedPath.Count;
		//lr.SetPositions(optimizedPath.Select(el => el.LocalCoord).ToArray());
		StartCoroutine(Move(optimizedPath));
		return;


	}

	protected List<Node> GetPathFromTo(Node start, Node end)
	{
		List<Node> path = new List<Node>();
		if (end == null)
		{
			//Debug.Log($"cant move,  Destination is null");
			return path;
		}
		if (start == null)
		{
			Debug.Log($"cant move, CurentPos is null");
			return path;


		}
		NavMeshPath navMeshPath = new NavMeshPath();

		//AiAgent.agent.CalculatePath(end.LocalCoord, navMeshPath);
		NavMesh.CalculatePath(start.LocalCoord, end.LocalCoord, 1, navMeshPath);
		lr.positionCount = 0;
		lr.endColor = Color.red;
		lr.positionCount = navMeshPath.corners.Length;
		lr.SetPositions(navMeshPath.corners);


		path = FindPath.getPathToDestination(navMeshPath.corners, start.grid.floor, end.grid.floor);
		if (path.Count == 0)
		{
			Debug.Log($"  path from {start} to {end} is 0 we wont move");
			return path;


		}
		List<Node> optimizedPath = FindPath.createWayPointOriginal(path);
		return optimizedPath;
	}

	public void StartCrossing(Vector3 destination)
	{
		AiAgent.agent.speed = 2;
		mAnimator.SetFloat(sSpeedHash, AiAgent.agent.speed);
		crossing = StartCoroutine(Cross(destination));
	}

	protected Floor GetFloorPressed(Camera cam = null)
	{
		if (cam == null) cam = Camera.main;
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		RaycastHit[] hits = Physics.SphereCastAll(ray.origin, 0.25f, ray.direction);
		foreach (RaycastHit hit in hits)
		{
			Floor newFloor = hit.transform.GetComponent<Floor>();
			if (newFloor != null)
			{
				return newFloor;
			}
		}

		return ActiveFloor;
	}

	public void ListenToNodeLinkEvent(NodeLink node)
	{
		node.AddObservable(this);
		node.Destiation.AddObservable(this);
	}

	public NodeLink ClosestNodeLinkAvailable(Floor newFloor)
	{
		float minDistance = int.MaxValue;
		NodeLink closestNode = null;
		float currentDistance;

		// get all nodeLinks that have the Destination.floor is the newFloor
		List<NodeLink> NodeLinksThatLeadToNewFloor = ActiveFloor.nodeLinks.Where(node => node.Destiation.floor == newFloor).ToList();

		foreach (NodeLink node in NodeLinksThatLeadToNewFloor)
		{
			currentDistance = Vector3.Distance(node.transform.position, transform.position);

			if (currentDistance < minDistance)
			{
				minDistance = currentDistance;
				closestNode = node;
			}
		}
		ActiveNodeLink = closestNode;
		return closestNode;
	}

	public void CrossingToNodeLinkDestination(NodeLink currentNodelink, AgentManager unit)
	{
		//Debug.Log($"final Destination is {FinalDestination} on the {FinalDestination.grid.floor} active floor is {ActiveFloor}");
		if (unit != AiAgent) return;
		StopCoroutine("Move");
		StopCoroutine("Cross");
		crossing = null;

		// we want to Move to nodeLink.Destination (cross)
		//crossing = StartCoroutine(Cross(currentNodelink.Destiation.node.LocalCoord));
		StartCrossing(currentNodelink.Destiation.node.LocalCoord);
		ActiveNodeLink = currentNodelink.Destiation;
	}

	public void WhenReachNodeLinkDestination(NodeLink currentNodelink, AgentManager unit)
	{
		if (unit != AiAgent) return;
		StopCoroutine("Cross"); crossing = null;
		StopCoroutine("Move");

		AiAgent.agent.speed = 4f;

		CurentPositon = currentNodelink.node;
		ActiveFloor = currentNodelink.node.grid.floor;

		//Debug.Log($"when Finished Crossing ActiveFloor is {ActiveFloor} and destination is {FinalDestination}  current pos is {curentPositon}");
		StartMoving(FinalDestination);
		pressedOnDifferentFloor = false;
	}

	private IEnumerator RunScenario(List<Node> path, int Index)
	{
		AiAgent.agent.SetDestination(path[Index].LocalCoord);
		//mAnimator.SetFloat(sSpeedHash, AiAgent.agent.speed);
		yield return new WaitUntil(() =>
		{
			//Debug.Log($"index {Index} distance ={Vector3.Distance(wordSpacePath[Index] + Vector3.up * AiAgent.agent.baseOffset, AiAgent.agent.transform.position)} agen radius {AiAgent.agent.radius}  comp is {Vector3.Distance(wordSpacePath[Index] + Vector3.up * AiAgent.agent.baseOffset, AiAgent.agent.transform.position) <= AiAgent.agent.radius}");
			return path[Index] == CurentPositon;
			//return Vector3.Distance(path[Index] + Vector3.up * AiAgent.agent.baseOffset, AiAgent.agent.transform.position) <= AiAgent.agent.radius;
		});

		yield return new WaitForSeconds(0.1f);
	}

	private IEnumerator Cross(Vector3 dest)
	{
		//yield return new WaitForSeconds(0.5f);

		AiAgent.agent.SetDestination(dest);
		yield return new WaitUntil(() =>
		{
			return Vector3.Distance(dest + Vector3.up * AiAgent.agent.baseOffset, AiAgent.agent.transform.position) < AiAgent.agent.radius;
		});
		//Debug.Log($"cross ");
	}

	protected IEnumerator Move(List<Node> path)
	{
		float pauseTime = 0.1f;
		mAnimator.SetFloat(sSpeedHash, AiAgent.agent.speed);

		for (int i = 0; i < path.Count; i++)
		{
			yield return StartCoroutine(RunScenario(path, i));
			yield return new WaitUntil(() =>
			{
				return CurentPositon == path[i];
				//return Vector3.Distance(path[i] + Vector3.up * AiAgent.agent.baseOffset, AiAgent.agent.transform.position) <= AiAgent.agent.radius;
			});

			yield return new WaitForSeconds(pauseTime);
		}
		mAnimator.SetFloat(sSpeedHash, 0);
	}

	private void OnDisable()
	{
		foreach (var nodeLink in ActiveFloor.nodeLinks)
		{
			nodeLink.RemoveUnitObservable(this);
		}
	}

	protected HashSet<RangeNode> GetRangeOfMevement(Node currenPosition, int MaxRange, int minRange = 0, int currentLineOfRange = 0,
		HashSet<RangeNode> nodesInRange = null, List<Node> nextLineRange = null, List<Node> alreadyChecked = null)
	{
		if (currenPosition == null) currenPosition = ActiveFloor.grid.GetNode(AiAgent.transform);
		if (alreadyChecked == null) alreadyChecked = new List<Node>();
		if (nextLineRange == null)
			nextLineRange = new List<Node>() { currenPosition };
		if (nodesInRange == null)
			nodesInRange = new HashSet<RangeNode>() { new RangeNode(currenPosition, true) };
		if (currentLineOfRange > MaxRange) return nodesInRange;

		List<Node> newLineRange = new List<Node>();

		foreach (Node node in nextLineRange)
		{
			if (alreadyChecked.Contains(node)) continue;
			alreadyChecked.Add(node);
			List<Node> tmp = node.neighbours.Union(node.RemoteNodes).ToList();
			foreach (Node neighbor in tmp)
			{
				if (alreadyChecked.Contains(neighbor)) continue;
				newLineRange.Add(neighbor);

				if (currentLineOfRange >= minRange && !neighbor.isObstacle)
				{
					if (currentLineOfRange > (float)(MaxRange - minRange) / 2 + minRange)
					{
						nodesInRange.Add(new RangeNode(neighbor, false));
					}
					else
					{
						nodesInRange.Add(new RangeNode(neighbor, true));
					}
				}
				//else
				//{
				//	nodesInRange.Add(new RangeNode(neighbor, true));

				//}
			}
		}
		return GetRangeOfMevement(currenPosition, MaxRange, minRange, currentLineOfRange + 1, nodesInRange, newLineRange, alreadyChecked);
	}
	private void OnDestroy()
	{
		foreach (var nodeLink in ActiveFloor.nodeLinks)
		{
			nodeLink.RemoveUnitObservable(this);
		}
	}

	public abstract void AgentInputSystem();

	public virtual void start()
	{
		throw new System.NotImplementedException();
	}

	public virtual void awake(AgentManager agent)
	{
		throw new System.NotImplementedException();
	}

	public virtual void update()
	{
		throw new System.NotImplementedException();
	}
}