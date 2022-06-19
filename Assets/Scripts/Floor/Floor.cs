using GridNameSpace;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Floor : MonoBehaviour
{

	[Tooltip("By changing the scale of the platform we change the Grid Size")]
	public LayerMask NodelinkLayer;
	public LayerMask ObstacleLayer;
	public LayerMask PortalLayer;

	public FloorGrid grid;

	public Transform parentCanvas;
	public GameObject prefab;
	[HideInInspector]
	public List<NodeLink> nodeLinks = new List<NodeLink>();
	private float X = 2;
	private float Y = 2;
	public List<Portal> Portals = new List<Portal>();
	void Awake()
	{
		X = transform.localScale.x;
		Y = transform.localScale.z;
		grid = new FloorGrid(Y, X, this, 1);
		//Debug.Log($"we init grid of {transform.name}");

		//foreach (Node node in grid.nodes)
		//{
		//	GameObject newobj = Instantiate(prefab, node.WordCoord, Quaternion.Euler(new Vector3(90, 0, 0)), parentCanvas);
		//	newobj.transform.GetChild(0).GetComponent<Text>().text = $"{node.X},{node.Y}";

		//}

		//Debug.Log($"{grid.nodes.Length}");
		UpdateNodes();
		foreach (var item in Portals)
		{
			//Debug.Log($" enter Portal {item.Enter} exit {item.Exit}");
		}


	}

	private void OnValidate()
	{
		//grid = new FloorGrid(X, Y, this, 1);
		//CheckForLinks();
	}

	//public void CheckForObstacles()
	//{
	//	for (int i = 0; i < grid.height; i++)
	//	{
	//		for (int j = 0; j < grid.width; j++)
	//		{
	//			Node curentNode = grid.nodes[i, j];
	//			Collider[] hits = Physics.OverlapSphere(curentNode.LocalCoord + Vector3.up * 0.2f, 0.2f, ObstacleLayer);
	//			if (hits.Length > 0)
	//			{
	//				grid.nodes[i, j].isObstacle = true;
	//			}



	//		}
	//	}
	//}



	private bool LayerCheck(Collider collier, LayerMask layerToCHeck)
	{
		return ((1 << collier.gameObject.layer) & layerToCHeck.value) != 0; //true
	}
	public void UpdateNodes()
	{
		for (int i = 0; i < grid.height; i++)
		{
			for (int j = 0; j < grid.width; j++)
			{
				Node curentNode = grid.nodes[i, j];
				Collider[] hits = Physics.OverlapSphere(curentNode.LocalCoord + Vector3.up * 0.2f, 0.45f);
				Vector3 center = grid.nodes[i, j].LocalCoord + Vector3.up * 0.17f;
				Vector3 BoxSize = new Vector3(grid.nodeSize, (float)grid.nodeSize / 3, grid.nodeSize) * 0.8f;
				//Collider[] hits = Physics.OverlapBox(center, BoxSize, Quaternion.Euler(Vector3.up));

				//RaycastHit[] boxHits = Physics.BoxCastAll(center, BoxSize, transform.up, Quaternion.identity, NodelinkLayer);


				foreach (Collider hit in hits)
				{
					if (hit == null) continue;

					if (LayerCheck(hit, NodelinkLayer))
					{
						NodeLink nodeLink = hit.transform.GetComponent<NodeLink>();

						nodeLink.node = curentNode;
						nodeLink.floor = this;
						if (!nodeLinks.Contains(nodeLink))
						{
							//Debug.Log($" found nodeLink  {nodeLink.name}");
							AddNodeLink(nodeLink);
						}
					}
					else if (LayerCheck(hit, ObstacleLayer))
					{
						if (hit.CompareTag("Wall"))
						{

							Vector3 closestPoint = hit.ClosestPoint(grid.nodes[i, j].LocalCoord);

							WallDirection direction = WallDirection.Middle;

							float diffX = closestPoint.x - grid.nodes[i, j].LocalCoord.x;
							float diffY = closestPoint.z - grid.nodes[i, j].LocalCoord.z;


							// vertical wall => need to check if it is on the rightor the left
							if (diffY <= 0.0001)
							{
								if (diffX >= diffY)
								{
									direction = WallDirection.Right;
								}
								else
								{
									direction = WallDirection.Left;
								}
							}
							// horizental wall => need to chekc if it it on top or botom
							else if (diffX <= 0.0001)
							{
								if (diffX >= diffY)
								{
									direction = WallDirection.Bottom;
								}
								else
								{
									direction = WallDirection.Top;
								}
							}

							grid.nodes[i, j].NotifieNeighborsWithSomeRestriction(direction);

						}
						else // not a wall
						{

							grid.nodes[i, j].isObstacle = true;
						}
					}
					else if (LayerCheck(hit, PortalLayer))
					{
						Portal portal = hit.GetComponent<Portal>();

						if (!Portals.Contains(portal))
						{
							portal.initPortal();
							Portals.Add(portal);
						}
					}


				}
			}
		}
	}

	private void AddNodeLink(NodeLink nodeLink)
	{
		nodeLinks.Add(nodeLink);

	}

	void Update()
	{
		//if (grid == null || grid.nodes == null) return;

		UpdateNodes();
		grid.Reset();
	}



	//public async void OnValidate()
	//{
	//	if (grid == null) return;
	//	await drawGrid();
	//}



	public override string ToString()
	{
		return $" {transform.name}";
	}

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
				//Gizmos.DrawSphere(grid.nodes[i, j].LocalCoord + Vector3.up * 0.2f, 0.45f);

				//if (grid.nodes[i, j].canGoLeft == false)
				//{
				//	Gizmos.color = Color.red;
				//	Gizmos.DrawSphere(grid.nodes[i, j].LocalCoord + Vector3.up * 0.1f, 0.25f);
				//}
				//if (grid.nodes[i, j].canGoRight == false)

				//{
				//	Gizmos.color = Color.green;
				//	Gizmos.DrawSphere(grid.nodes[i, j].LocalCoord + Vector3.up * 0.3f, 0.25f);
				//}

				//if (grid.nodes[i, j].canGoTop == false)
				//{
				//	Gizmos.color = Color.white;
				//	Gizmos.DrawSphere(grid.nodes[i, j].LocalCoord + Vector3.up * 0.5f, 0.25f);
				//}
				//if (grid.nodes[i, j].canGoBottom == false)

				//{
				//	Gizmos.color = Color.black;
				//	Gizmos.DrawSphere(grid.nodes[i, j].LocalCoord + Vector3.up * 0.7f, 0.25f);
				//}




			}
		}

		return Task.Delay(500);




	}
}


public enum WallDirection
{
	Right, Left, Top, Bottom, Middle
}