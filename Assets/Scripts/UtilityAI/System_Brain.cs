using GridNameSpace;
using System.Collections.Generic;
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

		private void Start()
		{
			npc = GetComponent<AgentManager>();
			desideButton.onClick.AddListener(() => DecideBestAction());
		}

		// Update is called once per frame
		private void Update()
		{
		}

		// Loop through all the available actions Give me the highest scoring action
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
				Debug.Log($" {actionsAvailable[i]} => score {actionsAvailable[i].score}");
			}
			//Debug.Log($"   best action {actionsAvailable[nextBestActionIndex]} with score {actionsAvailable[nextBestActionIndex].score}");
			bestAction = actionsAvailable[nextBestActionIndex];
			bestAction.Execute(npc);
		}

		// Loop through all the considerations of the action Score all the considerations
		// Average the consideration scores ==> overall action score
		public float ScoreAction(Action action)
		{
			float score = 1f;
			for (int i = 0; i < action.considerations.Length; i++)
			{
				float considerationScore = action.considerations[i].ScoreConsideration(npc);
				score *= considerationScore;

				if (score == 0)
				{
					action.score = 0;
					return action.score; // No point computing further
				}
			}

			// Averaging scheme of overall score
			action.score = average(score, action.considerations.Length);

			return action.score;
		}

		private float average(float score, int NBconditions)
		{
			float originalScore = score;
			float modFactor = 1 - (1 / NBconditions);
			float makeupValue = (1 - originalScore) * modFactor;
			return originalScore + (makeupValue * originalScore);
		}

		public AgentManager SelectBestTarget(List<Transform> TargetInVision)
		{
			List<TargetInformation> targetsInformation = new List<TargetInformation>();
			foreach (Transform target in TargetInVision)
			{
				if (target.TryGetComponent(out AgentManager TargetAgentManaget))
				{
					//targetsInformation.Add(new TargetInformation(TargetAgentManaget, npc));
				}
			}

			return null;
		}
	}

	public class TargetInformation
	{
		public AgentManager Unit;
		public AgentManager TargetManager;
		public Node nodePosition;
		public float DistanceToUnit;
		public float AimPercent;

		public CoverNode Cover
		{
			get;
			set;
		}

		public bool InRangeOfWeapon
		{
			get
			{
				return IsInRangeOf(Unit);
			}
			private set { }
		}

		private bool IsInRangeOf(AgentManager unit)
		{
			Gun UnitWeapon = unit.weapon;
			float weaponRange = UnitWeapon?.MaxRange ?? 0f;

			return Vector3.Distance(unit.transform.position, TargetManager.transform.position) <= weaponRange;
		}
	}
}