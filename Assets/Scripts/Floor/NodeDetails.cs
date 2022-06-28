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

	public class TargetDetail
	{
		private List<CoverTransform> CoverGOList;
		public Transform transform;
		public CoverNode CoverSpot;
		public float Aim = 0;
		public AgentManager Agent;
		public NodeDetails TargetedBy;

		public TargetDetail(NodeDetails Detail)
		{
			TargetedBy = Detail;
			if (TargetedBy == null) Debug.Log($"targeted by  is null");
			Agent = Detail.TargetManager;
			transform = Agent.transform;
			CoverGOList = Detail.CoverGOList;
			CoverSpot = Detail.GetCoverNode(Agent);
		}
	}

	public class CoverNode
	{
		public CoverTransform CoverTransform;
		public Node node;
		public float Value { get; set; }
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

		public override string ToString()
		{
			return $"cover node {node } of GO {CoverTransform.name}";
		}
	}

	[Serializable]
	public class CoverTransform
	{
		public HashSet<CoverNode> _coverList = new HashSet<CoverNode>();

		public HashSet<CoverNode> CoverList
		{ get { return _coverList; } }

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

		public void calculateValue(AgentManager agent)
		{
			float agentHeight = agent.GetComponent<Renderer>().bounds.size.y;
			float GOHeight = Transform.GetComponent<Renderer>().bounds.size.y;
			float percent = Mathf.Clamp01(agentHeight / GOHeight);
			float typevalue = 0;
			if (type == CoverType.Thin)
			{
				typevalue = 0.3f;
			}
			else if (type == CoverType.Small)
			{
				typevalue = 0.6f;
			}
			else if (type == CoverType.Thick)
			{
				typevalue = 1f;
			}

			float value = (typevalue + percent) / 2;

			foreach (CoverNode cover in CoverList)
			{
				cover.Value = value;
			}
		}
	}

	public class NodeDetails
	{
		public TargetDetail Target;
		public AgentManager TargetManager;
		public Node someNodePosition;
		public List<CoverTransform> CoverGOList;
		public float Value;
		public List<Node> CoverAvailable = new List<Node>();
		public List<Node> FlunkedCover = new List<Node>();
		public AgentManager UnitManager;
		public CoverNode AgentCoverSpot;
		public HashSet<RangeNode> nodeInRanger;

		public NodeDetails(AgentManager unit, AgentManager Target, Node someNodePosition, List<CoverTransform> coverTransforms, HashSet<RangeNode> nodeInRanger)
		{
			TargetManager = Target;
			UnitManager = unit;
			this.someNodePosition = someNodePosition;
			CoverGOList = coverTransforms;
			this.nodeInRanger = nodeInRanger;
			this.Target = new TargetDetail(this);
			this.Target.Aim = CalculateAim();
			AgentCoverSpot = GetCoverNode(UnitManager);

			UpdateCoverSpots(Target);
			calculateNodeDetailsScore();
		}


		private void calculateNodeDetailsScore()
		{

			float score = 1;

			score *= Target.Aim;

			float agentCoverValue = 0.1f;
			if (AgentCoverSpot != null)
			{
				AgentCoverSpot.CoverTransform.calculateValue(UnitManager);
				agentCoverValue = AgentCoverSpot.Value;
			}
			score *= agentCoverValue;

			Value = average(score, 2);
		}
		public CoverNode GetCoverNode(AgentManager target)
		{
			if (CoverGOList == null || CoverGOList.Count == 0)
			{
				Debug.Log($" couldn't found Any Cover  ");
				return null;
			}
			Node TargetNode = target.LocomotionSystem.ActiveFloor.grid.GetNode(target.transform);
			CoverNode MyCover = null;
			foreach (CoverTransform cover in CoverGOList)
			{
				foreach (CoverNode CoverSpot in cover.CoverList)
				{
					if (CoverSpot.node == TargetNode)
						MyCover = CoverSpot;
				}
			}

			return MyCover;
		}

		private void UpdateCoverSpots(AgentManager Target)
		{
			foreach (CoverTransform coverTransform in CoverGOList)
			{
				CheckCoverTowardTarget(coverTransform, Target, nodeInRanger);
			}
		}

		public enum TargetDirectionTowardCoverGO
		{
			none,
			front, left, right, back,
			topRight,
			bottomRight,
			topLeft,
			bottomLeft
		}

		private float RoundFloat(float value, int nb)
		{
			return Mathf.Round(value * Mathf.Pow(10, nb)) * (1 / Mathf.Pow(10, nb));
		}

		public void CheckCoverTowardTarget(CoverTransform CoverGameObject, AgentManager Target, HashSet<RangeNode> nodeInRange)
		{
			Vector3 targetDir = CoverGameObject.Transform.position - Target.transform.position;

			Vector3 rightSide = (CoverGameObject.TopRightPoint + CoverGameObject.bottomRightPoint) / 2;

			Vector3 leftSide = (CoverGameObject.bottomLeftPoint + CoverGameObject.TopLeftPoint) / 2;

			Vector3 frontSide = (CoverGameObject.bottomLeftPoint + CoverGameObject.bottomRightPoint) / 2;

			Vector3 backSide = (CoverGameObject.TopRightPoint + CoverGameObject.TopLeftPoint) / 2;

			TargetDirectionTowardCoverGO direction = TargetDirectionTowardCoverGO.none;

			float targetX = RoundFloat(Target.transform.position.x, 2), TargetZ = RoundFloat(Target.transform.position.z, 2);

			float maxX = RoundFloat(CoverGameObject.bottomRightPoint.x, 2), minX = RoundFloat(CoverGameObject.bottomLeftPoint.x, 2);
			float minZ = RoundFloat(CoverGameObject.bottomRightPoint.z, 2), maxZ = RoundFloat(CoverGameObject.TopRightPoint.z, 2);

			// if the target is facing one of the 4 side of the cover
			if ((targetX >= minX && targetX <= maxX) || (TargetZ >= minZ && TargetZ <= maxZ))
			{
				if (Physics.Raycast(Target.transform.position, targetDir, out RaycastHit hit, LayerMask.GetMask(new string[2] { "Cover", "Obstacle" })))
				{
					float x = RoundFloat(hit.point.x, 2), z = RoundFloat(hit.point.z, 2);

					if (z == minZ) direction = TargetDirectionTowardCoverGO.front;
					else if (z == maxZ) direction = TargetDirectionTowardCoverGO.back;
					else if (x == minX) direction = TargetDirectionTowardCoverGO.right;
					else if (x == maxX) direction = TargetDirectionTowardCoverGO.left;
				}
			}
			else // if he is on diagonal sides
			{
				if (targetX > maxX)
				{
					if (TargetZ > maxZ) direction = TargetDirectionTowardCoverGO.topRight;
					else if (TargetZ < minZ) direction = TargetDirectionTowardCoverGO.bottomRight;
				}
				else if (targetX < minX)
				{
					if (TargetZ > maxZ) direction = TargetDirectionTowardCoverGO.topLeft;
					else if (TargetZ < minZ) direction = TargetDirectionTowardCoverGO.bottomLeft;
				}
			}
			Vector3 origin = Vector3.zero, DotProductDirrection = Vector3.zero;
			if (direction == TargetDirectionTowardCoverGO.left)
			{
				origin = rightSide;
				DotProductDirrection = origin - leftSide;
			}
			else if (direction == TargetDirectionTowardCoverGO.right)
			{
				origin = leftSide;
				DotProductDirrection = origin - rightSide;
			}
			else if (direction == TargetDirectionTowardCoverGO.front)
			{
				origin = frontSide;
				DotProductDirrection = origin - backSide;
			}
			else if (direction == TargetDirectionTowardCoverGO.back)
			{
				origin = backSide;
				DotProductDirrection = origin - frontSide;
			}
			else if (direction == TargetDirectionTowardCoverGO.bottomRight)
			{
				Vector3 Axe1 = CoverGameObject.bottomLeftPoint - CoverGameObject.TopRightPoint;

				Vector2 dir1 = new Vector2(Axe1.x, Axe1.z);
				Vector2 perpendicular = Vector2.Perpendicular(dir1);
				Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
				origin = CoverGameObject.Transform.position;
				DotProductDirrection = perp;
			}
			else if (direction == TargetDirectionTowardCoverGO.topRight)
			{
				Vector3 Axe2 = CoverGameObject.bottomRightPoint - CoverGameObject.TopLeftPoint;

				Vector2 dir1 = new Vector2(Axe2.x, Axe2.z);
				Vector2 perpendicular = Vector2.Perpendicular(dir1);
				Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
				origin = CoverGameObject.Transform.position;
				DotProductDirrection = perp;
			}
			else if (direction == TargetDirectionTowardCoverGO.topLeft)
			{
				Vector3 Axe1 = CoverGameObject.TopRightPoint - CoverGameObject.bottomLeftPoint;

				Vector2 dir1 = new Vector2(Axe1.x, Axe1.z);
				Vector2 perpendicular = Vector2.Perpendicular(dir1);
				Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
				origin = CoverGameObject.Transform.position;
				DotProductDirrection = perp;
			}
			else if (direction == TargetDirectionTowardCoverGO.bottomLeft)
			{
				Vector3 Axe2 = CoverGameObject.TopLeftPoint - CoverGameObject.bottomRightPoint;

				Vector2 dir1 = new Vector2(Axe2.x, Axe2.z);
				Vector2 perpendicular = Vector2.Perpendicular(dir1);
				Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
				origin = CoverGameObject.Transform.position;
				DotProductDirrection = perp;
			}
			List<Node> AgentActiveTargetPositions = new List<Node>();
			foreach (Transform target in UnitManager.Targests)
			{
				AgentActiveTargetPositions.Add(target.GetComponent<AgentManager>().LocomotionSystem.CurentPositon);
			}
			foreach (CoverNode cover in CoverGameObject.CoverList)
			{
				// if cover spot is not in range ignore it
				if (nodeInRange.Select(el => el.node).Contains(cover.node) == false) continue;
				// if the cover node is occupied by any of the target exclude it
				if (AgentActiveTargetPositions.Contains(cover.node)) continue;

				Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, origin.y, cover.node.LocalCoord.z);
				Vector3 coverDir = coverPos - origin;

				if (Vector3.Dot(DotProductDirrection, coverDir) > 0)
				{
					Gizmos.color = Color.red;
					FlunkedCover.Add(cover.node);
					cover.Available = false;
				}
				else
				{
					Gizmos.color = Color.green;
					CoverAvailable.Add(cover.node);
					cover.Available = true;
				}
			}
		}

		public float CalculateAim()
		{
			// weapon range = 5
			float maxRange = UnitManager.weapon.MaxRange;
			float distance = Vector3.Distance(someNodePosition.LocalCoord, Target.transform.position);
			float percent = distance / maxRange;

			float score = 1 - Mathf.Clamp01(percent);

			// target cover Value
			float CoverValue = Target.CoverSpot?.Value ?? 0.1f;
			score *= (1.1f - CoverValue);

			// Target stats
			float DefenseValue = Target.Agent.stats.Defense / 10;
			score *= DefenseValue;

			// target health
			float maxdamage = UnitManager.weapon.Damage;
			float targethealth = Target.Agent.stats.Health;

			// damage val
			float DamageValue = Mathf.Clamp01(maxdamage / targethealth);
			score *= DamageValue;

			return average(score, 5);
		}

		private float average(float score, int NBconditions)
		{
			float originalScore = score;
			float modFactor = 1 - (1 / NBconditions);
			float makeupValue = (1 - originalScore) * modFactor;
			return originalScore + (makeupValue * originalScore);
		}
	}
}