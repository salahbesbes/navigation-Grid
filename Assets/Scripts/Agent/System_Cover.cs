using GridNameSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AgentManager)), ExecuteInEditMode]
public class System_Cover : MonoBehaviour
{
	public LayerMask CoversLayers;
	private Collider[] Colliders = new Collider[10]; // more is less performant, but more options
	public List<CoverTransform> coverTransforms { get; set; } = new List<CoverTransform>();
	public AgentManager BestTarget { get; private set; }

	public static Action<float, Transform> AimEvent { get; set; }


	public void SetBestTarget(AgentManager Target)
	{
		BestTarget = Target;
	}
	public void SetBestTarget(Transform Target)
	{
		AgentManager TargetManager = Target.GetComponent<AgentManager>();
		if (TargetManager != null)
			BestTarget = TargetManager;
		else
		{
			Debug.Log($"cant set  {Target.name} as Best TArget it must have AgentManage on It");
		}
	}
	private AgentManager _BestTarget;

	public AgentManager GetBestTarget()
	{
		if (AiAgent.Targests == null || AiAgent.Targests.Count == 0) return null;
		AvailableCover.Clear();
		CreateAllPossibleCoverInRangeOfVision(10);
		GetAllCoverInRangeOfMovementForAllTargets(AiAgent.Targests);

		Dictionary<AgentManager, float> FlunckedAgents = new Dictionary<AgentManager, float>();
		Dictionary<AgentManager, CoverDetails> CoveredAgents = new Dictionary<AgentManager, CoverDetails>();

		foreach (Transform target in AiAgent.Targests)
		{
			AgentManager TargerAgent = target.GetComponent<AgentManager>();

			Node TargetNode = TargerAgent.LocomotionSystem.curentPositon ?? TargerAgent.LocomotionSystem.ActiveFloor.grid.GetNode(target);

			CoverNode coverNode = GetTargetCover(target);
			if (coverNode != null)
			{
				CoverDetails CoverDetail = new CoverDetails(coverNode, AiAgent.transform, target);
				CoveredAgents.Add(TargerAgent, CoverDetail);
			}
			else
			{
				FlunckedAgents.Add(TargerAgent, CoverDetails.CalculateAimPercentStatic(AiAgent.transform, target));

			}

		}

		if (FlunckedAgents.Count > 0)
		{
			Dictionary<AgentManager, float> AgentsCovers = (from element in FlunckedAgents
									orderby element.Value descending
									select element).ToDictionary(keySelector: m => m.Key, elementSelector: m => m.Value);

			var coverSpot = AgentsCovers.FirstOrDefault();

			return coverSpot.Key;

		}

		if (CoveredAgents.Count > 0)
		{
			Dictionary<AgentManager, CoverDetails> AgentsCovers = (from element in CoveredAgents
									       orderby element.Value.AimPercent descending
									       orderby element.Value.Value ascending
									       select element).ToDictionary(keySelector: m => m.Key, elementSelector: m => m.Value);

			var coverSpot = AgentsCovers.FirstOrDefault();

			return coverSpot.Key;
		}
		return null;


	}

	public Transform BestTArgetFor(CoverNode coverSpot)
	{
		float bestAim = 0;
		Transform BestTarget = null;

		foreach (Transform target in AiAgent.Targests)
		{
			//AgentManager TargerAgent = target.GetComponent<AgentManager>();

			//Node TargetNode = TargerAgent.LocomotionSystem.curentPositon ?? TargerAgent.LocomotionSystem.ActiveFloor.grid.GetNode(target);

			CoverNode coverNode = GetTargetCover(target);


			if (coverNode != null)
			{

				float aim = CoverDetails.CalculateAimPercentStatic(AiAgent.transform, target, coverSpot);

				AimEvent.Invoke(aim, target);

				if (aim > bestAim)
				{
					bestAim = aim;
					BestTarget = target;
					//Debug.Log($" aim found  {CoverDetail.AimPercent}   new aim calcuated {new CoverDetails(CoverDetail.CoverSpot, AiAgent.transform, target)}");
				}
			}
			else
			{
				float aim = CoverDetails.CalculateAimPercentStatic(AiAgent.transform, target, coverSpot);
				AimEvent.Invoke(aim, target);

				if (aim > bestAim)
				{
					bestAim = aim;
					BestTarget = target;
					//Debug.Log($" aim found  {CoverDetail.AimPercent}   new aim calcuated {new CoverDetails(CoverDetail.CoverSpot, AiAgent.transform, target)}");
				}
			}


		}
		return BestTarget;
	}

