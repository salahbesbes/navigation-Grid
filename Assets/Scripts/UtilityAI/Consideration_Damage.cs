using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "DamageConsideration", menuName = "UtilityAI/Considerations/Damage Consideration")]
	public class Consideration_Damage : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{
			score = 0.1f;
			return score;
		}
	}
}