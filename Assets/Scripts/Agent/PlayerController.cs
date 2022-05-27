using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
	NavMeshAgent m_Agent;
	RaycastHit Hit;
	[SerializeField] private Camera cam;

	public void Start()
	{
		m_Agent = GetComponent<NavMeshAgent>();
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			var ray = cam.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out Hit))
			{
				m_Agent.destination = Hit.point;
			}
		}
	}
}
