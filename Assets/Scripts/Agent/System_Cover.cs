using GridNameSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AgentManager)), ExecuteInEditMode]
public class System_Cover : MonoBehaviour
{
	public LayerMask CoversLayers;
	private Collider[] Colliders = new Collider[10]; // more is less performance, but more options
	public List<CoverTransform> coverTransforms { get; set; } = new List<CoverTransform>();
	public TargetDetail BestTarget { get; private set; }
	public Node BestCoverPosition { get; private set; }
	public List<Node> AllAvailableCover = new List<Node>();
	public List<Node> AllFlunkedCover = new List<Node>();
	public static Action<float, Transform> AimEvent { get; set; }
	public List<NodeDetails> NodeDetailsList { get; set; } = new List<NodeDetails>();

	public void SetBestTarget(TargetDetail Target)
	{
		BestTarget = Target;
	}
	public void SetBestCoverPosition(Node cover)
	{
		BestCoverPosition = cover;
	}


	private AgentManager _BestTarget;

	public bool alreadCreated = false;
	private AgentManager AiAgent;

	public bool showAimPercent;
	public GameObject aimPrefab;
	public Transform parentCanvas;

	[SerializeField]
	public bool ShowCreationSports = false;

	[SerializeField]
	private bool ShowPotentialSpots = false;

	[SerializeField]
	private bool ShowPerfectSpot = false;

	public void awake(AgentManager agent)
	{
		AiAgent = agent;
	}

	public void start()
	{
		CreateNodeDetailsForMyCurrentPosition();
		GetSomeGoodCover();
		CoverNode mycover = GetCoverNode(AiAgent.LocomotionSystem.CurentPositon);
		//Debug.Log($"{mycover}");
		bool MyCoverIsGoodd = IsTheCoverNodeGood(mycover);
		TargetDetail bestTarger = GetPerfectTarget();
		TargetDetail bestTargerforMyposition = GetPerfectTargetForMyPosition();

		//Debug.Log($" best target for my position is {bestTargerforMyposition.Agent.name}, best node to aim at him is {bestTargerforMyposition.TargetedBy.someNodePosition} ");
		//Debug.Log($" best target is {bestTarger.Agent.name}, best node to aim at him is {bestTarger.TargetedBy.someNodePosition} ");

		//foreach (var item in NodeDetailsList)
		//{
		//	Debug.Log($"{item.Target.transform.name} aim => {item.Target.Aim}");
		//}
	}

	public bool IsTheCoverNodeGood(CoverNode Cover)
	{
		if (Cover == null) return false;

		foreach (NodeDetails detail in NodeDetailsList)
		{
			AllAvailableCover.AddRange(detail.CoverAvailable);
			AllFlunkedCover.AddRange(detail.FlunkedCover);
		}

		var NodesCoveredByMaxPlayer = AllAvailableCover
			.GroupBy(p => p)
			.OrderByDescending(p => p.Count())
			.ToDictionary(p => p.Key, q => q.Count());

		return NodesCoveredByMaxPlayer.Keys.Contains(Cover.node);
	}

	public CoverNode GetCoverNode(Node node)
	{
		if (coverTransforms == null || coverTransforms.Count == 0)
		{
			Debug.Log($" couldn't found Any Cover  ");
			return null;
		}
		CoverNode MyCover = null;

		// all covers that are adjacent to my position
		List<CoverNode> CoverNodes = new List<CoverNode>();

		foreach (CoverTransform cover in coverTransforms)
		{
			cover.calculateValue(AiAgent);
			foreach (CoverNode CoverSpot in cover.CoverList)
			{
				if (CoverSpot.node == node)
				{
					MyCover = CoverSpot;
					CoverNodes.Add(CoverSpot);
				}
			}
		}
		float bestValuecoverIhave = Mathf.Max(CoverNodes.Select(el => el.Value).ToArray());

		MyCover = CoverNodes.FirstOrDefault(el => el.Value == bestValuecoverIhave);

		return MyCover;
	}

	public List<NodeDetails> CreateNodeDetailsFor(Node npcPosition)
	{
		List<NodeDetails> nodeDetailsList = new List<NodeDetails>();
		foreach (Transform target in AiAgent.Targests)
		{
			AgentManager TargetAgent = target.GetComponent<AgentManager>();
			nodeDetailsList.Add(new NodeDetails(AiAgent, TargetAgent, npcPosition, coverTransforms, AiAgent.LocomotionSystem.NodeInRange));
		}
		return nodeDetailsList;
	}

