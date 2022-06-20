using GridNameSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;


[RequireComponent(typeof(AgentManager)), ExecuteInEditMode]
public class System_Cover : MonoBehaviour
{
	public LayerMask CoversLayers;
	private Collider[] Colliders = new Collider[10]; // more is less performant, but more options
	public List<CoverTransform> coverTransforms { get; set; } = new List<CoverTransform>();
	private AgentManager AiAgent;
	public HashSet<CoverNode> AvailableCover = new HashSet<CoverNode>();

	[SerializeField]
	public bool ShowCreationSports = false;
	[SerializeField]
	private bool ShowPotentialSpots = false;
	[SerializeField]
	private bool ShowPerfectSpot = false;

	private void AddCoverSpot(CoverNode cover)
	{

		cover.SetValue(AiAgent.transform, AiAgent.Target);
		cover.Available = true;
		AvailableCover.Add(cover);


	}
	public void awake(AgentManager agent)
	{
		AiAgent = agent;
	}
	public void start(Transform Target)
	{
		if (AiAgent.agent.name == "player") return;
		CoverNode perfectCover = AiAgent.coverSystem.CalculateThePerfectNode();
		if (perfectCover != null)
		{
			Debug.Log($"perfect node {perfectCover?.node}");
			Instantiate(AiAgent.LocomotionSystem.ActiveFloor.prefab, perfectCover.node.LocalCoord + Vector3.up, Quaternion.identity).GetComponent<Renderer>().material.color = Color.yellow;
		}
	}
	public CoverNode CalculateThePerfectNode()
	{
		AvailableCover.Clear();
		GetAllCoverInRange();

		return GetPerfectNode();
	}
	//private void Update()
	//{
	//	CoverNode perfectCover = AiAgent.coverSystem.CalculateThePerfectNode(AiAgent.Target);
	//	if (perfectCover != null)
	//	{
	//		Debug.Log($"perfect node {perfectCover?.node}");
	//		Instantiate(AiAgent.LocomotionSystem.ActiveFloor.prefab, perfectCover.node.LocalCoord + Vector3.up, Quaternion.identity).GetComponent<Renderer>().material.color = Color.yellow;
	//	}
	//}

	//private CoverNode GetPerfectCover(HashSet<CoverTransform> coverTransforms)
	//{
	//	if (coverTransforms?.Count == 0) return null;


	//	//Array.Sort(coverTransforms.ToArray(), new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });
	//	var SortedCoverTransforms = coverTransforms.OrderByDescending(cover => cover.height)
	//						.OrderBy(cover => cover.type).ToArray();



	//	HashSet<CoverNode> bestNodesOfTtransforms = new HashSet<CoverNode>();
	//	foreach (CoverTransform cover in SortedCoverTransforms)
	//	{
	//		bestNodesOfTtransforms.Add(cover.BestCoverAvailable);
	//		if (cover.BestCoverAvailable != null)
	//		{
	//			Instantiate(AiAgent.LocomotionSystem.ActiveFloor.prefab, cover.BestCoverAvailable.node.LocalCoord + Vector3.up * 0.5f, Quaternion.identity);

	//		}

	//	}





	//	CoverNode[] BestAvailableCovers = (from cover in bestNodesOfTtransforms
	//					   where cover != null && cover.Available && cover.InMovementRange
	//					   select cover).ToArray();

	//	//Array.Sort(BestAvailableCovers, new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });


	//	BestAvailableCovers = BestAvailableCovers.OrderByDescending(cover => cover.Value)
	//						.OrderBy(cover => cover.DistanceToPlayer)
	//						.OrderByDescending(cover => cover.CoverTransform.type).ToArray();
	//	if (BestAvailableCovers.Length == 0) return null;
	//	return BestAvailableCovers[0];

	//}

