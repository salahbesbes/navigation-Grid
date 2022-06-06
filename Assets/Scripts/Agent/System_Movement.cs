using GridNameSpace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AgentManager))]
public abstract class System_Movement : MonoBehaviour
{
	[HideInInspector]
	public AgentManager AiAgent;
	public static readonly int sSpeedHash = Animator.StringToHash("Speed");



	public void updateProperties()
	{
		curentPositon = ActiveFloor.grid.GetNode(AiAgent.transform);

	}

	protected Animator mAnimator;
	protected LineRenderer lr;
	protected List<Node> path = new List<Node>();
	public Floor ActiveFloor = null;
	public Node FinalDestination;
	public Node curentPositon;
	protected int destinationX;
	protected int destinationY;
	[HideInInspector]
	public NodeLink ActiveNodeLink;

	public Coroutine crossing = null;
	[HideInInspector]
	public bool pressedOnDifferentFloor;

	public LayerMask FloorLayer;
	public abstract void awake(AgentManager agent);

	public abstract void start();


	public async void StartMoving(Node destination)
	{
		curentPositon = ActiveFloor.grid.GetNode(transform);

		if (destination == null)
		{
			Debug.Log($"cant move,  Destination is null");
			return;
		}
		if (curentPositon == null)
		{
			Debug.Log($"cant move, CurentPos is null");
			return;
		}
		NavMeshPath navMeshPath = new NavMeshPath();

		AiAgent.agent.CalculatePath(destination.LocalCoord, navMeshPath);
		lr.positionCount = navMeshPath.corners.Length;
		lr.SetPositions(navMeshPath.corners);

		path = await FindPath.getPathToDestination(navMeshPath.corners, ActiveFloor);

		if (path.Count == 0)
		{
			//Debug.Log($"  path from {curentPositon} to {destination} is 0 we wont move");
			return;
		}
		List<Node> optimizedPath = FindPath.createWayPointOriginal(path);
		StartCoroutine(Move(optimizedPath));
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

		AiAgent.agent.speed = 8;



		curentPositon = currentNodelink.node;
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
			return path[Index] == curentPositon;
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

	private IEnumerator Move(List<Node> path)
	{
		float pauseTime = 0.1f;
		mAnimator.SetFloat(sSpeedHash, AiAgent.agent.speed);

		for (int i = 0; i < path.Count; i++)
		{
			yield return StartCoroutine(RunScenario(path, i));
			yield return new WaitUntil(() =>
			{
				return curentPositon == path[i];
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

	private void OnDestroy()
	{
		foreach (var nodeLink in ActiveFloor.nodeLinks)
		{
			nodeLink.RemoveUnitObservable(this);
		}
	}

	public abstract void AgentInputSystem();
}
