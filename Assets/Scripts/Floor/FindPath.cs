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
			foreach (Node neighbour in current.neighbours)
			{
				if (closedLsit.Contains(neighbour) || neighbour.isObstacle) continue;

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

		Debug.Log($"cant find path in the map  current pos {startNode} des  is {destination}");
		return res;

	}


	/*
	 startNode.g = 0;
		Queue<Node> QueueNotTested = new Queue<Node>();
		List<Node> result = new List<Node>();
		bool success = false;

		QueueNotTested.Enqueue(startNode);

		while (QueueNotTested.Count >= 0)
		{
			// sort the list in acending order by the heuretic value
			QueueNotTested = new Queue<Node>(QueueNotTested.OrderBy(item => item.h).OrderBy(item => item.nodeCost));

			//var queue = new Queue<string>(myStringList);


			//we can loop throw an empty list in that case we are sure we didnt
			//find any path
			if (QueueNotTested.Count == 0)
			{
				Debug.Log($"cant find path ");
				success = false;
				break;
			}

			// select the first node of the list which have the less value of the
			// heuritic value
			//Node current = QueueNotTested[0];
			//if (queueOfSameHValue.Count > 0) Debug.Log($"{cur},  {current}");
			//remove it from the rhe list
			Node current = QueueNotTested.Dequeue();
			// make it visited
			current.visited = true;

			// if the current == end we find the en point
			if (current == destination)
			{
				//Debug.Log($"Found End :) ");
				result = getThePath(startNode, current);
				success = true;
				break;
			}
			//printGrid(current);

			if (current.isObstacle == true) continue;

			for (int i = 0; i < current.neighbours.Count; i++)
			{
				// foreach neighbor which is not an obstacle
				Node neighbor = current.neighbours[i];
				if (neighbor.isObstacle == true) continue;
				else
				{
					// calculate the g val (toWard the parent)
					float neighborG = CalcG(current, neighbor);
					// calculate the new possible g value (toWard the start
					// node)
					float tempG = current.g + neighborG;
					// by default the neighbor.g is positif Infinit but after
					// setting g val to a neighbor we can revisit this node and
					// at this time the g val is not infinit so we wan do the
					// comparison

					if (tempG < neighbor.g)
					{
						neighbor.parent = current;
						neighbor.g = tempG;
						neighbor.h = neighbor.g + CalcH(neighbor, destination);
						// update the list we are worrking on
						QueueNotTested.Enqueue(neighbor);
					}
				}
			}
		}

		// we pass the result var to the get methode and we set the gridPath any way (empty
		// or full)
		return result;
	 
	 
	 
	 */
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
		//Debug.Log($"start {currentUnitNode} end {endUnitNode}");
		return AStarAlgo(currentUnitNode, endUnitNode);
	}

	/// <summary>
	/// create list of nodes of the shortest path between the start and end, start and end node
	/// included. save the path to the class variable GridPath
	/// </summary>
	/// <param name="startNode"> start node </param>
	/// <param name="current"> destiation </param>
	public static List<Node> getThePath(Node startNode, Node current)
	{
		Node tmp = current;
		// delete previous path
		List<Node> path = new List<Node>();
		startNode.path = new List<Node>();

		int pathCost = 0;
		while (tmp.parent != null)
		{
			// fill the path variable
			pathCost += tmp.nodeCost;
			path.Add(tmp);
			tmp = tmp.parent;
			//tmp.color = Color.green;
		}
		path.Add(startNode);
		path.Reverse();

		startNode.path = path;
		//Debug.Log($"path Cost {pathCost}");
		return path;
	}

	///// <summary> return an array of position where the unit change position </summary>
	///// <param name="path"> path between start and end nodes </param>
	///// <returns> array of position where the unit change direction </returns>
	//public static Vector3[] createWayPoint(List<Node> path)
	//{

	//	List<Vector3> pathPoint = new List<Vector3>();
	//	for (int i = 0; i < path.Count; i++)
	//	{
	//		Node currentNode = path[i];

	//		Vector3 point = currentNode.coord;

	//		if (currentNode.tile.obj != null)
	//		{
	//			Node prevNode = path[i - 1];
	//			pathPoint.Add(prevNode.coord);
	//			Vector3 prevUP = new Vector3(prevNode.coord.x, 1, prevNode.coord.z);
	//			pathPoint.Add(prevUP);
	//		}

	//		pathPoint.Add(point);
	//		// todo: create a reference on the object sits ontop of the tile so that i know how tall he is

	//	}

	//	return pathPoint.ToArray();


	//}
	public static List<Vector3> createWayPointOriginal(List<Node> path)
	{
		Vector2 oldDirection = Vector2.zero;
		List<Vector3> wayPoints = new List<Vector3>();

		for (int i = 1; i < path.Count; i++)
		{
			Vector2 prevNodePos = new Vector2(path[i - 1].LocalCoord.x, path[i - 1].LocalCoord.z);
			Vector2 currentNodePos = new Vector2(path[i].LocalCoord.x, path[i].LocalCoord.z);

			Vector2 directionNew = currentNodePos - prevNodePos;
			if (directionNew != oldDirection)
			{
				wayPoints.Add(path[i - 1].LocalCoord);
			}

			oldDirection = directionNew;
		}
		if (path.Count > 0)
		{

			Vector3 lastNodeCoord = path[path.Count - 1].LocalCoord;
			if (wayPoints.Contains(lastNodeCoord) == false)
			{
				wayPoints.Add(lastNodeCoord);
			}
			// remove the first position where the player is sitting
			wayPoints.RemoveAt(0);
		}

		return wayPoints;
	}
}


//public class Node
//{
//	public Color color;
//	public Vector3 coord;
//	public bool firstRange = false;
//	public float g = float.PositiveInfinity;
//	public float h = float.PositiveInfinity;
//	public float f = float.PositiveInfinity;
//	public int nodeCost = 0;
//	public bool inRange = false;
//	public bool isObstacle = false;
//	public List<Node> neighbours;
//	public Node parent = null;
//	public List<Node> path;
//	public bool visited = false;
//	public int x;
//	public int y;

//	public GameObject quad;

//	public Node(Vector3 coord, int x, int y)
//	{
//		this.coord = coord;
//		this.x = x;
//		this.y = y;
//		neighbours = new List<Node>();
//		path = new List<Node>();
//	}

//	public override string ToString()
//	{
//		return $" node ({x}, {y}) ";
//	}
