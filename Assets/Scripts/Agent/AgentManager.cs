using UnityEngine;
using UnityEngine.AI;
using UtilityAI;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
public class AgentManager : MonoBehaviour
{
	public System_Movement LocomotionSystem;
	public System_Cover coverSystem;
	public LineOfSight_System lineOfSight;
	public NavMeshAgent agent;
	public Transform Target;
	public System_Brain brain;
	public Gun weapon;
	public Stats stats;




	internal void OnFinishedAction()
	{
	}

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		stats = GetComponent<Stats>();
		LocomotionSystem.awake(this);
		coverSystem?.awake(this);
		lineOfSight?.awake(this);
		brain?.awake(this);
		weapon?.awake(this);

	}
	private void Start()
	{
		LocomotionSystem.start();
		coverSystem.start(Target);
	}


	private void Update()
	{

		LocomotionSystem.update();
		//Debug.Log($"{lineOfSight?.Target?.gameobject?.name}");

	}

}
