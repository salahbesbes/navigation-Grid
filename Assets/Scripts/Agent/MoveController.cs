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
	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();

	}
	public void StartMoving(Node destination)
	{
		curentPositon = floor.grid.GetNode(transform);
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

		path = FindPath.getPathToDestination(curentPositon, destination);

		if (path.Count == 0)
		{
			Debug.Log($"path.count is 0 we wont move");
			return;
		}
		List<Vector3> optimizedPath = FindPath.createWayPointOriginal(path);
		Debug.Log($"we are at {curentPositon} and start moving toward {destination}");
		//Debug.Log($"{wordSpacePath.Count}");
		StartCoroutine(Move(optimizedPath));
	}

	private void Update()
	{
		curentPositon = floor.grid.GetNode(transform);
		//Debug.Log($"{curentPositon}");
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
			else
			{
				pressedOnDifferentFloor = false;
			}



			if (pressedOnDifferentFloor)
			{
				newFloor.grid.GetNodeCoord(newFloor, out destinationX, out destinationY);

				if (destinationX >= 0 && destinationY >= 0)
				{
					Debug.Log($"dest [x{destinationX}, y{destinationY}]");
					nodeLink = ClosestNodeLinkAvailable();
					destination = nodeLink.node;
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
				Debug.Log($"dest [x{destinationX}, y{destinationY}]");

				floor.grid.GetNodeCoord(floor, out destinationX, out destinationY);
				if (destinationX >= 0 && destinationY >= 0)
				{
					if (floor.grid.GetNode(destinationX, destinationY).isObstacle)
					{
						Debug.Log($"you clicked on obstacle");
						return;
					}
				}



				destination = floor.grid.GetNode(destinationX, destinationY);

				StartMoving(destination);


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
		StopCoroutine("Move");
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
			destination = floor.grid.GetNode(destinationX, destinationY);
			//Debug.Log($"we are on node ({curentPositon}) in the new Floor {floor} and want to  move to newGrid[{destinationX},{destinationY}]  {destination}");
			path.Clear();
			path = FindPath.getPathToDestination(curentPositon, destination);
			List<Vector3> optimizedPath = FindPath.createWayPointOriginal(path);
			StartCoroutine(Move(optimizedPath));
			//Debug.Log($"new current {floor.grid.GetNode(transform)}");
			pressedOnDifferentFloor = false;
		}
	}

	private IEnumerator RunScenario(List<Vector3> path, int Index)
	{
		agent.SetDestination(path[Index]);
		yield return new WaitUntil(() =>
		{

			//Debug.Log($"index {Index} distance ={Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position)} agen radius {agent.radius}  comp is {Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius}");

			return Vector3.Distance(path[Index] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius;
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

	private IEnumerator Move(List<Vector3> path)
	{
		float pauseTime = 0.1f;
		for (int i = 0; i < path.Count; i++)
		{
			yield return StartCoroutine(RunScenario(path, i));
			//agent.SetDestination(wordSpacePath[i]);
			yield return new WaitUntil(() =>
			{
				return Vector3.Distance(path[i] + Vector3.up * agent.baseOffset, agent.transform.position) <= agent.radius;
			});
			yield return new WaitForSeconds(pauseTime);
		}

	}



}