	void GetCoverTransformInRange(Transform Target)
	{

		int Hits = Physics.OverlapSphereNonAlloc(AiAgent.agent.transform.position, 10, Colliders, CoversLayers); // 10 is radius of vision
		for (int i = 0; i < Hits; i++)
		{
			CoverTransform obj = new CoverTransform(AiAgent.LocomotionSystem.ActiveFloor.grid, Colliders[i].transform);
			obj.CreatePotentialCover(obj, 0.5f);
			coverTransforms.Add(obj);

		}
		//for (int i = 0; i < Hits; i++)
		//{
		//	if (NavMesh.SamplePosition(Colliders[i].transform.position - (Target.position - Colliders[i].transform.position).normalized, out NavMeshHit hit, 2f, AiAgent.agent.areaMask))
		//	{
		//		if (NavMesh.FindClosestEdge(hit.position, out hit, AiAgent.agent.areaMask))
		//		{
		//			// cover first Cover than create all potential Cover aroyn the CoverTransform
		//			Node nodeCov = AiAgent.LocomotionSystem.ActiveFloor.grid.GetSafeNode(hit.position);
		//			//Instantiate(AiAgent.LocomotionSystem.ActiveFloor.prefab, nodeCov.LocalCoord, Quaternion.identity);
		//			CoverTransform obj = new CoverTransform(nodeCov.grid, Colliders[i].transform);
		//			obj.CreatePotentialCover(obj, 0.5f);
		//			coverTransforms.Add(obj);
		//		}
		//	}
		//}

	}



	public void GetAllCoverInRange()
	{
		int Hits = Physics.OverlapSphereNonAlloc(AiAgent.agent.transform.position, 10, Colliders, CoversLayers); // 10 is radius of vision
		for (int i = 0; i < Hits; i++)
		{
			CoverTransform obj = new CoverTransform(AiAgent.LocomotionSystem.ActiveFloor.grid, Colliders[i].transform);
			//obj.CreatePotentialCover(obj, 0.5f);
			coverTransforms.Add(obj);

		}
		foreach (CoverTransform CoverGO in coverTransforms)
		{
			CoverGO.CoverList.Clear();
			CreateallCoverspot(CoverGO);
			CheckCoverTowardTarget(CoverGO, AiAgent.Target);
		}
	}





	private float RoundFloat(float value, int nb)
	{
		return Mathf.Round(value * 100 * nb) * (1 / Mathf.Pow(10, nb));
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
	public CoverNode GetPerfectNode()
	{
		float maxcoverVal = Mathf.Max(AvailableCover.Select(el => el.Value).ToArray());
		foreach (CoverNode cover in AvailableCover)
		{
			if (cover.Value == maxcoverVal)
			{
				Debug.Log($"start :   perfect node {cover.node} val = {maxcoverVal} count {AvailableCover.Count}");
				return cover;
			}
		}
		return null;
	}
	public void CheckCoverTowardTarget(CoverTransform CoverGameObject, Transform Target)
	{
		Vector3 targetDir = CoverGameObject.Transform.position - Target.position;

		Vector3 rightSide = (CoverGameObject.TopRightPoint + CoverGameObject.bottomRightPoint) / 2;


		Vector3 leftSide = (CoverGameObject.bottomLeftPoint + CoverGameObject.TopLeftPoint) / 2;

		Vector3 frontSide = (CoverGameObject.bottomLeftPoint + CoverGameObject.bottomRightPoint) / 2;

		Vector3 backSide = (CoverGameObject.TopRightPoint + CoverGameObject.TopLeftPoint) / 2;


		TargetDirectionTowardCoverGO direction = TargetDirectionTowardCoverGO.none;

		float targetX = RoundFloat(Target.position.x, 2), TargetZ = RoundFloat(Target.position.z, 2);

		float maxX = RoundFloat(CoverGameObject.bottomRightPoint.x, 2), minX = RoundFloat(CoverGameObject.bottomLeftPoint.x, 2);
		float minZ = RoundFloat(CoverGameObject.bottomRightPoint.z, 2), maxZ = RoundFloat(CoverGameObject.TopRightPoint.z, 2);



		// if the target is facing one of the 4 side of the cover
		if ((targetX >= minX && targetX <= maxX) || (TargetZ >= minZ && TargetZ <= maxZ))
		{
			if (Physics.Raycast(Target.position, targetDir, out RaycastHit hit, CoversLayers))
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

		foreach (var cover in CoverGameObject.CoverList)
		{
			Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, origin.y, cover.node.LocalCoord.z);
			Vector3 coverDir = coverPos - origin;

			if (Vector3.Dot(DotProductDirrection, coverDir) > 0)
			{
				Gizmos.color = Color.red;
				cover.Available = false;
			}
			else
			{
				Gizmos.color = Color.green;
				AddCoverSpot(cover);
				cover.Available = true;

			}
		}
	}
	public void CreateallCoverspot(CoverTransform CoverGameObject)
	{



		CoverGameObject.CoverList.Clear();
		// ------------------------- Create all Cover spot  -----------------------------------
		float offset = 0.5f;

		if (CoverGameObject.type != CoverType.Thin)
		{
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.bottomLeftPoint + new Vector3(-offset, 0, 0));
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.bottomLeftPoint + new Vector3(0, 0, -offset));
		}

