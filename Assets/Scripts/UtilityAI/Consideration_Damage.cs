using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "DamageConsideration", menuName = "UtilityAI/Considerations/Damage Consideration")]
	public class Consideration_Damage : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{
			AgentManager Target = npc.Target.GetComponent<AgentManager>();

			float percent = 1 - ((Target.stats.Health - npc.weapon.Damage) / Target.stats.Health);

			percent = Mathf.Clamp01(percent);

			score = RoundFloat(Responsecurve.Evaluate(percent), 2);

			Debug.Log($" dmage score  {score}");
			return score;
		}
	}
}