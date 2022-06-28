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

			npc.coverSystem.SetBestTarget(npc.coverSystem.GetPerfectTarget());
			npc.LocomotionSystem.StartMoving(npc.coverSystem.BestTarget.TargetedBy.someNodePosition);
		}
	}
}