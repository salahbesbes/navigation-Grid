using GridNameSpace;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Floor : MonoBehaviour
{

	[Tooltip("By changing the scale of the platform we change the Grid Size")]
	public LayerMask NodelinkLayer;
	public LayerMask floorLayer;

	public FloorGrid grid;

	public Transform parentCanvas;
	public GameObject prefab;
	[HideInInspector]
	public List<NodeLink> nodeLinks = new List<NodeLink>();
	private int X = 2;
	private int Y = 2;

	void Start()
	{
		X = (int)transform.localScale.x;
		Y = (int)transform.localScale.z;
		grid = new FloorGrid(Y, X, this, 1);
		//Debug.Log($"we init grid of {transform.name}");

		//foreach (Node node in grid.nodes)
		//{
		//	GameObject newobj = Instantiate(prefab, node.WordCoord, Quaternion.Euler(new Vector3(90, 0, 0)), parentCanvas);
		//	newobj.transform.GetChild(0).GetComponent<Text>().text = $"{node.X},{node.Y}";

		//}

		Debug.Log($"{grid.nodes.Length}");
		foreach (var item in grid.nodes)
		{
			Debug.Log($"[{item.X},{item.Y}]");
		}
		CheckForLinks();

	}

	private void OnValidate()
	{
		//grid = new FloorGrid(X, Y, this, 1);
		//CheckForLinks();
	}



	public void CheckForLinks()
	{
		for (int i = 0; i < grid.height; i++)
		{
			for (int j = 0; j < grid.width; j++)
			{
				Node curentNode = grid.nodes[i, j];
				Collider[] hits = Physics.OverlapSphere(curentNode.LocalCoord + Vector3.up * 0.2f, 0.2f, NodelinkLayer);
				if (hits.Length > 0)
				{
					Collider firstColliderHit = hits[0];
					NodeLink nodeLink = firstColliderHit.transform.GetComponent<NodeLink>();
					nodeLink.node = curentNode;
					nodeLink.floor = this;
					if (!nodeLinks.Contains(nodeLink))
					{
						nodeLinks.Add(nodeLink);
					}
					//grid.nodes[i, j].isNodeLink = true;
					//Debug.DrawLine(curentNode.LocalCoord, curentNode.LocalCoord + Vector3.up, Color.red);
					//// get the first collider hit
					////Debug.Log($"found NodeLayer at {curentNode} with name {firstColliderHit.transform.name} in the Grid {grid.floor.name}");
					//Link link = firstColliderHit.transform.GetComponentInParent<Link>();
					//if (link != null)
					//{
					//	grid.nodes[i, j].isNodeLink = true;
					//	NodeLink nodeLink = curentNode.CreateNodeLink(link);
					//	link.AddNode(nodeLink);
					//}

				}
			}
		}
	}



	void Update()
	{
		//if (grid == null || grid.nodes == null) return;

		CheckForLinks();
		foreach (Node node in grid.nodes)
		{
			node.Reset();
		}
	}



	//public async void OnValidate()
	//{
	//	if (grid == null) return;
	//	await drawGrid();
	//}





	private async void OnDrawGizmos()
	{


		X = (int)transform.localScale.x;
		Y = (int)transform.localScale.z;
		Vector3 buttonLeft = transform.position - (Vector3.right * X) / 2 - (Vector3.forward * Y) / 2;



		buttonLeft += new Vector3(0, transform.localScale.y / 2, 0);
		Debug.DrawLine(buttonLeft, buttonLeft + Vector3.up * 2, Color.yellow);

		for (int x = 0; x < Y; x++)
		{
			Debug.DrawLine(buttonLeft + new Vector3(0, 0, x), new Vector3(X + buttonLeft.x, buttonLeft.y, (x + buttonLeft.z)), Color.black);
		}
		for (int x = 0; x < X; x++)
		{
			Debug.DrawLine(buttonLeft + new Vector3(x, 0, 0), new Vector3(x + buttonLeft.x, buttonLeft.y, (Y + buttonLeft.z)), Color.black);
		}

		if (grid == null || grid.nodes == null) return;


		await drawGrid();


	}

	private Task drawGrid()
	{



		//foreach (Node node in grid.nodes)
		//{
		//	Debug.DrawLine(node.WordCoord, node.WordCoord + Vector3.up, Color.red);
		//}


		Vector3 offset = new Vector3(0, 0.2f, 0);


		for (int i = 0; i < grid.height; i++)
		{
			for (int j = 0; j < grid.width; j++)
			{
				Gizmos.DrawSphere(grid.nodes[i, j].LocalCoord + Vector3.up * 0.2f, 0.2f);


			}
		}

		return Task.Delay(500);




	}
}