	public void CreateNodeDetailsForMyCurrentPosition()
	{
		CreateAllPossibleCoverInRangeOfVision(10);
		Node npcPosition = AiAgent.LocomotionSystem.ActiveFloor.grid.GetNode(AiAgent.transform);
		NodeDetailsList.Clear();
		foreach (Transform target in AiAgent.Targests)
		{
			AgentManager TargetAgent = target.GetComponent<AgentManager>();
			NodeDetailsList.Add(new NodeDetails(AiAgent, TargetAgent, npcPosition, coverTransforms, AiAgent.LocomotionSystem.NodeInRange));
		}
	}

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
		foreach (CoverTransform cover in coverTransforms)
		{
			cover.calculateValue(AiAgent);
		}
	}

	public TargetDetail GetPerfectTargetForMyPosition()
	{
		CreateNodeDetailsForMyCurrentPosition();
		return GetPerfectTargetWithinSomeNodeDetails(NodeDetailsList);
	}

	public TargetDetail GetPerfectTarget()
	{
		AllAvailableCover.Clear();
		AllFlunkedCover.Clear();
		CreateNodeDetailsForMyCurrentPosition();
		foreach (NodeDetails detail in NodeDetailsList)
		{
			AllAvailableCover.AddRange(detail.CoverAvailable);
			AllFlunkedCover.AddRange(detail.FlunkedCover);
		}

		var NodesCoveredByMaxPlayer = AllAvailableCover
			.GroupBy(p => p)
			.OrderByDescending(p => p.Count())
			.ToDictionary(p => p.Key, q => q.Count());
		int maxOccurence = NodesCoveredByMaxPlayer.FirstOrDefault().Value;

		List<Node> bestNodes = (from el in NodesCoveredByMaxPlayer
					where el.Value == maxOccurence
					select el.Key).ToList();
		List<NodeDetails> listNodeDetails = new List<NodeDetails>();
		List<TargetDetail> bestTargets = new List<TargetDetail>();
		foreach (Node node in bestNodes)
		{
			listNodeDetails = CreateNodeDetailsFor(node);
			TargetDetail bestTarget = GetPerfectTargetWithinSomeNodeDetails(listNodeDetails);
			bestTargets.Add(bestTarget);
		}
		float bestAim = Mathf.Max(bestTargets.Select(el => el.Aim).ToArray());

		return bestTargets.FirstOrDefault(el => el.Aim == bestAim);
	}

	public TargetDetail GetPerfectTargetWithinSomeNodeDetails(List<NodeDetails> nodeDetails)
	{
		float maxAim = Mathf.Max(nodeDetails.Select(el => el.Target.Aim).ToArray());
		return nodeDetails.FirstOrDefault(el => el.Target.Aim == maxAim)?.Target;
	}

	public Node GetSomeGoodCover()
	{
		foreach (NodeDetails detail in NodeDetailsList)
		{
			AllAvailableCover.AddRange(detail.CoverAvailable);
			AllFlunkedCover.AddRange(detail.FlunkedCover);
		}

		var NodesCoveredByMaxPlayer = AllAvailableCover
			.GroupBy(p => p)
			.OrderByDescending(p => p.Count())
			.ToDictionary(p => p.Key, q => q.Count());

		return NodesCoveredByMaxPlayer.FirstOrDefault().Key;
	}

	private float RoundFloat(float value, int nb)
	{
		return Mathf.Round(value * Mathf.Pow(10, nb)) * (1 / Mathf.Pow(10, nb));
	}

	public void CreateallCoverspot(CoverTransform CoverGameObject)
	{
		CoverGameObject.CoverList.Clear();
		// ------------------------- Create all Cover spot
		// -----------------------------------
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

	// I dont have to pass the Target => to fill the available Cover I need to loop throw all
	// TArget and fill it but i dont ned to pass the target
	//public CoverNode GetTargetCover(Transform Target)
	//{
	//	if (coverTransforms == null || coverTransforms.Count == 0)
	//	{
	//		Debug.Log($" couldent found Any Cover for {Target.transform.name} ");
	//		return null;
	//	}
	//	Node UnitNode = AiAgent.LocomotionSystem.ActiveFloor.grid.GetNode(Target);

	// CoverNode TargetCover = null; foreach (CoverTransform cover in coverTransforms) { foreach
	// (CoverNode CoverSpot in cover.CoverList) { if (CoverSpot.node == UnitNode) TargetCover =
	// CoverSpot; } }

	//	return TargetCover;
	//}

	//public CoverNode GetMyCoverNode(List<CoverTransform> covers)
	//{
	//	if (covers == null || covers.Count == 0)
	//	{
	//		Debug.Log($" couldn't found Any Cover  ");
	//		return null;
	//	}
	//	Node UnitNode = AiAgent.LocomotionSystem.ActiveFloor.grid.GetNode(AiAgent.transform);

	// CoverNode MyCover = null; foreach (CoverTransform cover in covers) { foreach (CoverNode
	// CoverSpot in cover.CoverList) { if (CoverSpot.node == UnitNode) MyCover = CoverSpot; } }

	//	return MyCover;
	//}

	private void OnDrawGizmos()
	{
		if (AiAgent?.LocomotionSystem?.ActiveFloor?.grid == null) return;
		if (alreadCreated == false)
		{
			alreadCreated = true;

			CreateNodeDetailsForMyCurrentPosition();
			Debug.Log($"onnce ");
		}

		int count = 0;
		AllAvailableCover.Clear();
		AllFlunkedCover.Clear();
		foreach (NodeDetails detail in NodeDetailsList)
		{
			Vector3 offset = Vector3.one * 0.2f * count;
			AllAvailableCover.AddRange(detail.CoverAvailable);
			AllFlunkedCover.AddRange(detail.FlunkedCover);
			foreach (Node cover in detail.CoverAvailable)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(cover.LocalCoord + offset + Vector3.left * 0.2f * (count + 1), 0.4f);
			}
			foreach (Node flunk in detail.FlunkedCover)
			{
				if (detail.CoverAvailable.Contains(flunk))
				{
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(flunk.LocalCoord + offset + Vector3.right * 0.2f * (count + 1), 0.4f);
				}
				else
				{
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(flunk.LocalCoord + offset + Vector3.left * 0.2f * (count - 1), 0.4f);
				}
			}
			count++;
		}
	}

	private void AddPotentialNodeCover(List<Node> coverAvailable)
	{
		throw new NotImplementedException();
	}

	private void AddFlunckedCover(List<Node> flunkedCover)
	{
		throw new NotImplementedException();
	}
}