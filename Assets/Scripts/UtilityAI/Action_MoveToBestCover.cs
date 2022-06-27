using GridNameSpace;
using UnityEngine;



namespace TL.UtilityAI.Actions
{
	[CreateAssetMenu(fileName = "MoveToCover", menuName = "UtilityAI/Actions/MoveToCover")]
	public class Action_MoveToBestCover : Action
	{
		public override void Execute(AgentManager npc)
		{
			if (npc.agent.name == "player") return;
			Debug.Log("I'm Moving to best cover Spot");

			npc.coverSystem.AvailableCover.Clear();
			npc.coverSystem.CreateAllPossibleCoverInRangeOfVision(10);
			npc.coverSystem.GetAllCoverInRangeOfMovementForAllTargets(npc.Targests);

			CoverDetails bestCover = npc.coverSystem.GetThePrefectSpotForShooting();



			if (bestCover.CoverSpot != null)
			{
				//Debug.Log($"perfect node {perfectCover?.node}");
				Instantiate(npc.LocomotionSystem.ActiveFloor.prefab, bestCover.CoverSpot.node.LocalCoord + Vector3.up, Quaternion.identity).GetComponent<Renderer>().material.color = Color.yellow;
				npc.LocomotionSystem.StartMoving(bestCover.CoverSpot.node);

			}
			else
			{

				Debug.Log($"cant fint best cover");
			}
		}
	}
}