using GridNameSpace;
using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "AimConsideration", menuName = "UtilityAI/Considerations/Aim Consideration")]
	public class Consideration_Aim : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{


			npc.coverSystem.AvailableCover.Clear();
			npc.coverSystem.CreateAllPossibleCoverInRangeOfVision(10);
			npc.coverSystem.GetAllCoverInRangeOfMovementForAllTargets(npc.Targests);

			CoverDetails bestCover = npc.coverSystem.GetThePrefectSpotForShooting();

			//npc.coverSystem.GetBestTarget();
			CoverNode myCover = npc.coverSystem.GetMyCoverNode();


			if (myCover == null)
			{
				Debug.Log($" i have no cover return 0 ");
				return 0;
			}


			Transform bestTargetForBestCover = npc.coverSystem.BestTArgetFor(bestCover.CoverSpot);
			Transform bestTargetFormyCoverSpot = npc.coverSystem.BestTArgetFor(myCover);
			float myBestTargetAim = CoverDetails.CalculateAimPercentStatic(npc.transform, bestTargetFormyCoverSpot, myCover);
			float BestTargetAimforbestCover = CoverDetails.CalculateAimPercentStatic(npc.transform, bestTargetForBestCover, bestCover.CoverSpot);

			float percent = myBestTargetAim / BestTargetAimforbestCover;
			Debug.Log($"my aim {myBestTargetAim}  best aim {BestTargetAimforbestCover}");
			score = RoundFloat(Responsecurve.Evaluate(percent), 2);

			if (score < 0.5f)
			{
				npc.coverSystem.SetBestTarget(bestTargetForBestCover);
			}
			else
			{
				npc.coverSystem.SetBestTarget(bestTargetFormyCoverSpot);
			}
			//Debug.Log($" bestDetailForMyCover  {bestDetailForMyCover.AimPercent}, bestDetailForBestCover {bestDetailForBestCover.AimPercent}, percent {percent} score is {score}");

			return score;
		}
	}
}