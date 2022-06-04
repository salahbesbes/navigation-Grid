using GridNameSpace;
using UnityEngine;

public class Portal : MonoBehaviour
{
	public Node Enter;
	public Node Exit;
	public float width;
	public float depth;
	public PortalDirection Direction;
	public Floor floor;

	public void initPortal()
	{
		Enter = floor.grid.GetNode(transform);
		if (Direction == PortalDirection.Horizental)
		{
			Exit = floor.grid.GetNode(Enter.X + 1, Enter.Y);
		}
		else if (Direction == PortalDirection.Vertical)
		{
			Exit = floor.grid.GetNode(Enter.X, Enter.Y + 1);

		}
		if (Exit == null)
		{
			Debug.Log($"Portal Near Edge ");
		}
	}
}
