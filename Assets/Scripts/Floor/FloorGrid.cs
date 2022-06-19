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
	public class CoverNode
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



		public void CalculateDistanceToPlayer(Transform Player)
		{
			DistanceToPlayer = Vector3.Distance(node.LocalCoord, Player.position);
		}

		public CoverNode(Node node, CoverTransform CoverTransform)
		{
			this.node = node;
			this.CoverTransform = CoverTransform;

		}
	}

	public class CoverTransform
	{

		public List<CoverNode> _coverList = new List<CoverNode>();
		public List<CoverNode> CoverList { get { return _coverList; } }
		public string name = "default";
		public int id = 0;
		public Transform CoverPosition;
		public CoverType type;
		public float height;
		public CoverNode BestCoverAvailable { get; set; }
		public FloorGrid grid;
		public CoverTransform(FloorGrid grid, Transform transform)
		{
			this.grid = grid;
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

		public void CreatePotentialCover()
		{
			float width, depth;

			if (CoverPosition.rotation.eulerAngles.y % 180 == 0)
			{
				width = CoverPosition.localScale.x;
				depth = CoverPosition.localScale.z;
			}
			else
			{
				width = CoverPosition.localScale.z;
				depth = CoverPosition.localScale.x;
			}
			Vector3 bottomLeftPoint = new Vector3(CoverPosition.position.x - width / 2, CoverPosition.position.y, CoverPosition.position.z - depth / 2);
			Vector3 TopLeftPoint = new Vector3(CoverPosition.position.x - width / 2, CoverPosition.position.y, CoverPosition.position.z + depth / 2);

			Vector3 TopRightPoint = new Vector3(TopLeftPoint.x + width, TopLeftPoint.y, TopLeftPoint.z);
			Vector3 bottomRightPoint = new Vector3(bottomLeftPoint.x + width, bottomLeftPoint.y, bottomLeftPoint.z);

			Debug.DrawLine(bottomLeftPoint, bottomLeftPoint + Vector3.up);
			Debug.DrawLine(TopLeftPoint, bottomLeftPoint + Vector3.up);


			float tmp = TopLeftPoint.z - bottomLeftPoint.z;


			while (tmp > 1)
			{

				float Zpos = bottomLeftPoint.z + tmp - 1;

				Vector3 CoverPos = new Vector3(bottomLeftPoint.x, bottomLeftPoint.y, Zpos);
				Debug.DrawLine(CoverPos, CoverPos + Vector3.left);
				Gizmos.color = Color.black;
				Vector3 oppositCver = new Vector3(bottomLeftPoint.x + width, bottomLeftPoint.y, Zpos);
				Debug.DrawLine(oppositCver, oppositCver + Vector3.right);

				tmp -= 1;
			}


			tmp = bottomRightPoint.x - bottomLeftPoint.x;

			while (tmp > 1)
			{

				float Xpos = bottomLeftPoint.x + tmp - 1;

				Vector3 CoverPos = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z);
				Gizmos.color = Color.green;
				Debug.DrawLine(CoverPos, CoverPos + Vector3.back);


				Gizmos.color = Color.black;
				Vector3 oppositCver = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z + depth);
				Debug.DrawLine(oppositCver, oppositCver + Vector3.forward);

				tmp -= 1;
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

		// called when ever theire is some wall, we want to not cross through it but still can move the node  in other cases
		public void NotifieNeighborsWithSomeRestriction(WallDirection direction)
		{
			foreach (Node neighbor in neighbours)
			{
				switch (direction)
				{
					case WallDirection.Right:
						canGoRight = false;

						if (neighbor.X > X && neighbor.Y == Y) // right 
						{
							neighbor.canGoLeft = false;
						}

						break;
					case WallDirection.Left:
						canGoLeft = false;

						if (neighbor.X < X && neighbor.Y == Y) // left neighbor 
						{
							neighbor.canGoRight = false;
						}
						break;
					case WallDirection.Top:
						canGoTop = false;

						if (neighbor.Y > Y && neighbor.X == X) // top
						{
							neighbor.canGoBottom = false;
						}
						break;
					case WallDirection.Bottom:
						canGoBottom = false;

						if (neighbor.Y < Y && neighbor.X == X) // btom neighbor
						{
							neighbor.canGoTop = false;
						}
						break;
					case WallDirection.Middle:
						isObstacle = true;
						break;
					default:
						break;
				}





				// tell the left neighbor that he can talk/go/search to me (this node)
			}
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

		public bool canReachNeighbor(Node neighbor)
		{
			// if left neighbor
			if (neighbor.X < X && neighbor.Y == Y)
			{
				return canGoLeft;
			}
			if (neighbor.X > X && neighbor.Y == Y) // right 
			{
				return canGoRight;
			}
			if (neighbor.Y > Y && neighbor.X == X) // top
			{
				return canGoTop;
			}
			if (neighbor.Y < Y && neighbor.X == X) // btom neighbor
			{
				return canGoBottom;
			}
			return true;
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
		[NonSerialized]

		public bool isBlocked = false;

		public bool canGoLeft = false;
		public bool canGoRight = false;
		public bool canGoTop = false;
		public bool canGoBottom = false;

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
			//isBlocked = false;
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
		private float floorSizeX;
		private float floorSizeY;
		public FloorGrid(float X, float Y, Floor floor, int Nodesize = 1)
		{
			// Y is same(replace) to floor.z

			// todo: if we add one row/col and we dont have enough space to go(move) to the the center of that node
			// we have to calculate the nearest point on the navmesh using SampleNamv methode

			nodeSize = Nodesize;
			floorSizeX = X;
			floorSizeY = Y;

			width = Mathf.FloorToInt(floorSizeX / nodeSize);
			height = Mathf.FloorToInt(floorSizeY / nodeSize);
			//Debug.Log($"original {X}, modul {X % nodeSize} ,  {Y} modul {Y % nodeSize}   , sized/ 4 = { (float)nodeSize / 8}");
			//if (X % nodeSize >= (float)nodeSize / 8)
			//{
			//	Debug.Log($"we added 1  To the X (width) ");
			//	width++;
			//}
			//if (Y % nodeSize >= (float)nodeSize / 8)
			//{
			//	Debug.Log($"we added 1  To the Y (height) ");
			//	height++;
			//}

			//Debug.Log($"grid [{width},{height}] in {floor} with cell size {nodeSize}");
			nodeRadius = nodeSize / 2;
			nodes = new Node[height, width];
			this.floor = floor;
			generateNodes();


		}


		public void GetNodeCoord(Floor floor, LayerMask floorLayer, out int destinationX, out int destinationY, Camera cam = null)
		{

			// todo: modify tyhis methode to handle nodeSize changes ( now it works only for nodeSize = 1)

			if (cam == null) cam = Camera.main;
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			Debug.DrawRay(ray.origin, ray.direction, Color.black);
			if (Physics.Raycast(ray, out RaycastHit hit, floorLayer))
			{

				Vector3 worldPosition = ray.GetPoint(hit.distance);
				if (worldPosition.x >= floor.transform.position.x - (float)floor.grid.height / 2 && worldPosition.x <= (float)floor.grid.height / 2 + floor.transform.position.x
					&& worldPosition.z >= floor.transform.position.z - (float)floor.grid.width / 2 && worldPosition.z <= (float)floor.grid.width / 2 + floor.transform.position.z)
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

			if (X >= 0 && X < height && Y >= 0 && Y < width)
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
					//Debug.Log($" j {j} -> x ({nodes[x, y].LocalCoord.x}), i {i} -> z ({nodes[x, y].LocalCoord.z})");
					if (nodes[x, y].LocalCoord.x == j && nodes[x, y].LocalCoord.z == i)
					{
						return nodes[x, y];
					}
				}
			}
			return null;
		}

		public Node GetNode(Transform prefab)
		{
			int percentX, percentY;
			float tmpX, tmpY;

			tmpX = (prefab.position.z - buttonLeft.z) / nodeSize;
			tmpY = (prefab.position.x - buttonLeft.x) / nodeSize;

			percentX = Mathf.RoundToInt(tmpX);
			percentY = Mathf.RoundToInt(tmpY);


			return GetNode(percentY, percentX);
		}

		public Node GetNode(Vector3 Pos)
		{
			int percentX, percentY;
			float tmpX, tmpY;

			tmpX = (Pos.z - buttonLeft.z) / nodeSize;
			tmpY = (Pos.x - buttonLeft.x) / nodeSize;

			percentX = Mathf.RoundToInt(tmpX);
			percentY = Mathf.RoundToInt(tmpY);


			return GetNode(percentY, percentX);
		}

		public Node GetSafeNode(Vector3 Pos)
		{
			int percentX, percentY;
			float tmpX, tmpY;

			tmpX = (Pos.z - buttonLeft.z) / nodeSize;
			tmpY = (Pos.x - buttonLeft.x) / nodeSize;

			percentX = Mathf.RoundToInt(tmpX);
			percentY = Mathf.RoundToInt(tmpY);


			if (percentX < 0)
			{
				percentX = 0;
			}
			if (percentX >= height)
			{
				percentX = height - 1;
			}
			if (percentY < 0)
			{
				percentY = 0;
			}
			if (percentY >= width)
			{
				percentY = width - 1;
			}

			return GetNode(percentX, percentY);
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
						currentNode.canGoLeft = true;
						currentNode.neighbours.Add(nodes[x - 1, y]);
					}
					//X is not mapSizeX - 1, then we can add right (x + 1)
					if (x < height - 1)
					{
						currentNode.canGoRight = true;
						currentNode.neighbours.Add(nodes[x + 1, y]);
					}
					//Y is not 0, then we can add downwards (y - 1 )
					if (y > 0)
					{
						currentNode.canGoBottom = true;
						currentNode.neighbours.Add(nodes[x, y - 1]);
					}
					//Y is not mapSizeY -1, then we can add upwards (y + 1)
					if (y < width - 1)
					{
						currentNode.canGoTop = true;
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
