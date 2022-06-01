using GridNameSpace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MoveController : MonoBehaviour
{
	NavMeshAgent agent;

	List<Node> path = new List<Node>();
	List<Vector3> wordSpacePath = new List<Vector3>();
	public Floor floor = null;
	Node destination;
	public Node curentPositon;
	private int destinationX;
	private int destinationY;
	NodeLink nodeLink;
	public Transform Enemy;
	void Start()
	{
		agent = GetComponent<NavMeshAgent>();

		// Disabling auto-braking allows for continuous movement
		// between points (ie, the agent doesn't slow down as it
		// approaches a destination point).

	}
	public void StartMoving(Node destination)
	{
		curentPositon = floor.grid.GetNode(transform);
		path = FindPath.getPathToDestination(curentPositon, destination);
		if (path.Count == 0)
		{
			Debug.Log($"path.count is 0 we wont move");
			return;
		}
		wordSpacePath = FindPath.createWayPointOriginal(path);
		//Debug.Log($"{wordSpacePath.Count}");
		StartCoroutine(Move());
	}

	private void Update()
	{
		curentPositon = floor.grid.GetNode(transform);
		if (agent.name == "player")
		{
			transform.LookAt(Enemy);
		}

		if (Input.GetMouseButtonDown(0) && agent.name == "player")
		{



			Floor newFloor = GetFloorPressed();
			if (newFloor != floor)
			{
				pressedOnDifferentFloor = true;
			}



			if (pressedOnDifferentFloor)
			{
				newFloor.grid.GetNodeCoord(out destinationX, out destinationY);
				if (destinationX >= 0 && destinationY >= 0)
				{
					if (newFloor.grid.GetNode(destinationX, destinationY).isObstacle)
					{
						Debug.Log($"you clicked on obstacle");
						return;
					}
				}

				nodeLink = ClosestNodeLinkAvailable();
				destination = nodeLink.node;
				if (destination != null)
				{
					StartMoving(destination);

				}

			}
			else
			{
				floor.grid.GetNodeCoord(out destinationX, out destinationY);
				if (destinationX >= 0 && destinationY >= 0)
				{
					if (floor.grid.GetNode(destinationX, destinationY).isObstacle)
					{
						Debug.Log($"you clicked on obstacle");
						return;
					}
				}



				destination = floor.grid.GetNode(destinationX, destinationY);

				if (destination != null)
				{
					StartMoving(destination);
				}


			}










		}
	}

	private Floor GetFloorPressed(Camera cam = null)
	{
		if (cam == null) cam = Camera.main;
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		Debug.DrawRay(ray.origin, ray.direction, Color.black);
		if (Physics.Raycast(ray, out RaycastHit hit, floor.floorLayer))
		{
			Floor newFloor = hit.transform.GetComponent<Floor>();
			if (newFloor != null)
			{
				return newFloor;
			}
		}
		return floor;
	}

	public NodeLink ClosestNodeLinkAvailable()
	{
		float minDistance = int.MinValue;
		NodeLink closestNode = null;
		float currentDistance;
		foreach (NodeLink node in floor.nodeLinks)
		{
			currentDistance = Vector3.Distance(node.transform.position, transform.position);
			if (currentDistance > minDistance)
			{
				minDistance = currentDistance;
				closestNode = node;
			}
		}
		nodeLink = closestNode;
		return closestNode;
	}

	private Coroutine crossing = null;
	public bool pressedOnDifferentFloor;

	private void OnTriggerEnter(Collider other)
	{
		NodeLink nodelink = other.transform.GetComponent<NodeLink>();

		if (nodelink == null || pressedOnDifferentFloor == false) return;
		StopCoroutine(Move());
		// how to know if we want to move to nodeLink Destination or
		// we just came from some other floor and want to move to real Destination
		Link link = other.transform.GetComponentInParent<Link>();
		//Debug.Log($"we hit {other.name} with the link {link.name}");
		if (nodelink.node.grid == floor.grid && crossing == null)
		{
			// we want to Move to nodeLink.Destination (cross)
			//Debug.Log($"we are crossing the bridge to {nodelink.Destiation.node.LocalCoord} ");
			crossing = StartCoroutine(Cross(nodelink.Destiation.node.LocalCoord));
			//agent.SetDestination(nodelink.Destiation.node.LocalCoord);

		}
		else if (nodelink.node.grid != floor.grid && crossing != null)
		{

			// player changed floor need update this class properties
			StopCoroutine("Cross");
			crossing = null;
			floor = nodelink.node.grid.floor;
			nodelink = nodelink.Destiation;
			curentPositon = nodelink.Destiation.node;
			//Debug.Log($"we are on node {curentPositon} in the new Floor {floor} and want to  move to newGrid[{destinationX},{destinationY}] (manual set) ");

			destination = floor.grid.nodes[destinationX, destinationY];
			path.Clear();
			path = FindPath.getPathToDestination(curentPositon, destination);
			wordSpacePath = FindPath.createWayPointOriginal(path);
			StartCoroutine(Move());
			pressedOnDifferentFloor = false;
		}
	}

	private IEnumerator RunScenario(int Index)
	{
		agent.SetDestination(wordSpacePath[Index]);
		yield return new WaitUntil(() =>
		{

			//Debug.Log($"index {Index} distance ={Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position)} agen radius {agent.radius}  comp is {Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius}");

			return Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius;
		});
		yield return new WaitForSeconds(0.1f);


	}

	private IEnumerator Cross(Vector3 dest)
	{
		yield return new WaitForSeconds(0.2f);
		agent.SetDestination(dest);
		yield return new WaitUntil(() =>
		{
			return Vector3.Distance(dest + Vector3.up * agent.baseOffset, agent.transform.position) < agent.radius;
		});
	}

	private IEnumerator Move()
	{
		float pauseTime = 0.1f;
		for (int i = 0; i < wordSpacePath.Count; i++)
		{
			yield return StartCoroutine(RunScenario(i));
			//agent.SetDestination(wordSpacePath[i]);
			yield return new WaitUntil(() =>
			{
				return Vector3.Distance(wordSpacePath[i] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius;
			});
			yield return new WaitForSeconds(pauseTime);
		}

	}



}
