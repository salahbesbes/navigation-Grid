using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "DamageConsideration", menuName = "UtilityAI/Considerations/Damage Consideration")]
	public class Consideration_Damage : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{

			AgentManager TargetManager = npc.coverSystem.GetBestTarget();


			float percent = 1 - ((TargetManager.stats.Health - npc.weapon.Damage) / TargetManager.stats.Health);
			Debug.Log($"percent {percent    }");
			percent = Mathf.Clamp01(percent);
			score = RoundFloat(Responsecurve.Evaluate(percent), 2);

			return score;
		}
	}
}