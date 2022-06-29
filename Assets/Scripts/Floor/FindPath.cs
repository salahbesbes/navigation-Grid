using GridNameSpace;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FindPath
{
	/// <summary>
	/// "using Graph dataType" grid of Nodes is needed to find the closest path between the
	/// start node to the destination node
	/// </summary>
	/// <param name="startNode"> selected Node </param>
	/// <param name="destination"> destination to reach </param>
	// link to visit https://www.youtube.com/watch?v=icZj67PTFhc
	public static List<Node> AStarAlgo(Node startNode, Node destination)
	{
		if (startNode == destination)
		{
			return new List<Node>();
		}
		startNode.g = 0;
		startNode.h = CalcH(startNode, destination);
		startNode.f = startNode.g + startNode.h;
		List<Node> openList = new List<Node>() { startNode };
		List<Node> closedLsit = new List<Node>();
		Node current;
		List<Node> res = new List<Node>();
		while (openList.Count > 0)
		{
			// first sort the list by nodeCost then by the h value (distance to the destination)
			// this give us the shortest path and not expansive 
			openList = openList.OrderBy(item => item.nodeCost).OrderBy(item => item.h).ToList();
			current = openList[0];


			if (current == destination)
			{
				res = getThePath(startNode, current);
				return res;
			}


			openList.Remove(current);
			closedLsit.Add(current);

			if (current.neighbours == null || current.neighbours.Count == 0)
			{
				Debug.Log($"neighbors are 0");
			}

			List<Node> tmp = current.neighbours.Union(current.RemoteNodes).ToList();
			foreach (Node neighbour in tmp)
			{
				if (closedLsit.Contains(neighbour) || neighbour.isObstacle) continue;


				if (current.canReachNeighbor(neighbour) == false)
				{
					continue;
				}

				float tmpG = current.g + CalcG(current, neighbour);
				// if the tmpG is less then the current G on the neighbour node
				if (neighbour.g > tmpG)
				{
					neighbour.g = tmpG;
					neighbour.parent = current;
					neighbour.h = CalcH(neighbour, destination);
					neighbour.f = neighbour.g + neighbour.h + neighbour.nodeCost;
					if (!openList.Contains(neighbour))
						openList.Add(neighbour);
				}
			}


		}

		Debug.Log($"cant find path in the map  current pos {startNode} destination  is {destination}");
		return res;

	}


	public static float CalcG(Node a, Node b)
	{
		return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
	}

	public static float CalcH(Node a, Node e)
	{
		return Mathf.Abs(a.X - e.X) + Mathf.Abs(a.Y - e.Y);
	}

	/// <summary>
	/// get the path from the end node to start node, and make the unit prefab moves toward that
	/// destination folowing that path
	/// </summary>
	/// <param name="unit"> the prefab </param>
	/// <param name="currentUnitNode"> start node </param>
	/// <param name="endUnitNode"> destination </param>
	/// <param name="turnPoints"> class variable sent from Grid to save turn Points </param>
	/// <param name="gridPath"> class variable sent from the Grid to save the Hole path </param>
	/// <returns> </returns>
	public static List<Node> getPathToDestination(Node currentUnitNode, Node endUnitNode)
	{

		return AStarAlgo(currentUnitNode, endUnitNode);
	}

	/// <summary>
	/// create list of nodes of the shortest path between the start and end, start and end node
	/// included. save the path to the class variable GridPath
	/// </summary>
	/// <param name="startNode"> start node </param>
	/// <param name="DestinationNode"> destiation </param>
	public static List<Node> getThePath(Node startNode, Node DestinationNode)
	{
		//Debug.Log($"find path from {startNode} to {DestinationNode}");

		Node Current = DestinationNode;
		// delete previous path
		List<Node> path = new List<Node>();
		startNode.path = new List<Node>();

		int pathCost = 0;
		while (Current.parent != null)
		{
			// fill the path variable
			pathCost += Current.nodeCost;
			path.Add(Current);
			Current = Current.parent;
			//tmp.color = Color.green;
			pathCost++;
			if (pathCost >= 100)
			{
				Debug.Log($" endless loop cant find path ");
				break;
			}
		}
		path.Add(startNode);
		path.Reverse();
		path.ForEach(node => node.Reset());
		startNode.path = path;
		//Debug.Log($"path Cost {pathCost}");
		return path;
	}

	public static List<Node> createWayPointOriginal(List<Node> path)
	{
		Vector2 oldDirection = Vector2.zero;
		List<Node> wayPoints = new List<Node>();

		for (int i = 1; i < path.Count; i++)
		{
			Vector2 prevNodePos = new Vector2(path[i - 1].LocalCoord.x, path[i - 1].LocalCoord.z);
			Vector2 currentNodePos = new Vector2(path[i].LocalCoord.x, path[i].LocalCoord.z);

			Vector2 directionNew = currentNodePos - prevNodePos;
			if (directionNew != oldDirection)
			{
				wayPoints.Add(path[i - 1]);
			}

			oldDirection = directionNew;
		}
		if (path.Count > 0)
		{

			Node lastNodeCoord = path[path.Count - 1];
			wayPoints.Add(lastNodeCoord);
		}

		return wayPoints;
	}




	public static List<Node> getPathToDestination(Vector3[] navmeshPath, Floor floor, Floor alternativeFloor)
	{
		HashSet<Node> path = new HashSet<Node>();

		Floor ActiveFloor = floor;
		//Debug.Log($"get path called  navmesh corners are {navmeshPath.Length}");
		for (int i = 0; i < navmeshPath.Length; i++)
		{

			Node node = ActiveFloor.grid.GetNode(navmeshPath[i]);
			if (node == null)
			{
				//Node safenode = ActiveFloor.grid.GetSafeNode(navmeshPath[i]);

				//GameObject.Instantiate(ActiveFloor.prefab, safenode.LocalCoord, Quaternion.identity);
				//if (safenode.RemoteNodes.Count > 0)
				//{
				//	Node remoteNode = safenode.RemoteNodes[0];
				//	path.Add(remoteNode);
				//	ActiveFloor = alternativeFloor;
				//	i--;
				//}
				//else
				//{
				//	Debug.Log($"remote node is empty  and node isEEdge ");
				//}
				break;
			}
			else
			{
				//node = getClosestNeighbor(node, prevNode);
				path.Add(node);

			}

		}
		List<Node> result = new List<Node>(path);

		if (result.Count > 0)
		{
			path.Clear();
			Node prevNode = result[0];
			for (int i = 1; i < result.Count; i++)
			{
				Node node = result[i];
				//FindPath.getPathToDestination(prevNode, node));
				//Debug.Log($" search the path from {prevNode} to  {node}  {getPathToDestination(prevNode, node).Count}");

				List<Node> tmp = getPathToDestination(prevNode, node);

				path.UnionWith(tmp);

				prevNode = node;

			}
		}
		return path.ToList();
	}

}

public enum PortalDirection
{
	Horizental,
	Vertical,
}
