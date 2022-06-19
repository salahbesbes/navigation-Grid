using TL.UtilityAI;
using UnityEngine;
using UnityEngine.UI;

namespace UtilityAI
{
	public class System_Brain : MonoBehaviour
	{
		public Action bestAction { get; set; }
		private AgentManager npc;
		public Button desideButton;
		public Action[] actionsAvailable;
		// Start is called before the first frame update


		public void awake(AgentManager Npc)
		{
			npc = Npc;
		}
		void Start()
		{
			npc = GetComponent<AgentManager>();
			desideButton.onClick.AddListener(() => DecideBestAction());

		}

		// Update is called once per frame
		void Update()
		{

		}

		// Loop through all the available actions 
		// Give me the highest scoring action
		public void DecideBestAction()
		{
			float score = 0f;
			int nextBestActionIndex = 0;
			for (int i = 0; i < actionsAvailable.Length; i++)
			{
				if (ScoreAction(actionsAvailable[i]) > score)
				{
					nextBestActionIndex = i;
					score = actionsAvailable[i].score;
				}
			}

			bestAction = actionsAvailable[nextBestActionIndex];
			bestAction.Execute(npc);
		}

		// Loop through all the considerations of the action
		// Score all the considerations
		// Average the consideration scores ==> overall action score
		public float ScoreAction(Action action)
		{
			float score = 1f;
			for (int i = 0; i < action.considerations.Length; i++)
			{
				float considerationScore = action.considerations[i].ScoreConsideration();
				score *= considerationScore;

				if (score == 0)
				{
					action.score = 0;
					return action.score; // No point computing further
				}
			}

			// Averaging scheme of overall score
			float originalScore = score;
			float modFactor = 1 - (1 / action.considerations.Length);
			float makeupValue = (1 - originalScore) * modFactor;
			action.score = originalScore + (makeupValue * originalScore);

			return action.score;
		}




	}
}





