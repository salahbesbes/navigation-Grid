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

			npc.StartShootCoroutine(npc.coverSystem.BestTarget);
			npc.OnFinishedAction();
		}
	}
}