	bool alreadCreated = false;
	private AgentManager AiAgent;
	public Dictionary<Node, List<CoverDetails>> AvailableCover = new Dictionary<Node, List<CoverDetails>>();
	public Dictionary<Node, List<CoverDetails>> FlunkedCoverSpot = new Dictionary<Node, List<CoverDetails>>();



	public bool showAimPercent;
	public GameObject aimPrefab;
	public Transform parentCanvas;



	[SerializeField]
	public bool ShowCreationSports = false;
	[SerializeField]
	private bool ShowPotentialSpots = false;
	[SerializeField]
	private bool ShowPerfectSpot = false;

	private void AddDetailOfTargetToCoverSpot(CoverNode cover, Transform target)
	{
		if (cover == null)
			Debug.Log($"cover is null");
		CoverDetails details = new CoverDetails(cover, AiAgent.transform, target);




		// exlude The cover Detail if the TArget is alread Standing on It ( because any other Target cant take this cover
		Node TargetNode = target.GetComponent<AgentManager>().LocomotionSystem.ActiveFloor.grid.GetNode(target.position);
		if (AvailableCover.TryGetValue(TargetNode, out List<CoverDetails> ListOfdetails))
		{
			AvailableCover[TargetNode] = ListOfdetails.Where(el => el.CoverSpot.node != TargetNode).ToList();

		}





		if (TargetNode == cover.node) return;
		if (AvailableCover.Keys.Contains(cover.node))
		{
			AvailableCover[cover.node].Add(details);
		}
		else
		{
			AvailableCover.Add(cover.node, new List<CoverDetails> { details });
		}


	}
	private void AddFlunckedSpot(CoverNode cover, Transform target)
	{

		CoverDetails details = new CoverDetails(cover, AiAgent.transform, target);
		if (cover == null)
			Debug.Log($"fllunkec cover is null");

		if (FlunkedCoverSpot.Keys.Contains(cover.node))
		{
			FlunkedCoverSpot[cover.node].Add(details);
		}
		else
		{
			FlunkedCoverSpot.Add(cover.node, new List<CoverDetails> { details });
		}


	}


	public void awake(AgentManager agent)
	{
		AiAgent = agent;
	}
	public void start()
	{
		AvailableCover.Clear();
		CreateAllPossibleCoverInRangeOfVision(10);
		GetAllCoverInRangeOfMovementForAllTargets(AiAgent.Targests);

		CoverDetails bestCover = GetThePrefectSpotForShooting();



		Debug.Log($"Best Cover That Covers  Max Targets {bestCover}");
	}

	public CoverDetails GetThePrefectSpotForShooting()
	{
		int maxCount = Mathf.Max(AvailableCover.Values.Select(el => el.Count).ToArray());
		List<CoverDetails> GetAllDetailsThatCoversTheTarget = (from element in AvailableCover
								       where element.Value.Count == maxCount
								       from el in element.Value
									       //where el.CoverSpot.node != TArgertNode
								       select el).ToList();
		//Debug.Log($" GetAllDetailsThatCoversTheTarget count {GetAllDetailsThatCoversTheTarget.Count}");

		Dictionary<Node, int> NodeOccurence = new Dictionary<Node, int>();
		//Iterate through the values, setting count to 1 or incrementing current count.
		foreach (CoverDetails detail in GetAllDetailsThatCoversTheTarget)
			if (NodeOccurence.ContainsKey(detail.CoverSpot.node))
				NodeOccurence[detail.CoverSpot.node]++;
			else
				NodeOccurence[detail.CoverSpot.node] = 1;

		List<Node> tmppp = (from entry in NodeOccurence orderby entry.Value descending select entry.Key).ToList();
		Node besNode = tmppp.FirstOrDefault();



		List<CoverDetails> GetAllDetails = (from element in GetAllDetailsThatCoversTheTarget
						    where element.CoverSpot.node == besNode
						    select element).ToList();

		return GetBestAimVal(GetAllDetails);

	}

