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
		if (AiAgent.agent.name == "player") return;
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
		if (BestAvailableCovers.Length == 0) return null;
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
					Node nodeCov = AiAgent.LocomotionSystem.ActiveFloor.grid.GetSafeNode(hit.position);
					//Instantiate(AiAgent.LocomotionSystem.ActiveFloor.prefab, nodeCov.LocalCoord, Quaternion.identity);
					CoverTransform obj = new CoverTransform(nodeCov.grid, Colliders[i].transform);
					CoverNode newCover = new CoverNode(nodeCov, obj);
					obj.CreatePotentialCover();
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

	private void OnDrawGizmos()
	{
		if (coverTransforms != null)
		{
			Gizmos.color = Color.yellow;
			foreach (var item in coverTransforms)
			{
				float width, depth;

				if (item.CoverPosition.rotation.eulerAngles.y % 180 == 0)
				{
					width = item.CoverPosition.localScale.x;
					depth = item.CoverPosition.localScale.z;
				}
				else
				{
					width = item.CoverPosition.localScale.z;
					depth = item.CoverPosition.localScale.x;
				}


				Vector3 bottomLeftPoint = new Vector3(item.CoverPosition.position.x - width / 2, item.CoverPosition.position.y, item.CoverPosition.position.z - depth / 2);
				Vector3 TopLeftPoint = new Vector3(item.CoverPosition.position.x - width / 2, item.CoverPosition.position.y, item.CoverPosition.position.z + depth / 2);

				Vector3 TopRightPoint = new Vector3(TopLeftPoint.x + width, TopLeftPoint.y, TopLeftPoint.z);
				Vector3 bottomRightPoint = new Vector3(bottomLeftPoint.x + width, bottomLeftPoint.y, bottomLeftPoint.z);
				Gizmos.color = Color.red;


				float offset;

				if (width < 0.5f || depth < 0.5f)
				{
					offset = 0.2f;
				}
				else
				{
					offset = 0.5f;
				}

				Gizmos.DrawSphere(bottomLeftPoint + new Vector3(-offset, 0, 0), 0.05f);
				Gizmos.DrawSphere(bottomLeftPoint + new Vector3(0, 0, -offset), 0.05f);
				Gizmos.DrawSphere(TopLeftPoint + new Vector3(-offset, 0, 0), 0.05f);
				Gizmos.DrawSphere(TopLeftPoint + new Vector3(0, 0, offset), 0.05f);
				Gizmos.DrawSphere(TopRightPoint + new Vector3(offset, 0, 0), 0.05f);
				Gizmos.DrawSphere(TopRightPoint + new Vector3(0, 0, offset), 0.05f);
				Gizmos.DrawSphere(bottomRightPoint + new Vector3(offset, 0, 0), 0.05f);
				Gizmos.DrawSphere(bottomRightPoint + new Vector3(0, 0, -offset), 0.05f);


				float tmp = TopLeftPoint.z - bottomLeftPoint.z;
				while (tmp > 1)
				{
					Gizmos.color = Color.black;
					float Zpos = bottomLeftPoint.z + tmp - 1;
					Zpos += offset;

					Vector3 CoverPos = new Vector3(bottomLeftPoint.x - offset, bottomLeftPoint.y, Zpos);
					Gizmos.DrawSphere(CoverPos, 0.05f);
					Gizmos.color = Color.white;
					Vector3 oppositCver = new Vector3(bottomLeftPoint.x + offset + width, bottomLeftPoint.y, Zpos);
					Gizmos.DrawSphere(oppositCver, 0.05f);


					tmp -= 1;
				}


				tmp = bottomRightPoint.x - bottomLeftPoint.x;

				while (tmp > 1)
				{

					Gizmos.color = Color.green;
					float Xpos = bottomLeftPoint.x + tmp - 1;
					Vector3 CoverPos = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z - offset);
					Gizmos.DrawSphere(CoverPos, 0.05f);

					Gizmos.color = Color.yellow;
					Vector3 oppositCver = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z + offset + depth);
					Gizmos.DrawSphere(oppositCver, 0.05f);


					tmp -= 1;
				}

			}
			/*
			 foreach (var item in coverTransforms)
				{
					float width, depth;
					if (item.CoverPosition.rotation.eulerAngles.y % 180 == 0)
					{
						width = item.CoverPosition.localScale.x;
						depth = item.CoverPosition.localScale.z;

						Vector3 bottomLeftPoint = new Vector3(item.CoverPosition.position.x - width / 2, item.CoverPosition.position.y, item.CoverPosition.position.z - depth / 2);
						Vector3 TopLeftPoint = new Vector3(item.CoverPosition.position.x - width / 2, item.CoverPosition.position.y, item.CoverPosition.position.z + depth / 2);

						Vector3 TopRightPoint = new Vector3(TopLeftPoint.x + width, TopLeftPoint.y, TopLeftPoint.z);
						Vector3 bottomRightPoint = new Vector3(bottomLeftPoint.x + width, bottomLeftPoint.y, bottomLeftPoint.z);
						Gizmos.color = Color.yellow;
						Gizmos.DrawSphere(bottomLeftPoint, 0.05f);
						Gizmos.color = Color.red;
						Gizmos.DrawSphere(TopLeftPoint, 0.05f);

						Gizmos.color = Color.cyan;
						Gizmos.DrawSphere(TopRightPoint, 0.05f);
						Gizmos.color = Color.white;
						Gizmos.DrawSphere(bottomRightPoint, 0.05f);



						float tmp = TopLeftPoint.z - bottomLeftPoint.z;

						while (tmp > 1)
						{

							float Zpos = bottomLeftPoint.z + tmp - 1;

							Vector3 CoverPos = new Vector3(bottomLeftPoint.x, bottomLeftPoint.y, Zpos);
							Gizmos.color = Color.green;
							Gizmos.DrawSphere(CoverPos, 0.05f);

							Gizmos.color = Color.black;
							Vector3 oppositCver = new Vector3(bottomLeftPoint.x + width, bottomLeftPoint.y, Zpos);
							Gizmos.DrawSphere(oppositCver, 0.05f);
							tmp -= 1;
						}


						tmp = bottomRightPoint.x - bottomLeftPoint.x;

						while (tmp > 1)
						{

							float Xpos = bottomLeftPoint.x + tmp - 1;

							Vector3 CoverPos = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z);
							Gizmos.color = Color.green;
							Gizmos.DrawSphere(CoverPos, 0.05f);

							Gizmos.color = Color.black;
							Vector3 oppositCver = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z + depth);
							Gizmos.DrawSphere(oppositCver, 0.05f);
							tmp -= 1;
						}
					}
					else
					{

						width = item.CoverPosition.localScale.z;
						depth = item.CoverPosition.localScale.x;

						Vector3 bottomLeftPoint = new Vector3(item.CoverPosition.position.x - width / 2, item.CoverPosition.position.y, item.CoverPosition.position.z - depth / 2);
						Vector3 TopLeftPoint = new Vector3(item.CoverPosition.position.x - width / 2, item.CoverPosition.position.y, item.CoverPosition.position.z + depth / 2);

						Vector3 TopRightPoint = new Vector3(TopLeftPoint.x + width, TopLeftPoint.y, TopLeftPoint.z);
						Vector3 bottomRightPoint = new Vector3(bottomLeftPoint.x + width, bottomLeftPoint.y, bottomLeftPoint.z);
						Gizmos.color = Color.yellow;
						Gizmos.DrawSphere(bottomLeftPoint, 0.05f);
						Gizmos.color = Color.red;
						Gizmos.DrawSphere(TopLeftPoint, 0.05f);

						Gizmos.color = Color.cyan;
						Gizmos.DrawSphere(TopRightPoint, 0.05f);
						Gizmos.color = Color.white;
						Gizmos.DrawSphere(bottomRightPoint, 0.05f);



						float tmp = bottomRightPoint.x - bottomLeftPoint.x;

						while (tmp > 1)
						{

							float Xpos = bottomLeftPoint.x + tmp - 1;

							Vector3 CoverPos = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z);
							Gizmos.color = Color.green;
							Gizmos.DrawSphere(CoverPos, 0.05f);

							Gizmos.color = Color.black;
							Vector3 oppositCver = new Vector3(Xpos, bottomLeftPoint.y, bottomLeftPoint.z + depth);
							Gizmos.DrawSphere(oppositCver, 0.05f);
							tmp -= 1;
						}

					}









				}

			 */

			if (AiAgent?.LocomotionSystem?.ActiveFloor?.grid == null) return;




			for (int i = 0; i < AiAgent.LocomotionSystem.ActiveFloor.grid.height; i++)
			{
				for (int j = 0; j < AiAgent.LocomotionSystem.ActiveFloor.grid.width; j++)
				{
					//Gizmos.DrawSphere(grid.nodes[i, j].LocalCoord + Vector3.up * 0.2f, 0.45f);
				}
			}
		}
	}
}
