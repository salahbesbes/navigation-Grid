using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "AP_Consideration", menuName = "UtilityAI/Considerations/AP Consideration")]
	public class Consideration_ActionPoint : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{

			float actionPoint = 1;
			float percent = actionPoint / npc.stats.AvailableActionPoint;

			percent = Mathf.Clamp01(percent);

			score = RoundFloat(Responsecurve.Evaluate(percent), 2);

			Debug.Log($" AP {score}");
			return score;
		}
	}
}