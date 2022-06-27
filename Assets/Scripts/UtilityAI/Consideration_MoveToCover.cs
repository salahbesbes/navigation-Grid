using GridNameSpace;
using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "MoveToCoverConsideration", menuName = "UtilityAI/Considerations/MoveToCover Consideration")]
	public class Consideration_MoveToCover : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{


			npc.coverSystem.AvailableCover.Clear();
			npc.coverSystem.CreateAllPossibleCoverInRangeOfVision(10);
			npc.coverSystem.GetAllCoverInRangeOfMovementForAllTargets(npc.Targests);

			CoverDetails bestCover = npc.coverSystem.GetThePrefectSpotForShooting();
			Transform bestTArget = bestCover.Target;


			CoverNode myCover = npc.coverSystem.GetMyCoverNode();

			if (myCover == null)
			{
				Debug.Log($"mycover  is null");
				return 1;
			}

			CoverDetails myCoverDetail = new CoverDetails(myCover, npc.transform, bestTArget);


			float percent = 0;
			if (myCover.node == bestCover.CoverSpot.node)
			{
				Debug.Log($"my cover is the best cover ");
			}
			if (myCover.node == bestCover.CoverSpot.node) return 0;


			if (bestCover.Value >= myCoverDetail.Value)
			{
				percent = myCoverDetail.Value / bestCover.Value;
			}
			else
			{
				percent = 1 - (bestCover.Value / myCoverDetail.Value);
			}

			//Debug.Log($"percent {percent}");
			score = RoundFloat(Responsecurve.Evaluate(percent), 2);

			//Debug.Log($" best cover  {bestCover.Value}, my cover val {myCover.Value}, percent {percent} score is {score}");
			return score;
		}
	}
}