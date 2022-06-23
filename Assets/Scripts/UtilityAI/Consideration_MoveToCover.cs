using GridNameSpace;
using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "MoveToCoverConsideration", menuName = "UtilityAI/Considerations/MoveToCover Consideration")]
	public class Consideration_MoveToCover : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{
			CoverNode myCover = npc.coverSystem.GetUnitCover(npc.transform);
			if (myCover == null) return 1;


			CoverNode cover = npc.coverSystem.GetPerfectCoverSpotForDefense();


			float percent = myCover.Value / cover.Value;

			score = RoundFloat(Responsecurve.Evaluate(percent), 2);

			Debug.Log($" best cover  {cover.Value}, my cover val {myCover.Value}, percent {percent} score is {score}");
			return score;
		}
	}
}