using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridNameSpace
{
	public enum CoverType
	{
		None,
		Small,
		Destructable,
		Thick,
	}
	class CoverNode
	{
		public CoverTransform CoverTransform;
		public Node node;
		public float Value;
		public float DistanceToPlayer;
		public bool InMovementRange = true;
		private bool _available = false;
		public bool Available
		{
			get => _available;
			set => _available = value;
		}



		public void CalculateDistanceToPlayer(MoveController Player)
		{
			if (Player.curentPositon == null)
			{
				Player.curentPositon = Player.floor.grid.GetNode(Player.transform);
			}
			DistanceToPlayer = Vector3.Distance(node.LocalCoord, Player.curentPositon.LocalCoord);
		}

		public CoverNode(Node node, CoverTransform CoverTransform)
		{
			this.node = node;
			this.CoverTransform = CoverTransform;

		}
	}

	class CoverTransform : Node
	{

		public List<CoverNode> _coverList = new List<CoverNode>();
		public List<CoverNode> CoverList { get { return _coverList; } }
		public string name = "default";
		public int id = 0;
		public Transform CoverPosition;
		public CoverType type;
		public float height;
		public CoverNode BestCoverAvailable { get; set; }

		public CoverTransform(Vector3 localCoord, int x, int y, FloorGrid grid, Transform transform) : base(localCoord, x, y, grid)
		{
			name = transform.name;
			id = transform.GetInstanceID();
			CoverPosition = transform;
			//float volume = transform.localScale.x * transform.localScale.z * transform.localScale.y;
			if (transform.localScale.x < 0.5f || transform.localScale.z < 0.5f)
			{
				type = CoverType.Small;
			}
			else
			{
				type = CoverType.Thick;
			}
			height = transform.transform.localScale.y;
		}


		public void CreatePotentialCover(CoverNode StartNode, List<Node> alreadyDone)
		{

			if (alreadyDone == null)
			{
				alreadyDone = new List<Node>();
			}
			Vector3 offset = Vector3.up * 0.2f;
			if (StartNode.node.isObstacle)
			{

				foreach (Node node in StartNode.node.neighbours)
				{
					if (!alreadyDone.Contains(node) && node.isObstacle == false)
					{
						CoverNode newCover = new CoverNode(node, StartNode.CoverTransform);
						CreatePotentialCover(newCover, alreadyDone);

					}
				}
				return;
			}

			bool potentialcoverExist = false;

			Vector3[] directions = new Vector3[4] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
			foreach (Vector3 dir in directions)
			{
				Ray ray = new Ray(StartNode.node.LocalCoord + offset, dir);
				if (Physics.SphereCast(ray, 0.25f, out RaycastHit hit, 1f, grid.floor.ObstacleLayer))
				{
					if (hit.transform.GetInstanceID() == id)
					{
						//Debug.Log($" {StartNode} is added");
						CoverList.Add(StartNode);
						//UpdateBestCover();
						alreadyDone.Add(StartNode.node);
						potentialcoverExist = true;
					}
				}

			}




			if (potentialcoverExist == false)
			{
				return;
			}


			foreach (Node node in StartNode.node.neighbours)
			{
				if (!alreadyDone.Contains(node))
				{
					CoverNode newCover = new CoverNode(node, StartNode.CoverTransform);
					CreatePotentialCover(newCover, alreadyDone);
				}
			}


		}

		public void UpdateBestCover()
		{
			if (CoverList?.Count == 0)
			{
				BestCoverAvailable = null;
				return;
			}

			CoverNode[] AvailableCovers = (from cover in CoverList
						       where cover != null && cover.Available && cover.InMovementRange
						       select cover).ToArray();
			//Array.Sort(AvailableCovers, new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });

			AvailableCovers = AvailableCovers.OrderByDescending(cover => cover.Value)
							.OrderBy(cover => cover.DistanceToPlayer).ToArray();

			// return 
			BestCoverAvailable = AvailableCovers[0];


		}
	}


	public class Node : AStarNode
	{
		public Vector3 LocalCoord;
		public Vector3 WordPosition;
		public int X;
		public int Y;
		public FloorGrid grid;
		public int nodeCost;
		public bool isObstacle;
		public bool isNodeLink;


		public Node(Vector3 localCoord, int x, int y, FloorGrid grid)
		{
			LocalCoord = localCoord;
			X = x;
			Y = y;
			this.grid = grid;
		}


		public override void Reset()
		{
			base.Reset();
			isObstacle = false;
			isNodeLink = false;

		}
		public override string ToString()
		{
			return $"({X}, {Y})";
		}
	}

	public class AStarNode
	{
		[NonSerialized]
		public List<Node> neighbours;
		[NonSerialized]
		public List<Node> path;
		[NonSerialized]
		public float g = float.PositiveInfinity;
		[NonSerialized]
		public float h = float.PositiveInfinity;
		[NonSerialized]
		public float f = float.PositiveInfinity;
		[NonSerialized]
		public Node parent = null;


		public AStarNode()
		{
			neighbours = new List<Node>();
			path = new List<Node>();

		}


		public virtual void Reset()
		{
			path.Clear();
			g = float.PositiveInfinity;
			h = float.PositiveInfinity;
			f = float.PositiveInfinity;
			parent = null;
		}
	}

	[Serializable]
	public class FloorGrid
	{
		[HideInInspector]
		public int width = 2, height = 2;
		[NonSerialized]
		public int nodeSize = 1;
		[NonSerialized]
		float nodeRadius = 0.5f;
		[NonSerialized]
		public Node[,] nodes = new Node[0, 0];
		[NonSerialized]
		public Vector3 buttonLeft;
		public Floor floor;
		public FloorGrid(int X, int Y, Floor floor, int Nodesize = 1)
		{


			width = Mathf.RoundToInt(X / nodeSize);
			height = Mathf.RoundToInt(Y / nodeSize);
			Debug.Log($"grid {width},{height}");
			nodeSize = Nodesize;
			nodeRadius = nodeSize / 2;
			nodes = new Node[height, width];
			this.floor = floor;
			generateNodes();


		}


		public void GetNodeCoord(out int destinationX, out int destinationY, Camera cam = null)
		{
			if (cam == null) cam = Camera.main;
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			Debug.DrawRay(ray.origin, ray.direction, Color.black);
			if (Physics.Raycast(ray, out RaycastHit hit, floor.floorLayer))
			{

				// todo: we get the position of the mouse toward the grid we create we need
				// to implement the logic we need
				Vector3 worldPosition = ray.GetPoint(hit.distance);
				if (worldPosition.x >= floor.transform.position.x - width / 2 && worldPosition.x <= width / 2 + floor.transform.position.x
					&& worldPosition.z >= floor.transform.position.z - height / 2 && worldPosition.z <= height / 2 + floor.transform.position.z)
				{
					// objectif is to have always 8
					float roundX;
					float roundY;
					int indexY = 0;
					int indexX = 0;


					roundX = Mathf.Floor(worldPosition.x) + (float)nodeSize / 2;
					indexX = (int)(roundX - buttonLeft.x) / nodeSize;

					//Debug.Log($"{worldPosition.x} => floorX = {roundX }, left {buttonLeft.x}  col {(roundX - buttonLeft.x) / nodeSize}");

					roundY = Mathf.Floor(worldPosition.z) + (float)nodeSize / 2;
					indexY = (int)(roundY - buttonLeft.z) / nodeSize;

					//Debug.Log($"{worldPosition.z} => floorY = {roundY},   line {(roundY - buttonLeft.z) / nodeSize}");

					destinationX = indexX;
					destinationY = indexY;
					return;
				}
			}
			destinationX = -1;
			destinationY = -1;
		}
		public Node GetNode(int X, int Y)
		{
			if (nodes == null) return null;

			if (X >= 0 && X < width && Y >= 0 && Y < height)
				return nodes[X, Y];

			return null;
		}

		public Node GetNode(float i, float j)
		{
			if (nodes == null) return null;
			for (int x = 0; x < height; x++)
			{
				for (int y = 0; y < width; y++)
				{
					if (nodes[x, y].LocalCoord.x == i && nodes[x, y].LocalCoord.z == j)
					{
						return nodes[x, y];
					}
				}
			}
			return null;
		}

		public Node GetNode(Transform prefab, Vector3? vect3 = null)
		{
			if (prefab != null)
			{
				Vector3 pos = prefab.position;
				float posX = pos.x;
				float posY = pos.z;

				float percentX = Mathf.Floor(posX) + (float)nodeSize / 2;
				float percentY = Mathf.Floor(posY) + (float)nodeSize / 2;
				return GetNode(percentX, percentY);
			}
			else if (prefab == null && vect3 != null)
			{
				float posX = vect3.Value.x;
				float posY = vect3.Value.z;

				float percentX = Mathf.Floor(posX) + (float)nodeSize / 2;
				float percentY = Mathf.Floor(posY) + (float)nodeSize / 2;

				return GetNode(percentX, percentY);
			}
			return null;
		}
		void generateNodes()
		{
			buttonLeft = floor.transform.position - (Vector3.right * floor.transform.localScale.x) / 2 - (Vector3.forward * floor.transform.localScale.z) / 2;
			buttonLeft += new Vector3((float)nodeSize / 2, floor.transform.localScale.y / 2, (float)nodeSize / 2);
			Debug.DrawLine(buttonLeft, buttonLeft + Vector3.up * 2, Color.yellow);
			//initialize graph
			for (int x = 0; x < height; x++)
			{
				for (int y = 0; y < width; y++)
				{

					Vector3 localCoord = buttonLeft + Vector3.right * nodeSize * x + Vector3.forward * nodeSize * y;
					// create node
					nodes[x, y] = new Node(localCoord, x, y, this);

				}
			}


			for (int x = 0; x < height; x++)
			{
				for (int y = 0; y < width; y++)
				{
					Node currentNode = nodes[x, y];
					//X is not 0, then we can add left (x - 1)
					if (x > 0)
					{
						currentNode.neighbours.Add(nodes[x - 1, y]);
					}
					//X is not mapSizeX - 1, then we can add right (x + 1)
					if (x < height - 1)
					{
						currentNode.neighbours.Add(nodes[x + 1, y]);
					}
					//Y is not 0, then we can add downwards (y - 1 )
					if (y > 0)
					{
						currentNode.neighbours.Add(nodes[x, y - 1]);
					}
					//Y is not mapSizeY -1, then we can add upwards (y + 1)
					if (y < width - 1)
					{
						currentNode.neighbours.Add(nodes[x, y + 1]);
					}
				}
			}



		}



		public void Reset()
		{
			foreach (Node node in nodes)
			{
				node.Reset();
			}
		}

	}

}
