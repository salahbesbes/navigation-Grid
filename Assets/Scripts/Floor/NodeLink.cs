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
}