	private CoverDetails GetBestVal(List<CoverDetails> list)
	{
		float MaxVal = Mathf.Max(list.Select(el => el.Value).ToArray());

		foreach (CoverDetails cover in list)
		{
			if (cover.Value == MaxVal)
			{
				return cover;
			}
		}
		return null;
	}
	private CoverDetails GetBestAimVal(List<CoverDetails> list)
	{
		float MaxVal = Mathf.Max(list.Select(el => el.AimPercent).ToArray());
		foreach (CoverDetails cover in list)
		{
			if (cover.AimPercent == MaxVal)
			{
				return cover;
			}
		}
		return null;
	}
	private CoverDetails GetBestAimVal(CoverNode targetCoverSpot, CoverNode fromThisSpot)
	{
		if (AvailableCover.TryGetValue(targetCoverSpot.node, out var covers))
		{
			return covers.FirstOrDefault(el => el.CoverSpot.node == targetCoverSpot.node && el.Unit == AiAgent.transform);
		}
		return null;
	}



	public List<CoverDetails> CalculateBestCoverSpots(Transform Target)
	{

		// cover that have the most Target Covered
		Dictionary<Node, List<CoverDetails>> SortedDict = AvailableCover.OrderByDescending(el => el.Value.Count)
								.ToDictionary(keySelector: m => m.Key, elementSelector: m => m.Value);

		// nb of target Known by the node (Cover Spot)


		// get all Element that have same target nb
		int BestOfSortedDict = Mathf.Max(SortedDict.Values.Select(el => el.Count).ToArray());




		List<CoverDetails> GetAllDetailsThatCoversTheTarget = (from element in SortedDict
								       where element.Value.Count == BestOfSortedDict
								       from el in element.Value
								       where el.Target == Target
								       select el).ToList();




		//List<CoverDetails> BestListOfCoverDetails = new List<CoverDetails>();

		CoverDetails BestCoverDetailForTarget = GetBestAimVal(GetAllDetailsThatCoversTheTarget);
		return GetAllDetailsThatCoversTheTarget;

		//Debug.Log($"best aim detail : {BestCoverDetailForTarget}");
		//return BestCoverDetailForTarget;

	}


	public CoverDetails GetBestCoverSpotOffensive(Transform Target)
	{

		// cover that have the most Target Covered
		Dictionary<Node, List<CoverDetails>> SortedDict = AvailableCover.OrderByDescending(el => el.Value.Count)
								.ToDictionary(keySelector: m => m.Key, elementSelector: m => m.Value);

		// nb of target Known by the node (Cover Spot)


		// get all Element that have same target nb
		int BestOfSortedDict = Mathf.Max(SortedDict.Values.Select(el => el.Count).ToArray());


		List<CoverDetails> GetAllDetailsThatCoversTheTarget = (from element in SortedDict
								       where element.Value.Count == BestOfSortedDict
								       from el in element.Value
								       where el.Target == Target
								       select el).ToList();




		//List<CoverDetails> BestListOfCoverDetails = new List<CoverDetails>();

		CoverDetails BestCoverDetailForTarget = GetBestAimVal(GetAllDetailsThatCoversTheTarget);

		Debug.Log($"best aim detail : {BestCoverDetailForTarget}");
		return BestCoverDetailForTarget;

	}

	//public CoverNode CalculateThePerfectDefense()
	//{
	//	AvailableCover.Clear();
	//	FlunkedCoverSpot.Clear();
	//	GetAllCoverInRange(Target);

	//	return GetPerfectCoverSpotForDefense();
	//}

