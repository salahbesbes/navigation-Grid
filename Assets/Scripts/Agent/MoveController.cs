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
	void Start()
	{
		agent = GetComponent<NavMeshAgent>();

		// Disabling auto-braking allows for continuous movement
		// between points (ie, the agent doesn't slow down as it
		// approaches a destination point).

	}


	private void Update()
	{
		curentPositon = floor.grid.GetNode(transform);
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Debug.DrawRay(ray.origin, ray.direction, Color.black);
		if (Input.GetMouseButtonDown(0))
		{

			List<int> res = floor.grid.GetNodeCoord(out destinationX, out destinationY);

			Debug.Log($"{wordSpacePath.Count}");
			if (res != null)
			{
				destination = floor.grid.GetNode(res[0], res[1]);

				if (destination != null)
				{

					path.Clear();
					path = FindPath.getPathToDestination(curentPositon, destination);
					wordSpacePath = FindPath.createWayPointOriginal(path);
					StartCoroutine(Move());
				}


			}









		}
	}


	private Coroutine crossing;

	private IEnumerator OnTriggerEnter(Collider other)
	{
		NodeLink nodelink = other.transform.GetComponent<NodeLink>();

		if (nodelink == null) yield break;
		StopCoroutine(Move());
		// how to know if we want to move to nodeLink Destination or
		// we just came from some other floor and want to move to real Destination

		Link link = other.transform.GetComponentInParent<Link>();
		//Debug.Log($"we hit {other.name} with the link {link.name}");
		if (nodelink.node.grid == floor.grid && crossing == null)
		{
			// we want to Move to nodeLink.Destination (cross)
			Debug.Log($"we are crossing the bridge to {nodelink.Destiation.node.LocalCoord} ");
			yield return crossing = StartCoroutine(Cross(nodelink.Destiation.node.LocalCoord));
			//agent.SetDestination(nodelink.Destiation.node.LocalCoord);
		}
		else
		{

			// player changed floor need update this class properties
			crossing = null;
			floor = nodelink.node.grid.floor;
			nodelink = nodelink.Destiation;
			curentPositon = nodelink.Destiation.node;
			Debug.Log($"we are on node {curentPositon} in the new Floor {floor} and want to  move to newGrid[0,0] (manual set) ");

			destination = floor.grid.nodes[0, 0];
			path.Clear();
			path = FindPath.getPathToDestination(curentPositon, destination);
			wordSpacePath = FindPath.createWayPointOriginal(path);
			StartCoroutine(Move());
		}
	}

	private IEnumerator RunScenario(int Index)
	{
		Vector3 startPosition = agent.transform.position;

		agent.SetDestination(wordSpacePath[Index]);

		yield return new WaitUntil(() =>
		{
			return Vector3.Distance(wordSpacePath[Index] + Vector3.up * agent.baseOffset, agent.transform.position) < agent.radius;
		});
		yield return new WaitForSeconds(0.25f);


	}

	private IEnumerator Cross(Vector3 dest)
	{
		yield return new WaitForSeconds(0.25f);
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
			yield return new WaitUntil(() =>
			{
				return Vector3.Distance(wordSpacePath[i] + Vector3.up * agent.baseOffset, agent.transform.position) < agent.radius;
			});
			yield return new WaitForSeconds(pauseTime);
		}
	}



}
