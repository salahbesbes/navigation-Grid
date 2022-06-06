using UnityEngine;
using UnityEngine.AI;
[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
public class AgentManager : MonoBehaviour
{
	public System_Movement LocomotionSystem;
	public System_Cover coverSystem;
	public NavMeshAgent agent;
	public Transform Target;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();

		LocomotionSystem.awake(this);
		coverSystem?.awake(this);
	}
	private void Start()
	{
		LocomotionSystem.start();
		coverSystem.start(Target);
	}



	private void Update()
	{
		LocomotionSystem.updateProperties();
		LocomotionSystem.AgentInputSystem();


	}

}