	public void CreateAllPossibleCoverInRangeOfVision(float RangeOfVision)
	{
		int Hits = Physics.OverlapSphereNonAlloc(AiAgent.agent.transform.position, RangeOfVision, Colliders, CoversLayers); // 10 is radius of vision
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
		}
	}

	// this methode will loop throw all Target in ListOTargets and get all valid cover spot and add it to the AvailableCover List
	// the Available cover list will have duplicate Cover Spot because the cover can be for multiple target 
	// the best cover spot is the one shared by all Targets
	public void GetAllCoverInRangeOfMovementForAllTargets(List<Transform> ListOTargets)
	{
		foreach (Transform Target in ListOTargets)
		{
			GetAllCoverInRangeOfMovementFor(Target);

		}
	}


	// this methode will loop throw all CoverGO in coverTransforms and check for a valid cover spot and add it to the availlableCover List

	public void GetAllCoverInRangeOfMovementFor(Transform Target)
	{

		foreach (CoverTransform CoverGO in coverTransforms)
		{
			CheckCoverTowardTarget(CoverGO, Target, AiAgent.LocomotionSystem.NodeInRange);
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

	public CoverDetails GetPerfectCoverSpotForDefense(Transform Target)
	{
		if (AvailableCover == null || AvailableCover.Count == 0) return null;
		// cover that have the most Target Covered
		Dictionary<Node, List<CoverDetails>> SortedDict = AvailableCover.OrderByDescending(el => el.Value.Count)
								.ToDictionary(keySelector: m => m.Key, elementSelector: m => m.Value);

		int NbOfGreatestTargetCovered = SortedDict.FirstOrDefault().Value.Count;



		foreach (var List in SortedDict.Values)
		{
			//var tmp = List.FindAll(el => el.Target == Target);
		}



		// nb of target Known by the node (Cover Spot)

		// get all Element that have same target nb
		var BestOfSortedDict = SortedDict.Where(el => el.Value.Count == NbOfGreatestTargetCovered)
						.ToDictionary(keySelector: m => m.Key, elementSelector: m => m.Value);


		List<CoverDetails> BestListOfCoverDetails = new List<CoverDetails>();

		CoverDetails BestDetailIntheDict = null;
		float BestValueIntheDict = int.MinValue;

		foreach (var details in BestOfSortedDict)
		{
			// calculate best val for a cover spot
			CoverDetails BestCoverDetailForSpot = GetBestVal(details.Value);
			if (BestCoverDetailForSpot.Value > BestValueIntheDict)
			{
				BestValueIntheDict = BestCoverDetailForSpot.Value;
				BestDetailIntheDict = BestCoverDetailForSpot;
			}
		}

		Debug.Log($" best aim detail :  {BestDetailIntheDict}");
		return BestDetailIntheDict;

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


	// I dont have to pass the Target => to fill the available Cover I need to loop throw all TArget and fill it 
	// but i dont ned to pass the target
	public CoverNode GetTargetCover(Transform Target)
	{
		if (coverTransforms == null || coverTransforms.Count == 0)
		{
			Debug.Log($" couldent found Any Cover for {Target.transform.name} ");
			return null;
		}
		Node UnitNode = AiAgent.LocomotionSystem.ActiveFloor.grid.GetNode(Target);

		CoverNode TargetCover = null;
		foreach (CoverTransform cover in coverTransforms)
		{
			foreach (CoverNode CoverSpot in cover.CoverList)
			{
				if (CoverSpot.node == UnitNode)
					TargetCover = CoverSpot;

			}
		}


		return TargetCover;
	}
	public CoverNode GetMyCoverNode()
	{
		if (coverTransforms == null || coverTransforms.Count == 0)
		{
			Debug.Log($" couldent found Any Cover  ");
			return null;
		}
		Node UnitNode = AiAgent.LocomotionSystem.ActiveFloor.grid.GetNode(AiAgent.transform);

		CoverNode MyCover = null;
		foreach (CoverTransform cover in coverTransforms)
		{
			foreach (CoverNode CoverSpot in cover.CoverList)
			{
				if (CoverSpot.node == UnitNode)
					MyCover = CoverSpot;

			}
		}


		return MyCover;

	}



	public float getweaponPenaltyPenalty(AgentManager Unit, Transform target, CoverNode ShootingSpot = null)
	{

		float TargetWeaponPenalty = 0;
		// suppose the perfect range of the weapon is 3m => 
		/*
		 *  if distance to target <= 3 => no penalty of the aim
		 *  else if distance to target > 3 => we apply penalty 
		 *  suppose penalty is fixed to 0.3
		 */
		float perfectShotDistance = 5;
		float Cover_Target_Distance;


		Vector3 ShootPosition = ShootingSpot != null ? ShootingSpot.node.LocalCoord : AiAgent.transform.position;
		Cover_Target_Distance = Vector3.Distance(ShootPosition, target.transform.position); ;

		if (Unit.weapon.type == WeaponType.shortRange)
		{

			if (Cover_Target_Distance <= perfectShotDistance)
			{
				TargetWeaponPenalty = 0;
			}
			else
			{
				TargetWeaponPenalty = 0.3f;
			}

		}
		return TargetWeaponPenalty;
	}

	//public float CalculateAimPercent(AgentManager unit, Transform target, CoverNode UnitCover)
	//{
	//	float globalPenaltiToAim;
	//	float aimPercent = 1;
	//	float TargetCoverTypePenalty;




	//	float TargetWeaponPenalty = getweaponPenaltyPenalty(AiAgent, target);

	//	// suppose the trget have 4 defense and max defense is 10
	//	float TargetDefencePenalty = (float)4 / (float)10;
	//	//TargetDefencePercent /= 2;
	//	//globalPenaltiToAim += TargetDefencePercent;




	//	// get the cover of the target
	//	CoverDetails TargetCoverDetails = GetTargetCover(target);
	//	if (TargetCoverDetails == null)
	//	{
	//		TargetCoverTypePenalty = 0;
	//	}
	//	else
	//	{
	//		TargetCoverTypePenalty = TargetCoverDetails.CoverSpot.getCoverTypePenalty(unit.transform);
	//	}







	//	float Cover_Target_Distance = Vector3.Distance(UnitCover.node.LocalCoord, target.transform.position); ;

	//	// distance from spot to target, 
	//	float distancepercent = 1 - (Cover_Target_Distance / unit.weapon.MaxRange);
	//	// we add small amount to the aimpercent just to have different values aim in the cover with the same value
	//	distancepercent = Mathf.Clamp01(distancepercent) / 10;

	//	aimPercent += distancepercent;



	//	globalPenaltiToAim = TargetCoverTypePenalty + TargetWeaponPenalty + TargetDefencePenalty;
	//	aimPercent -= (globalPenaltiToAim / 3);


	//	return aimPercent;
	//}
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

		foreach (CoverNode cover in CoverGameObject.CoverList)
		{
			// if cover spot is not in range ignore it
			if (nodeInRange.Select(el => el.node).Contains(cover.node) == false) continue;



			Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, origin.y, cover.node.LocalCoord.z);
			Vector3 coverDir = coverPos - origin;

			if (Vector3.Dot(DotProductDirrection, coverDir) > 0)
			{
				Gizmos.color = Color.red;
				AddFlunckedSpot(cover, Target);
				cover.Available = false;
			}
			else
			{
				Gizmos.color = Color.green;
				AddDetailOfTargetToCoverSpot(cover, Target);
				cover.Available = true;

			}
		}
	}

	private void OnDrawGizmos()
	{

		if (coverTransforms != null && AiAgent != null)
		{
			AvailableCover.Clear();
			FlunkedCoverSpot.Clear();

			foreach (Transform Target in AiAgent.Targests)
			{
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
							//Gizmos.DrawSphere(CoverPos, 0.1f);


							Vector3 oppositCver = new Vector3(CoverTransform.bottomLeftPoint.x + offset + CoverTransform.width, CoverTransform.bottomLeftPoint.y, Zpos);
							CoverTransform.CreateNewCoverSpot(oppositCver);
							//Gizmos.DrawSphere(oppositCver, 0.1f);




							tmp -= 0.7f;
						}


						tmp = CoverTransform.bottomRightPoint.x - CoverTransform.bottomLeftPoint.x;

						while (tmp > 0.7f)
						{

							float Xpos = CoverTransform.bottomLeftPoint.x + tmp - 0.7f;
							Vector3 CoverPos = new Vector3(Xpos, CoverTransform.bottomLeftPoint.y, CoverTransform.bottomLeftPoint.z - offset);
							CoverTransform.CreateNewCoverSpot(CoverPos);
							//Gizmos.DrawSphere(CoverPos, 0.1f);

							Vector3 oppositCver = new Vector3(Xpos, CoverTransform.bottomLeftPoint.y, CoverTransform.bottomLeftPoint.z + offset + CoverTransform.depth);
							CoverTransform.CreateNewCoverSpot(oppositCver);
							//Gizmos.DrawSphere(oppositCver, 0.1f);


							tmp -= 0.7f;
						}
						foreach (CoverNode cover in CoverTransform.CoverList)
						{
							Vector3 coverPos = new Vector3(cover.node.LocalCoord.x, CoverTransform.Transform.position.y, cover.node.LocalCoord.z);
							//Gizmos.DrawSphere(coverPos, 0.2f);

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




						Vector3 targetDir = CoverTransform.Transform.position - Target.position;

						Vector3 rightSide = (CoverTransform.TopRightPoint + CoverTransform.bottomRightPoint) / 2;


						Vector3 leftSide = (CoverTransform.bottomLeftPoint + CoverTransform.TopLeftPoint) / 2;

						Vector3 frontSide = (CoverTransform.bottomLeftPoint + CoverTransform.bottomRightPoint) / 2;

						Vector3 backSide = (CoverTransform.TopRightPoint + CoverTransform.TopLeftPoint) / 2;


						TargetDirectionTowardCoverGO direction = TargetDirectionTowardCoverGO.none;

						float targetX = RoundFloat(Target.position.x, 2), TargetZ = RoundFloat(Target.position.z, 2);

						float maxX = RoundFloat(CoverTransform.bottomRightPoint.x, 2), minX = RoundFloat(CoverTransform.bottomLeftPoint.x, 2);
						float minZ = RoundFloat(CoverTransform.bottomRightPoint.z, 2), maxZ = RoundFloat(CoverTransform.TopRightPoint.z, 2);



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
								//Gizmos.DrawSphere(coverPos, 0.2f);
								AddFlunckedSpot(cover, Target);
								cover.Available = false;

							}
							else
							{
								//Gizmos.DrawSphere(coverPos, 0.2f);
								AddDetailOfTargetToCoverSpot(cover, Target);
								cover.Available = true;

							}
						}

					}



					//foreach (var dict in AvailableCover)
					//{
					//	foreach (CoverDetails spot in dict.Value)
					//	{
					//		if (dict.Value.Count == maxCount)
					//		{
					//			Gizmos.color = Color.green;
					//			Gizmos.DrawSphere(spot.CoverSpot.node.LocalCoord, 0.2f);
					//		}
					//		else
					//		{

					//			Gizmos.color = Color.red;
					//			Gizmos.DrawSphere(spot.CoverSpot.node.LocalCoord, 0.2f);
					//		}

					//	}

					//}


				}




				// -------------  calculate CoverSpot Value --------------------------------------------------

				//if (showAimPercent && alreadCreated == false)
				//{
				//	alreadCreated = true;
				//	foreach (CoverNode cover in AvailableCover)
				//	{
				//		showAimPercent = false;
				//		alreadCreated = true;


				//		float aimpercent = CalculateAimPercent(AiAgent, Target, cover);


				//		GameObject obj = Instantiate(aimPrefab, new Vector3(cover.node.LocalCoord.x, 1.5f, cover.node.LocalCoord.z), Quaternion.Euler(aimPrefab.transform.rotation.eulerAngles), parentCanvas);
				//		obj.transform.GetChild(1).GetComponent<Text>().text = $"{aimpercent * 100}";
				//	}
				//}

				AgentManager Agent = Target.GetComponent<AgentManager>();

				Node TArgertNode = Agent.LocomotionSystem.ActiveFloor.grid.GetNode(Target);


				//List<CoverDetails> Eliminate = 
				if (AvailableCover.TryGetValue(TArgertNode, out List<CoverDetails> details))
				{
					AvailableCover[TArgertNode] = details.Where(el => el.CoverSpot.node != TArgertNode).ToList();
				}


			}


			int maxCount = Mathf.Max(AvailableCover.Values.Select(el => el.Count).ToArray());
			List<CoverDetails> GetAllDetailsThatCoversTheTarget = (from element in AvailableCover
									       where element.Value.Count == maxCount
									       from el in element.Value
										       //where el.CoverSpot.node != TArgertNode
									       select el).ToList();
			//Debug.Log($" GetAllDetailsThatCoversTheTarget count {GetAllDetailsThatCoversTheTarget.Count}");

			Dictionary<Node, int> NodeOccurence = new Dictionary<Node, int>();
			//Iterate through the values, setting count to 1 or incrementing current count.
			foreach (CoverDetails detail in GetAllDetailsThatCoversTheTarget)
				if (NodeOccurence.ContainsKey(detail.CoverSpot.node))
					NodeOccurence[detail.CoverSpot.node]++;
				else
					NodeOccurence[detail.CoverSpot.node] = 1;

			List<Node> tmppp = (from entry in NodeOccurence orderby entry.Value descending select entry.Key).ToList();
			Node besNode = tmppp.FirstOrDefault();



			List<CoverDetails> GetAllDetails = (from element in GetAllDetailsThatCoversTheTarget
							    where element.CoverSpot.node == besNode
							    select element).ToList();

			Debug.Log($" last details count  {GetAllDetails.Count}");
			foreach (var item in GetAllDetails)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(item.CoverSpot.node.LocalCoord, 0.2f);

			}
		}
	}



}

