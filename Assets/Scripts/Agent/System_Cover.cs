using GridNameSpace;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AgentManager)), ExecuteInEditMode]
public class System_Cover : MonoBehaviour
{
	public LayerMask CoversLayers;
	private Collider[] Colliders = new Collider[10]; // more is less performant, but more options
	public List<CoverTransform> coverTransforms { get; set; } = new List<CoverTransform>();

	bool alreadCreated = false;
	private AgentManager AiAgent;
	public HashSet<CoverNode> AvailableCover = new HashSet<CoverNode>();
	public HashSet<CoverNode> FlunkedCoverSpot = new HashSet<CoverNode>();



	public bool showAimPercent;
	public GameObject aimPrefab;
	public Transform parentCanvas;



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
	private void AddFlunckedSpot(CoverNode cover)
	{

		cover.Available = false;
		cover.Value = 0;
		FlunkedCoverSpot.Add(cover);


	}


	public void awake(AgentManager agent)
	{
		AiAgent = agent;
	}
	public void start(Transform Target)
	{
		AvailableCover.Clear();
		GetAllCoverInRange();
	}
	public CoverNode CalculateThePerfectoffensive()
	{
		AvailableCover.Clear();
		FlunkedCoverSpot.Clear();
		GetAllCoverInRange();

		return GetPerfectCoverSpotForShooting();
	}


	public CoverNode CalculateThePerfectDefense()
	{
		AvailableCover.Clear();
		FlunkedCoverSpot.Clear();
		GetAllCoverInRange();

		return GetPerfectCoverSpotForDefense();
	}

	public void GetAllCoverInRange()
	{
		int Hits = Physics.OverlapSphereNonAlloc(AiAgent.agent.transform.position, 10, Colliders, CoversLayers); // 10 is radius of vision
		coverTransforms.Clear();
		for (int i = 0; i < Hits; i++)
		{
			CoverTransform obj = new CoverTransform(AiAgent.LocomotionSystem.ActiveFloor.grid, Colliders[i].transform);
			//obj.CreatePotentialCover(obj, 0.5f);
			coverTransforms.Add(obj);

		}
		foreach (CoverTransform CoverGO in coverTransforms)
		{
			CoverGO.CoverList?.Clear();
			CreateallCoverspot(CoverGO);
			CheckCoverTowardTarget(CoverGO, AiAgent.Target, AiAgent.LocomotionSystem.NodeInRange);

		}
	}





	private float RoundFloat(float value, int nb)
	{
		return Mathf.Round(value * Mathf.Pow(10, nb)) * (1 / Mathf.Pow(10, nb));
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

	public CoverNode GetPerfectCoverSpotForDefense()
	{
		if (AvailableCover == null || AvailableCover.Count == 0) return null;

		// update AvailableCover

		float maxValue = Mathf.Max(AvailableCover.Select(el => el.Value).ToArray());
		foreach (CoverNode cover in AvailableCover)
		{

			if (cover.Value == maxValue)
				return cover;
			//GameObject obj = Instantiate(aimPrefab, new Vector3(cover.node.LocalCoord.x, 1.5f, cover.node.LocalCoord.z), Quaternion.Euler(aimPrefab.transform.rotation.eulerAngles), parentCanvas);
			//obj.transform.GetChild(1).GetComponent<Text>().text = $"{aimpercent * 100}";
		}

		return null;
	}
	public CoverNode GetPerfectCoverSpotForShooting()
	{
		if (AvailableCover == null) return null;

		// update AvailableCover
		foreach (CoverNode cover in AvailableCover)
		{
			float aimpercent = CalculateAimPercent(AiAgent, AiAgent.Target, cover);
			cover.AimPercent = aimpercent;

			//GameObject obj = Instantiate(aimPrefab, new Vector3(cover.node.LocalCoord.x, 1.5f, cover.node.LocalCoord.z), Quaternion.Euler(aimPrefab.transform.rotation.eulerAngles), parentCanvas);
			//obj.transform.GetChild(1).GetComponent<Text>().text = $"{aimpercent * 100}";
		}

		// order the list by aimpercent
		var ordred = AvailableCover.OrderByDescending(x => x.AimPercent).ToList();
		// return greatest aim
		if (ordred.Count > 0)
		{
			return ordred[0];
		}
		return null;
	}
	public void CheckCoverTowardTarget(CoverTransform CoverGameObject, Transform Target, HashSet<RangeNode> nodeInRange)
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
			// if cover spot is not in range ignore it
			if (nodeInRange.Select(el => el.node).Contains(cover.node) == false) continue;



			Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, origin.y, cover.node.LocalCoord.z);
			Vector3 coverDir = coverPos - origin;

			if (Vector3.Dot(DotProductDirrection, coverDir) > 0)
			{
				Gizmos.color = Color.red;
				AddFlunckedSpot(cover);
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
	private void OnDrawGizmos()
	{

		if (coverTransforms != null)
		{
			AvailableCover.Clear();
			FlunkedCoverSpot.Clear();
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
			if (showAimPercent && alreadCreated == false)
			{
				alreadCreated = true;
				foreach (CoverNode cover in AvailableCover)
				{
					showAimPercent = false;
					alreadCreated = true;


					float aimpercent = CalculateAimPercent(AiAgent, AiAgent.Target, cover);


					GameObject obj = Instantiate(aimPrefab, new Vector3(cover.node.LocalCoord.x, 1.5f, cover.node.LocalCoord.z), Quaternion.Euler(aimPrefab.transform.rotation.eulerAngles), parentCanvas);
					obj.transform.GetChild(1).GetComponent<Text>().text = $"{aimpercent * 100}";
				}
			}
		}
	}

	public CoverNode GetUnitCover(Transform unit)
	{
		if (AvailableCover == null || AvailableCover.Count == 0)
		{
			GetAllCoverInRange();
		}
		AgentManager TargetManager = unit.transform.GetComponent<AgentManager>();
		Node TargetNode = TargetManager.LocomotionSystem.curentPositon ?? AiAgent.LocomotionSystem.ActiveFloor.grid.GetNode(TargetManager.transform);

		CoverNode TargetCover = AvailableCover.FirstOrDefault(el => el.node == TargetNode);

		return TargetCover;
	}



	public float CalculateAimPercent(AgentManager unit, Transform target, CoverNode cover)
	{
		float globalPenaltiToAim;
		float aimPercent = 1;
		float TargetCoverTypePenalty;




		float TargetWeaponPenalty = cover.getweaponPenaltyPenalty(target, unit.weapon);

		// suppose the trget have 4 defense and max defense is 10
		float TargetDefencePenalty = (float)4 / (float)10;
		//TargetDefencePercent /= 2;
		//globalPenaltiToAim += TargetDefencePercent;




		// get the cover of the target
		CoverNode TargetCover = GetUnitCover(target);
		if (TargetCover == null)
		{
			TargetCoverTypePenalty = 0;
		}
		else
		{
			TargetCoverTypePenalty = TargetCover.getCoverTypePenalty(unit.transform);
		}







		float Cover_Target_Distance = Vector3.Distance(cover.node.LocalCoord, target.transform.position); ;

		// distance from spot to target, 
		float distancepercent = 1 - (Cover_Target_Distance / unit.weapon.MaxRange);
		// we add small amount to the aimpercent just to have different values aim in the cover with the same value
		distancepercent = Mathf.Clamp01(distancepercent) / 10;

		aimPercent += distancepercent;



		globalPenaltiToAim = TargetCoverTypePenalty + TargetWeaponPenalty + TargetDefencePenalty;
		aimPercent -= (globalPenaltiToAim / 3);


		return aimPercent;
	}

}

