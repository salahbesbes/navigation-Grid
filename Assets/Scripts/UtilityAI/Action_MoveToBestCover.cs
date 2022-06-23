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
			// Logic for updating everything involved with eating

			CoverNode Cover = npc.coverSystem.CalculateThePerfectDefense();

			if (Cover != null)
			{
				//Debug.Log($"perfect node {perfectCover?.node}");
				Instantiate(npc.LocomotionSystem.ActiveFloor.prefab, Cover.node.LocalCoord + Vector3.up, Quaternion.identity).GetComponent<Renderer>().material.color = Color.yellow;
				npc.LocomotionSystem.StartMoving(Cover.node);

			}
			else
			{

				Debug.Log($"cant fint best cover");
			}
		}
	}
}