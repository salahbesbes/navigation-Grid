using GridNameSpace;
using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "AimConsideration", menuName = "UtilityAI/Considerations/Aim Consideration")]
	public class Consideration_Aim : Consideration
	{
		public override float ScoreConsideration(AgentManager npc)
		{

			TargetDetail bestTarget = npc.coverSystem.GetPerfectTarget();

			// if we want to move or no we set the best cover spot
			npc.coverSystem.SetBestCoverPosition(bestTarget.TargetedBy.someNodePosition);

			TargetDetail myBestTArget = npc.coverSystem.GetPerfectTargetForMyPosition();


			//Debug.Log($"bestatrget {bestTarget.transform.name} detail val {bestTarget.Aim}");
			//Debug.Log($" My best target {myBestTArget.transform.name} detail val {myBestTArget.Aim}");

			float percent = myBestTArget.Aim / bestTarget.Aim;

			score = Responsecurve.Evaluate(RoundFloat(percent, 2));
			//Debug.Log($"percent {percent}  score {score}");



			return score;
		}
	}
}