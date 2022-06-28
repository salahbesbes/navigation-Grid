using GridNameSpace;
using UnityEngine;



namespace TL.UtilityAI.Actions
{
	[CreateAssetMenu(fileName = "Shoot", menuName = "UtilityAI/Actions/Shoot")]
	public class Action_Shoot : Action
	{
		public override void Execute(AgentManager npc)
		{
			Debug.Log("I Have Shot The enemy");
			// Logic for updating everything involved with eating
			TargetDetail target;
			if (score >= 0.5)
			{
				target = npc.coverSystem.GetPerfectTargetForMyPosition();
				npc.coverSystem.SetBestTarget(target);
			}
			else
			{
				target = npc.coverSystem.GetPerfectTarget();
				npc.coverSystem.SetBestTarget(target);
			}

			npc.StartShootCoroutine(npc.coverSystem.BestTarget);
			npc.OnFinishedAction();
		}
	}
}