		if (CoverGameObject.type != CoverType.Thin)
		{
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.TopLeftPoint + new Vector3(-offset, 0, 0));
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.TopLeftPoint + new Vector3(0, 0, offset));
		}

		if (CoverGameObject.type != CoverType.Thin)
		{
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.TopRightPoint + new Vector3(offset, 0, 0));
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.TopRightPoint + new Vector3(0, 0, offset));
		}

		if (CoverGameObject.type != CoverType.Thin)
		{
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.bottomRightPoint + new Vector3(offset, 0, 0));
			CoverGameObject.CreateNewCoverSpot(CoverGameObject.bottomRightPoint + new Vector3(0, 0, -offset));
		}

		float tmp = CoverGameObject.TopLeftPoint.z - CoverGameObject.bottomLeftPoint.z;
		while (tmp > 0.7f)
		{
			float Zpos = CoverGameObject.bottomLeftPoint.z + tmp - 0.7f;
			Zpos += offset;

			Vector3 CoverPos = new Vector3(CoverGameObject.bottomLeftPoint.x - offset, CoverGameObject.bottomLeftPoint.y, Zpos);

			CoverGameObject.CreateNewCoverSpot(CoverPos);


			Vector3 oppositCver = new Vector3(CoverGameObject.bottomLeftPoint.x + offset + CoverGameObject.width, CoverGameObject.bottomLeftPoint.y, Zpos);
			CoverGameObject.CreateNewCoverSpot(oppositCver);




			tmp -= 0.7f;
		}


		tmp = CoverGameObject.bottomRightPoint.x - CoverGameObject.bottomLeftPoint.x;

		while (tmp > 0.7f)
		{

			float Xpos = CoverGameObject.bottomLeftPoint.x + tmp - 0.7f;
			Vector3 CoverPos = new Vector3(Xpos, CoverGameObject.bottomLeftPoint.y, CoverGameObject.bottomLeftPoint.z - offset);
			CoverGameObject.CreateNewCoverSpot(CoverPos);

			Vector3 oppositCver = new Vector3(Xpos, CoverGameObject.bottomLeftPoint.y, CoverGameObject.bottomLeftPoint.z + offset + CoverGameObject.depth);
			CoverGameObject.CreateNewCoverSpot(oppositCver);

			tmp -= 0.7f;
		}
	}
	private async Task OnDrawGizmos()
	{

		if (coverTransforms != null)
		{
			AvailableCover.Clear();

			foreach (var CoverTransform in coverTransforms)
			{

				CoverTransform.CoverList.Clear();
				// ------------------------- Create all Cover spot  -----------------------------------
				float offset = 0.5f;

				if (CoverTransform.type != CoverType.Thin)
				{
					CoverTransform.CreateNewCoverSpot(CoverTransform.bottomLeftPoint + new Vector3(-offset, 0, 0));
					CoverTransform.CreateNewCoverSpot(CoverTransform.bottomLeftPoint + new Vector3(0, 0, -offset));
				}

				if (CoverTransform.type != CoverType.Thin)
				{
					CoverTransform.CreateNewCoverSpot(CoverTransform.TopLeftPoint + new Vector3(-offset, 0, 0));
					CoverTransform.CreateNewCoverSpot(CoverTransform.TopLeftPoint + new Vector3(0, 0, offset));
				}

				if (CoverTransform.type != CoverType.Thin)
				{
					CoverTransform.CreateNewCoverSpot(CoverTransform.TopRightPoint + new Vector3(offset, 0, 0));
					CoverTransform.CreateNewCoverSpot(CoverTransform.TopRightPoint + new Vector3(0, 0, offset));
				}

				if (CoverTransform.type != CoverType.Thin)
				{
					CoverTransform.CreateNewCoverSpot(CoverTransform.bottomRightPoint + new Vector3(offset, 0, 0));
					CoverTransform.CreateNewCoverSpot(CoverTransform.bottomRightPoint + new Vector3(0, 0, -offset));
				}

				if (ShowCreationSports)
				{
					float tmp = CoverTransform.TopLeftPoint.z - CoverTransform.bottomLeftPoint.z;
					while (tmp > 0.7f)
					{
						float Zpos = CoverTransform.bottomLeftPoint.z + tmp - 0.7f;
						Zpos += offset;

						Vector3 CoverPos = new Vector3(CoverTransform.bottomLeftPoint.x - offset, CoverTransform.bottomLeftPoint.y, Zpos);

						CoverTransform.CreateNewCoverSpot(CoverPos);
						Gizmos.DrawSphere(CoverPos, 0.1f);


						Vector3 oppositCver = new Vector3(CoverTransform.bottomLeftPoint.x + offset + CoverTransform.width, CoverTransform.bottomLeftPoint.y, Zpos);
						CoverTransform.CreateNewCoverSpot(oppositCver);
						Gizmos.DrawSphere(oppositCver, 0.1f);




						tmp -= 0.7f;
					}


					tmp = CoverTransform.bottomRightPoint.x - CoverTransform.bottomLeftPoint.x;

					while (tmp > 0.7f)
					{

						float Xpos = CoverTransform.bottomLeftPoint.x + tmp - 0.7f;
						Vector3 CoverPos = new Vector3(Xpos, CoverTransform.bottomLeftPoint.y, CoverTransform.bottomLeftPoint.z - offset);
						CoverTransform.CreateNewCoverSpot(CoverPos);
						Gizmos.DrawSphere(CoverPos, 0.1f);

						Vector3 oppositCver = new Vector3(Xpos, CoverTransform.bottomLeftPoint.y, CoverTransform.bottomLeftPoint.z + offset + CoverTransform.depth);
						CoverTransform.CreateNewCoverSpot(oppositCver);
						Gizmos.DrawSphere(oppositCver, 0.1f);


						tmp -= 0.7f;
					}
					foreach (CoverNode cover in CoverTransform.CoverList)
					{
						Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, CoverTransform.Transform.position.y, cover.node.LocalCoord.z);
						Gizmos.DrawSphere(coverPos, 0.2f);

					}
				}


				if (ShowPotentialSpots)
				{


					// -----------------------------  Check Cover Toward Target  -------------------------------------


					// these condition are used to check if the cover is thin and have a small depth


					Gizmos.color = Color.red;
					Gizmos.DrawSphere(CoverTransform.bottomLeftPoint, 0.05f);
					Gizmos.color = Color.green;
					Gizmos.DrawSphere(CoverTransform.TopLeftPoint, 0.05f);
					Gizmos.color = Color.black;
					Gizmos.DrawSphere(CoverTransform.TopRightPoint, 0.05f);
					Gizmos.color = Color.yellow;
					Gizmos.DrawSphere(CoverTransform.bottomRightPoint, 0.05f);



					Gizmos.color = Color.cyan;
					Gizmos.DrawLine(CoverTransform.TopRightPoint, CoverTransform.bottomLeftPoint);
					Gizmos.DrawLine(CoverTransform.bottomRightPoint, CoverTransform.TopLeftPoint);




					Vector3 targetDir = CoverTransform.Transform.position - AiAgent.Target.position;

					Vector3 rightSide = (CoverTransform.TopRightPoint + CoverTransform.bottomRightPoint) / 2;


					Vector3 leftSide = (CoverTransform.bottomLeftPoint + CoverTransform.TopLeftPoint) / 2;

					Vector3 frontSide = (CoverTransform.bottomLeftPoint + CoverTransform.bottomRightPoint) / 2;

					Vector3 backSide = (CoverTransform.TopRightPoint + CoverTransform.TopLeftPoint) / 2;


					TargetDirectionTowardCoverGO direction = TargetDirectionTowardCoverGO.none;

					float targetX = RoundFloat(AiAgent.Target.position.x, 2), TargetZ = RoundFloat(AiAgent.Target.position.z, 2);

					float maxX = RoundFloat(CoverTransform.bottomRightPoint.x, 2), minX = RoundFloat(CoverTransform.bottomLeftPoint.x, 2);
					float minZ = RoundFloat(CoverTransform.bottomRightPoint.z, 2), maxZ = RoundFloat(CoverTransform.TopRightPoint.z, 2);



					// if the target is facing one of the 4 side of the cover
					if ((targetX >= minX && targetX <= maxX) || (TargetZ >= minZ && TargetZ <= maxZ))
					{
						if (Physics.Raycast(AiAgent.Target.position, targetDir, out RaycastHit hit, CoversLayers))
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
						Vector3 Axe1 = CoverTransform.bottomLeftPoint - CoverTransform.TopRightPoint;

						Gizmos.color = Color.black;
						Vector2 dir1 = new Vector2(Axe1.x, Axe1.z);
						Vector2 perpendicular = Vector2.Perpendicular(dir1);
						Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
						Gizmos.DrawRay(CoverTransform.Transform.position, perp);
						origin = CoverTransform.Transform.position;
						DotProductDirrection = perp;

					}
					else if (direction == TargetDirectionTowardCoverGO.topRight)
					{
						Vector3 Axe2 = CoverTransform.bottomRightPoint - CoverTransform.TopLeftPoint;

						Gizmos.color = Color.black;
						Vector2 dir1 = new Vector2(Axe2.x, Axe2.z);
						Vector2 perpendicular = Vector2.Perpendicular(dir1);
						Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
						Gizmos.DrawRay(CoverTransform.Transform.position, perp);
						origin = CoverTransform.Transform.position;
						DotProductDirrection = perp;
					}
					else if (direction == TargetDirectionTowardCoverGO.topLeft)
					{
						Vector3 Axe1 = CoverTransform.TopRightPoint - CoverTransform.bottomLeftPoint;

						Gizmos.color = Color.black;
						Vector2 dir1 = new Vector2(Axe1.x, Axe1.z);
						Vector2 perpendicular = Vector2.Perpendicular(dir1);
						Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
						Gizmos.DrawRay(CoverTransform.Transform.position, perp);
						origin = CoverTransform.Transform.position;
						DotProductDirrection = perp;
					}
					else if (direction == TargetDirectionTowardCoverGO.bottomLeft)
					{
						Vector3 Axe2 = CoverTransform.TopLeftPoint - CoverTransform.bottomRightPoint;

						Gizmos.color = Color.black;
						Vector2 dir1 = new Vector2(Axe2.x, Axe2.z);
						Vector2 perpendicular = Vector2.Perpendicular(dir1);
						Vector3 perp = new Vector3(perpendicular.x, targetDir.y, perpendicular.y);
						Gizmos.DrawRay(CoverTransform.Transform.position, perp);
						origin = CoverTransform.Transform.position;
						DotProductDirrection = perp;
					}

					foreach (var cover in CoverTransform.CoverList)
					{
						Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, origin.y, cover.node.LocalCoord.z);
						Vector3 coverDir = coverPos - origin;
						Gizmos.color = Color.blue;
						Gizmos.DrawRay(origin, DotProductDirrection * 2);

						if (Vector3.Dot(DotProductDirrection, coverDir) > 0)
						{
							Gizmos.color = Color.red;
							Gizmos.DrawSphere(coverPos, 0.2f);
							cover.Available = false;
						}
						else
						{
							Gizmos.color = Color.green;
							Gizmos.DrawSphere(coverPos, 0.2f);
							AddCoverSpot(cover);
							cover.Available = true;

						}
					}

				}





			}

			// -------------  calculate CoverSpot Value --------------------------------------------------
			if (ShowPerfectSpot)
			{
				float maxcoverVal = Mathf.Max(AvailableCover.Select(el => el.Value).ToArray());
				foreach (CoverNode cover in AvailableCover)
				{
					Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, cover.CoverTransform.Transform.position.y, cover.node.LocalCoord.z);
					Color color = Color.green;
					if (cover.Value == maxcoverVal)
					{
						Debug.Log($"debug :   perfect node {cover.node} val = {maxcoverVal}  count {AvailableCover.Count}");
						Gizmos.color = Color.yellow;
						Gizmos.DrawSphere(coverPos, 0.4f);
					}
					else
					{
						Gizmos.color = color;
						Gizmos.DrawSphere(coverPos, 0.2f);

					}


				}
			}
		}
	}

	private CoverNode GetCover()
	{
		throw new NotImplementedException();
	}
}

