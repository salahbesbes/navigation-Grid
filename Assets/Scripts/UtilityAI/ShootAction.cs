using UnityEngine;



namespace TL.UtilityAI.Actions
{
	[CreateAssetMenu(fileName = "Shoot", menuName = "UtilityAI/Actions/Shoot")]
	public class ShootAction : Action
	{
		public override void Execute(AgentManager npc)
		{
			Debug.Log("I Have Shot The enemy");
			// Logic for updating everything involved with eating

			npc.OnFinishedAction();
		}
	}
}