using UnityEngine;

namespace TL.UtilityAI.Considerations
{
	[CreateAssetMenu(fileName = "AimConsideration", menuName = "UtilityAI/Considerations/Aim Consideration")]
	public class AimConsideration : Consideration
	{
		public override float ScoreConsideration()
		{
			return 0.9f;
		}
	}
}