using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "Ammo_Consideration", menuName = "UtilityAI/Considerations/Ammo Consideration")]
	public class Consideration_Ammo : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{

			float actionAmmo = 1;
			float percent = actionAmmo / npc.stats.AvailableAmmo;

			percent = Mathf.Clamp01(percent);

			score = RoundFloat(Responsecurve.Evaluate(percent), 2);

			Debug.Log($" ammos score {score}");
			return score;
		}
	}
}