using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridNameSpace
{
	public enum CoverType
	{
		None,
		Thin,
		Small,
		Destructable,
		Thick,
	}




	public class CoverDetails
	{
		public Transform Target;
		public Transform Unit;
		public float AimPercent { get; set; }
		public bool InMovementRange = true;
		public float Value { get; set; }

		public CoverNode CoverSpot { get; set; }
		public CoverDetails(CoverNode CoverSpot, Transform Unit, Transform Target)
		{
			this.Unit = Unit;
			this.Target = Target;
			this.CoverSpot = CoverSpot;
			if (this.CoverSpot == null) Debug.Log($"unit {Unit}  target {Target}");
			SetValue();
			AimPercent = CalculateAimPercent();
		}

		private float CalculateAimPercent()
		{

			float globalPenaltiToAim;
			float aimPercent = 1;
			float TargetCoverTypePenalty;

			AgentManager Agent = Unit.GetComponent<AgentManager>();
			if (Agent == null) return 0;

			float TargetWeaponPenalty = Agent.coverSystem.getweaponPenaltyPenalty(Agent, Target);

			// suppose the trget have 4 defense and max defense is 10
			float TargetDefencePenalty = (float)4 / (float)10;
			//TargetDefencePercent /= 2;
			//globalPenaltiToAim += TargetDefencePercent;




			// get the cover of the target
			CoverNode TargetCover = Agent.coverSystem.GetTargetCover(Target);
			if (TargetCover == null)
			{
				TargetCoverTypePenalty = 0;
			}
			else
			{
				TargetCoverTypePenalty = TargetCover.getCoverTypePenalty(Unit);
			}







			float Cover_Target_Distance = Vector3.Distance(CoverSpot.node.LocalCoord, Target.position); ;

			// distance from spot to target, 
			float distancepercent = 1 - (Cover_Target_Distance / Agent.weapon.MaxRange);
			// we add small amount to the aimpercent just to have different values aim in the cover with the same value
			distancepercent = Mathf.Clamp01(distancepercent) / 10;

			aimPercent += distancepercent;



			globalPenaltiToAim = TargetCoverTypePenalty + TargetWeaponPenalty + TargetDefencePenalty;
			aimPercent -= (globalPenaltiToAim / 3);

			return aimPercent;
		}

		private void SetValue()
		{

			Value = 0;

			// set height value
			float maxheight = Unit.transform.localScale.y * 2;
			float heightpercent = CoverSpot.CoverTransform.Transform.localScale.y / maxheight;
			heightpercent = Mathf.Clamp01(heightpercent);
			Value += heightpercent;

			// set CoverType 
			if (CoverSpot.CoverTransform.type == CoverType.Small)
			{
				Value += 0.25f;
			}
			else if (CoverSpot.CoverTransform.type == CoverType.Thick)
			{
				Value += 0.4f;
			}
			else if (CoverSpot.CoverTransform.type == CoverType.Thin)
			{
				Value += 0.2f;
			}

			// set Cover Distance (suppose max range weapon is 10)
			float maxWeaponDistance = 10f;

			float DistanceToPlayer = Mathf.Clamp(Vector3.Distance(CoverSpot.node.LocalCoord, Target.position), 0, 10);
			float distancePercent = 1 - (DistanceToPlayer) / maxWeaponDistance;
			Value += distancePercent;


			// TODO: average ( we need to found other methode, solution to calculate the average ) 
			Value = Value / 3;

		}


		public override string ToString()
		{
			return $" detail about the Cover spot {CoverSpot.node} and the Target {Target.name}";
		}


		public static float CalculateAimPercentStatic(Transform Unit, Transform Target, CoverNode ShootingSpot)
		{

			float globalPenaltiToAim;
			float aimPercent = 1;
			float TargetCoverTypePenalty;

			AgentManager Agent = Unit.GetComponent<AgentManager>();
			if (Agent == null) return 0;

			float TargetWeaponPenalty = Agent.coverSystem.getweaponPenaltyPenalty(Agent, Target, ShootingSpot);

			// suppose the trget have 4 defense and max defense is 10
			float TargetDefencePenalty = (float)4 / (float)10;
			//TargetDefencePercent /= 2;
			//globalPenaltiToAim += TargetDefencePercent;




			// get the cover of the target
			CoverNode TargetCover = Agent.coverSystem.GetTargetCover(Target);

			if (TargetCover == null)
			{
				TargetCoverTypePenalty = 0;
			}
			else
			{
				TargetCoverTypePenalty = TargetCover.getCoverTypePenalty(Unit);
			}







			float Cover_Target_Distance = Vector3.Distance(ShootingSpot.node.LocalCoord, Target.position); ;

			// distance from spot to target, 
			// we add small amount to the aimpercent just to have different values aim in the cover with the same value
			float DistanceScore = Agent.weapon.Evaluate(Cover_Target_Distance);
			//float distancepercent = Mathf.Clamp01(DistancePenalty) / 10;

			//aimPercent += distancepercent;



			globalPenaltiToAim = TargetCoverTypePenalty + TargetWeaponPenalty + TargetDefencePenalty + (1 - DistanceScore);
			aimPercent -= (globalPenaltiToAim / 4);

			return aimPercent;
		}

		public static float CalculateAimPercentStatic(Transform Unit, Transform Target)
		{

			float globalPenaltiToAim;
			float aimPercent = 1;
			float TargetCoverTypePenalty;

			AgentManager Agent = Unit.GetComponent<AgentManager>();
			if (Agent == null) return 0;

			float TargetWeaponPenalty = Agent.coverSystem.getweaponPenaltyPenalty(Agent, Target);

			// suppose the trget have 4 defense and max defense is 10
			float TargetDefencePenalty = (float)4 / (float)10;
			//TargetDefencePercent /= 2;
			//globalPenaltiToAim += TargetDefencePercent;




			// get the cover of the target
			CoverNode TargetCover = Agent.coverSystem.GetTargetCover(Target);

			if (TargetCover == null)
			{
				TargetCoverTypePenalty = 0;
			}
			else
			{
				TargetCoverTypePenalty = TargetCover.getCoverTypePenalty(Unit);
			}







			float Cover_Target_Distance = Vector3.Distance(Unit.transform.position, Target.position); ;

			// distance from spot to target, 
			float distancepercent = 1 - (Cover_Target_Distance / Agent.weapon.MaxRange);
			// we add small amount to the aimpercent just to have different values aim in the cover with the same value
			distancepercent = Mathf.Clamp01(distancepercent) / 10;

			aimPercent += distancepercent;



			globalPenaltiToAim = TargetCoverTypePenalty + TargetWeaponPenalty + TargetDefencePenalty;
			aimPercent -= (globalPenaltiToAim / 3);

			return aimPercent;
		}

	}
	public class CoverNode
	{
		public CoverTransform CoverTransform;
		public Node node;


		private bool _available = false;
		public bool Available
		{
			get => _available;
			set => _available = value;
		}










		public CoverNode(Node node, CoverTransform CoverTransform)
		{
			this.node = node;
			this.CoverTransform = CoverTransform;

		}




		public float getCoverTypePenalty(Transform unit)
		{
			float Value = 0;
			float maxheight = unit.transform.GetComponent<Renderer>().bounds.size.y;
			float coverheight = CoverTransform.Transform.GetComponent<Renderer>().bounds.size.y;

			float heightpercent = coverheight / maxheight;
			heightpercent = Mathf.Clamp01(heightpercent);
			Value += heightpercent;

			// set CoverType 
			if (CoverTransform.type == CoverType.Small)
			{
				Value += 0.25f;
			}
			else if (CoverTransform.type == CoverType.Thick)
			{
				Value += 0.5f;
			}
			else if (CoverTransform.type == CoverType.Thin)
			{
				Value += 0.2f;
			}
			return Value / 2;
		}

	}
	[Serializable]
	public class CoverTransform
	{

		public HashSet<CoverNode> _coverList = new HashSet<CoverNode>();
		public HashSet<CoverNode> CoverList { get { return _coverList; } }
		public string name = "default";
		public int id = 0;
		public Transform Transform;
		public CoverType type;
		public float height { get; set; }
		public CoverNode BestCoverAvailable { get; set; }
		public FloorGrid grid;
		public Vector3 HorozentalAxe { get; private set; }

		public Vector3 VerticalAxe { get; private set; }
		public bool isVertical { get; internal set; }


		public Vector3 bottomLeftPoint { get; private set; }
		public Vector3 TopLeftPoint { get; private set; }
		public Vector3 TopRightPoint { get; private set; }
		public Vector3 bottomRightPoint { get; private set; }
		public float width { get; private set; }
		public float depth { get; private set; }

		public CoverTransform(FloorGrid grid, Transform transform)
		{
			this.grid = grid;
			name = transform.name;
			id = transform.GetInstanceID();
			Transform = transform;
			//float volume = transform.localScale.x * transform.localScale.z * transform.localScale.y;
			if (Mathf.Min(Transform.localScale.x, Transform.localScale.z) <= 0.4f)
			{
				type = CoverType.Thin;
			}
			else if (Mathf.Max(Transform.localScale.x, Transform.localScale.z) < 2.5f)
			{
				type = CoverType.Small;
			}
			else
			{
				type = CoverType.Thick;
			}
			height = transform.transform.localScale.y;

			if (Transform.rotation.eulerAngles.y % 180 == 0)
			{
				width = Transform.localScale.x;
				depth = Transform.localScale.z;
			}
			else
			{
				width = Transform.localScale.z;
				depth = Transform.localScale.x;
			}
			bottomLeftPoint = new Vector3(Transform.position.x - width / 2, Transform.position.y, Transform.position.z - depth / 2);
			TopLeftPoint = new Vector3(Transform.position.x - width / 2, Transform.position.y, Transform.position.z + depth / 2);

			TopRightPoint = new Vector3(TopLeftPoint.x + width, TopLeftPoint.y, TopLeftPoint.z);
			bottomRightPoint = new Vector3(bottomLeftPoint.x + width, bottomLeftPoint.y, bottomLeftPoint.z);



		}

		public void CreateNewCoverSpot(Vector3 position)
		{
			Node node = grid.GetNode(position);
			if (node == null) return;

			List<Node> allNodesInCoverList = CoverList.Select(el => el.node).ToList();

			if (allNodesInCoverList.Contains(node)) return;


			CoverList.Add(new CoverNode(node, this));
		}


		public void CreatePotentialCover(CoverTransform CoverTransform, float offset)
		{

			float tmp = CoverTransform.TopLeftPoint.z - CoverTransform.bottomLeftPoint.z;
			while (tmp > 0.7f)
			{
				float Zpos = CoverTransform.bottomLeftPoint.z + tmp - 0.7f;
				Zpos += offset;

				Vector3 CoverPos = new Vector3(CoverTransform.bottomLeftPoint.x - offset, CoverTransform.bottomLeftPoint.y, Zpos);

				CoverTransform.CreateNewCoverSpot(CoverPos);


				Vector3 oppositCver = new Vector3(CoverTransform.bottomLeftPoint.x + offset + CoverTransform.width, CoverTransform.bottomLeftPoint.y, Zpos);
				CoverTransform.CreateNewCoverSpot(oppositCver);


				tmp -= 0.7f;
			}


			tmp = CoverTransform.bottomRightPoint.x - CoverTransform.bottomLeftPoint.x;

			while (tmp > 0.7f)
			{

				float Xpos = CoverTransform.bottomLeftPoint.x + tmp - 0.7f;
				Vector3 CoverPos = new Vector3(Xpos, CoverTransform.bottomLeftPoint.y, CoverTransform.bottomLeftPoint.z - offset);
				CoverTransform.CreateNewCoverSpot(CoverPos);

				Vector3 oppositCver = new Vector3(Xpos, CoverTransform.bottomLeftPoint.y, CoverTransform.bottomLeftPoint.z + offset + CoverTransform.depth);
				CoverTransform.CreateNewCoverSpot(oppositCver);


				tmp -= 0.7f;
			}



		}

		//public void UpdateBestCover()
		//{
		//	if (CoverList?.Count == 0)
		//	{
		//		BestCoverAvailable = null;
		//		return;
		//	}

		//	CoverNode[] AvailableCovers = (from cover in CoverList
		//				       where cover != null && cover.Available && cover.InMovementRange
		//				       select cover).ToArray();
		//	//Array.Sort(AvailableCovers, new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });

		//	AvailableCovers = AvailableCovers.OrderByDescending(cover => cover.Value)
		//					.OrderBy(cover => cover.DistanceToPlayer).ToArray();

		//	// return 
		//	BestCoverAvailable = AvailableCovers[0];


		//}
	}

	public class RangeNode
	{
		public Node node;
		public bool firstRange { get => _firstRanage; set { _firstRanage = value; SecondRange = !value; } }
		public bool SecondRange { get; set; }
		private bool _firstRanage = false;

		public RangeNode(Node node, bool isInFirstRange)
		{
			this.node = node;
			firstRange = isInFirstRange;
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
