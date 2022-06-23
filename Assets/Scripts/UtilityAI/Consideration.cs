using UnityEngine;



namespace TL.UtilityAI
{
	public abstract class Consideration : ScriptableObject
	{
		public string Name;
		[SerializeField] protected AnimationCurve Responsecurve;

		private float _score;
		[SerializeField]
		public float score
		{
			get { return _score; }
			set
			{
				this._score = Mathf.Clamp01(value);
			}
		}

		public virtual void Awake()
		{
			score = 0;
		}

		public abstract float ScoreConsideration(AgentManager agent);

		protected float RoundFloat(float value, int nb)
		{
			float power = Mathf.Pow(10, nb);
			return Mathf.Round(value * power) * (1 / power);
		}
	}
}


