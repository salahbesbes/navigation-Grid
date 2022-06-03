using GridNameSpace;
using UnityEngine;

public class NodeLink : MonoBehaviour
{
	public NodeLink Destiation;
	public Link link;
	[HideInInspector]
	public Node node;
	[HideInInspector]
	public Floor floor { get; set; }

	public override string ToString()
	{
		return $"this is NodeLink with the Link {link.name} in the Grid {node.grid}";
	}

	public delegate void StartCrossing(NodeLink player);
	public StartCrossing OnStartCrossing;

	public delegate void ReachDestination(NodeLink player);
	public StartCrossing OnReachDestination;


	public void AddObservable(MoveController unit)
	{
		OnStartCrossing += unit.CrossingToNodeLinkDestination;
		OnReachDestination += unit.WhenReachNodeLinkDestination;
	}


	private void OnTriggerEnter(Collider other)
	{

		if (other.transform.CompareTag("IgnoreFromTrigger")) return;

		MoveController unit = other.GetComponent<MoveController>();
		if (unit == null) return;
		if (unit.ActiveNodeLink != this) return;
		// if unit is nor crossing and both floor is the same dont trigger 
		if (unit.FinalDestination.grid.floor == floor && unit.crossing == null) return;



		// if crossing != null and the floores are different than mean unit is Crossing and he want to trigger reachDestination
		if (unit.crossing == null)
		{
			OnStartCrossing.Invoke(this);
		}
		else
		{
			OnReachDestination.Invoke(this);
		}
	}


	internal void RemoveUnitObservable(MoveController unit)
	{
		OnStartCrossing += unit.CrossingToNodeLinkDestination;
		OnReachDestination += unit.WhenReachNodeLinkDestination;
	}
}