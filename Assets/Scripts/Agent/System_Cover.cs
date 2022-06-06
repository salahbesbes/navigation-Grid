using GridNameSpace;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(AgentManager))]
public class System_Cover : MonoBehaviour
{
	public LayerMask CoversLayers;

	private Collider[] Colliders = new Collider[10]; // more is less performant, but more options
	public HashSet<CoverTransform> coverTransforms = new HashSet<CoverTransform>();
	private AgentManager AiAgent;


	public void awake(AgentManager agent)
	{
		AiAgent = agent;
	}
	public void start(Transform Target)
	{
		CoverNode perfectCover = AiAgent.coverSystem.CalculateThePerfectNode(Target);
		if (perfectCover != null)
		{
			Debug.Log($"perfect node {perfectCover?.node}");
			Instantiate(AiAgent.LocomotionSystem.ActiveFloor.prefab, perfectCover.node.LocalCoord + Vector3.up, Quaternion.identity).GetComponent<Renderer>().material.color = Color.yellow;
		}
	}
	public CoverNode CalculateThePerfectNode(Transform Target)
	{
		GetCoverTransformInRange(Target);
		UpdateAvailableCover(Target);
		return GetPerfectCover(coverTransforms);
	}


	private CoverNode GetPerfectCover(HashSet<CoverTransform> coverTransforms)
	{
		if (coverTransforms?.Count == 0) return null;


		//Array.Sort(coverTransforms.ToArray(), new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });
		var SortedCoverTransforms = coverTransforms.OrderByDescending(cover => cover.height)
							.OrderBy(cover => cover.type).ToArray();



		HashSet<CoverNode> bestNodesOfTtransforms = new HashSet<CoverNode>();
		foreach (CoverTransform cover in SortedCoverTransforms)
		{
			bestNodesOfTtransforms.Add(cover.BestCoverAvailable);
			if (cover.BestCoverAvailable != null)
			{
				Instantiate(AiAgent.LocomotionSystem.ActiveFloor.prefab, cover.BestCoverAvailable.node.LocalCoord + Vector3.up * 0.5f, Quaternion.identity);

			}

		}





		CoverNode[] BestAvailableCovers = (from cover in bestNodesOfTtransforms
						   where cover != null && cover.Available && cover.InMovementRange
						   select cover).ToArray();

		//Array.Sort(BestAvailableCovers, new CoverType[3] { CoverType.Thick, CoverType.Destructable, CoverType.Small });


		BestAvailableCovers = BestAvailableCovers.OrderByDescending(cover => cover.Value)
							.OrderBy(cover => cover.DistanceToPlayer)
							.OrderByDescending(cover => cover.CoverTransform.type).ToArray();

		// retrurn 
		return BestAvailableCovers[0];

	}

	void GetCoverTransformInRange(Transform Target)
	{

		int Hits = Physics.OverlapSphereNonAlloc(AiAgent.agent.transform.position, 10, Colliders, CoversLayers); // 10 is radius of vision

		for (int i = 0; i < Hits; i++)
		{
			if (NavMesh.SamplePosition(Colliders[i].transform.position - (Target.position - Colliders[i].transform.position).normalized, out NavMeshHit hit, 2f, AiAgent.agent.areaMask))
			{
				if (NavMesh.FindClosestEdge(hit.position, out hit, AiAgent.agent.areaMask))
				{
					// cover first Cover than create all potential Cover aroyn the CoverTransform
					Node nodeCov = AiAgent.LocomotionSystem.ActiveFloor.grid.GetNode(hit.position);
					CoverTransform obj = new CoverTransform(nodeCov.LocalCoord, nodeCov.X, nodeCov.Y, nodeCov.grid, Colliders[i].transform);
					CoverNode newCover = new CoverNode(nodeCov, obj);
					obj.CreatePotentialCover(newCover, null);
					coverTransforms.Add(obj);
				}
			}
		}

	}





	public void UpdateAvailableCover(Transform Target)
	{
		foreach (CoverTransform CoverPos in coverTransforms)
		{

			Vector3 dir1 = (Target.position - CoverPos.CoverPosition.position).normalized + Vector3.up * 0;

			Vector3 dir2 = Vector3.zero;
			if (CoverPos.CoverPosition.localScale.x <= 0.4f)
			{
				// dir2 is the horizental line of the object its fixed becasue the cover does not have so much width
				Vector3 left = CoverPos.CoverPosition.position - new Vector3(CoverPos.CoverPosition.localScale.z / 2, 0, 0);
				Vector3 right = CoverPos.CoverPosition.position + new Vector3(CoverPos.CoverPosition.localScale.z / 2, 0, 0);
				dir2 = left - right;
				dir2 = Quaternion.Euler(0, Mathf.Abs(CoverPos.CoverPosition.eulerAngles.y % 360), 0) * dir2;
			}
			else
			{
				dir2 = Quaternion.Euler(0, 360 - Vector3.Angle(dir2, dir1), 0) * dir1;

			}
			//Debug.Log($"{Vector3.Angle(dir2, dir1)}");


			//Debug.Log($" {CoverPos.name} {CoverPos}");
			foreach (CoverNode coverNode in CoverPos.CoverList)
			{

				if (CoverPos.CoverPosition.localScale.x <= 0.4f)
				{
					float playerDot = Vector3.Dot(dir2, dir1);
					if (playerDot < 0)
					{
						dir2 *= -1;
						//Debug.Log($" player on the left");
					}
					else
					{
						//Debug.Log($" player on the right");
					}

				}


				Vector3 dir3 = (coverNode.node.LocalCoord - CoverPos.CoverPosition.position).normalized;
				dir3.y = dir2.y;
				float dot = Vector3.Dot(dir2, dir3);

				if (dot > 0 && dot <= 0.01f)
					dot *= -1;
				else if (dot < 0 && dot >= -0.01f)
					dot *= -1;
				if (dot < 0)
				{
					//Debug.Log($" dot{coverNode.node} {dot} greed");
					coverNode.CalculateDistanceToPlayer(AiAgent.Target);
					coverNode.Available = true;

				}
				else
				{
					coverNode.Available = false;

					//Debug.Log($" dot{coverNode.node} {dot} red");
				}

				//GameObject.Instantiate(coverNode.node.grid.floor.prefab, coverNode.node.LocalCoord + Vector3.up, Quaternion.identity);
			}
			CoverPos.UpdateBestCover();
		}
	}



}
