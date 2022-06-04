using GridNameSpace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MoveController : MonoBehaviour
{
	NavMeshAgent agent;
	private static readonly int sSpeedHash = Animator.StringToHash("Speed");
	private Animator mAnimator;
	LineRenderer lr;
	List<Node> path = new List<Node>();
	public Floor ActiveFloor = null;
	public Node FinalDestination;
	public Node curentPositon;
	private int destinationX;
	private int destinationY;
	public NodeLink ActiveNodeLink;
	public Transform Enemy;

	public Coroutine crossing = null;
	public bool pressedOnDifferentFloor;
	private void Awake()
	{
		mAnimator = GetComponent<Animator>();
		agent = GetComponent<NavMeshAgent>();
		lr = GetComponent<LineRenderer>();

	}
	public void StartMoving(Node destination)
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
		agent.CalculatePath(destination.LocalCoord, navMeshPath);

		path = FindPath.getPathToDestination(navMeshPath.corners, ActiveFloor);

		if (path.Count == 0)
		{
			//Debug.Log($"  path from {curentPositon} to {destination} is 0 we wont move");
			return;
		}
		List<Vector3> optimizedPath = FindPath.createWayPointOriginal(path);

		StartCoroutine(Move(optimizedPath));
	}

	private void Update()
	{
		curentPositon = ActiveFloor.grid.GetNode(transform);
		if (agent.name == "player")
		{
			transform.LookAt(Enemy);
		}

		if (Input.GetMouseButtonDown(0) && agent.name == "player")
		{

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit Hit))
			{
				NavMeshPath path = new NavMeshPath();
				agent.CalculatePath(Hit.point, path);
				lr.positionCount = path.corners.Length;

				lr.SetPositions(path.corners);

			}


			Floor newFloor = GetFloorPressed();
			if (newFloor != ActiveFloor)
			{
				pressedOnDifferentFloor = true;
			}
			else
			{
				pressedOnDifferentFloor = false;
			}



			if (pressedOnDifferentFloor)
			{
				newFloor.grid.GetNodeCoord(newFloor, out destinationX, out destinationY);
				FinalDestination = newFloor.grid.GetNode(destinationX, destinationY);
				if (destinationX >= 0 && destinationY >= 0)
				{
					Debug.Log($"dest [x{destinationX}, y{destinationY}]");
					ActiveNodeLink = ClosestNodeLinkAvailable(newFloor);
					Node destination = ActiveNodeLink.node;
					StartMoving(destination);
					//Node finalDest = newFloor.grid.GetNode(destinationX, destinationY);
					//if (finalDest == null)
					//{
					//	Debug.Log($"final des is (null)");
					//	return;

					//}
					//else if (finalDest.isObstacle)
					//{
					//	Debug.Log($"you clicked on obstacle");
					//	return;
					//}
					//else
					//{

					//}
				}
				else
				{
					Debug.Log($"dest [x{destinationX}, y{destinationY}]");
				}



			}
			else
			{
				//Debug.Log($"dest [x{destinationX}, y{destinationY}]");

				ActiveFloor.grid.GetNodeCoord(ActiveFloor, out destinationX, out destinationY);
				if (destinationX >= 0 && destinationY >= 0)
				{
					if (ActiveFloor.grid.GetNode(destinationX, destinationY).isObstacle)
					{
						Debug.Log($"you clicked on obstacle");
						return;
					}
				}



				FinalDestination = ActiveFloor.grid.GetNode(destinationX, destinationY);

				StartMoving(FinalDestination);


			}

		}
	}

	private Floor GetFloorPressed(Camera cam = null)
	{
		if (cam == null) cam = Camera.main;
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		Debug.DrawRay(ray.origin, ray.direction, Color.black);
		if (Physics.Raycast(ray, out RaycastHit hit, ActiveFloor.floorLayer))
		{
			Floor newFloor = hit.transform.GetComponent<Floor>();
			if (newFloor != null)
			{
				return newFloor;
			}
		}
		return ActiveFloor;
	}

	public NodeLink ClosestNodeLinkAvailable(Floor newFloor)
	{
		float minDistance = int.MinValue;
		NodeLink closestNode = null;
		float currentDistance;

		// Listen To every Availabe nodeLink on the floor

		foreach (var nodeLink in ActiveFloor.nodeLinks)
		{
			nodeLink.AddObservable(this);
			nodeLink.Destiation.AddObservable(this);
		}
		// get all nodeLinks that have the Destination.floor is the newFloor
		List<NodeLink> NodeLinksThatLeadToNewFloor = ActiveFloor.nodeLinks.Where(node => node.Destiation.floor == newFloor).ToList();


		foreach (NodeLink node in NodeLinksThatLeadToNewFloor)
		{
			currentDistance = Vector3.Distance(node.transform.position, transform.position);
			if (currentDistance > minDistance)
			{
				minDistance = currentDistance;
				closestNode = node;
			}
		}
		ActiveNodeLink = closestNode;
		return closestNode;
	}





	public void CrossingToNodeLinkDestination(NodeLink currentNodelink)
	{
		//Debug.Log($"final Destination is {FinalDestination} on the {FinalDestination.grid.floor} active floor is {ActiveFloor}");
		StopCoroutine("Move");
		StopCoroutine("Cross");
		crossing = null;

		// we want to Move to nodeLink.Destination (cross)
		crossing = StartCoroutine(Cross(currentNodelink.Destiation.node.LocalCoord));
		ActiveNodeLink = currentNodelink.Destiation;
	}

	public void WhenReachNodeLinkDestination(NodeLink currentNodelink)
	{
		StopCoroutine("Cross"); crossing = null;
		StopCoroutine("Move");

		agent.speed = 8;



		curentPositon = currentNodelink.node;
		ActiveFloor = currentNodelink.node.grid.floor;

		//Debug.Log($"when Finished Crossing ActiveFloor is {ActiveFloor} and destination is {FinalDestination}  current pos is {curentPositon}");
		StartMoving(FinalDestination);
		pressedOnDifferentFloor = false;
	}



	private IEnumerator RunScenario(List<Vector3> path, int Index)
	{
		agent.SetDestination(path[Index]);
		//mAnimator.SetFloat(sSpeedHash, agent.speed);
		yield return new WaitUntil(() =>
		{

			//Debug.Log($"index {Index} distance ={Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position)} agen radius {agent.radius}  comp is {Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius}");

			return Vector3.Distance(path[Index] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius;
		});

		yield return new WaitForSeconds(0.1f);


	}

	private IEnumerator Cross(Vector3 dest)
	{
		agent.speed = 2;
		yield return new WaitForSeconds(0.5f);
		mAnimator.SetFloat(sSpeedHash, agent.speed);
		agent.SetDestination(dest);
		yield return new WaitUntil(() =>
		{
			return Vector3.Distance(dest + Vector3.up * agent.baseOffset, agent.transform.position) < agent.radius;
		});
		Debug.Log($"cross ");

	}

	private IEnumerator Move(List<Vector3> path)
	{
		float pauseTime = 0.1f;
		mAnimator.SetFloat(sSpeedHash, agent.speed);

		for (int i = 0; i < path.Count; i++)
		{
			yield return StartCoroutine(RunScenario(path, i));
			yield return new WaitUntil(() =>
			{
				return Vector3.Distance(path[i] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius;
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

}
