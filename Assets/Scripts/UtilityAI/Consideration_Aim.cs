using GridNameSpace;
using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "AimConsideration", menuName = "UtilityAI/Considerations/Aim Consideration")]
	public class Consideration_Aim : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{

			CoverNode myCover = npc.coverSystem.GetUnitCover(npc.transform);
			if (myCover == null)
			{
				Debug.Log($" i have no cover return 1 ");
				return 1;
			}


			CoverNode cover = npc.coverSystem.GetPerfectCoverSpotForShooting();
			float myCoverAim = npc.coverSystem.CalculateAimPercent(npc, npc.Target, myCover);
			float bestCoverAim = npc.coverSystem.CalculateAimPercent(npc, npc.Target, cover);

			float percent = myCoverAim / bestCoverAim;

			score = RoundFloat(Responsecurve.Evaluate(percent), 2);
			Debug.Log($" bestCoverAim  {bestCoverAim}, myCoverAim {myCoverAim}, percent {percent} score is {score}");

			return score;
		}
	}
}