using GridNameSpace;
using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "MoveToCoverConsideration", menuName = "UtilityAI/Considerations/MoveToCover Consideration")]
	public class Consideration_MoveToCover : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{

			CoverNode myCover = npc.coverSystem.GetCoverNode(npc.LocomotionSystem.CurentPositon);
			TargetDetail bestTarget = npc.coverSystem.GetPerfectTarget();

			// if we want to move or no we set the best cover spot
			npc.coverSystem.SetBestCoverPosition(bestTarget.TargetedBy.someNodePosition);

			if (myCover == null)
			{
				score = 1;
				return 1;
			}
			TargetDetail myBestTArget = npc.coverSystem.GetPerfectTargetForMyPosition();



			float percent = myBestTArget.TargetedBy.Value / bestTarget.TargetedBy.Value;

			score = Responsecurve.Evaluate(RoundFloat(percent, 2));
			//Debug.Log($" best cover  {bestCover.Value}, my cover val {myCover.Value}, percent {percent} score is {score}");
			return score;
		}
	}
}