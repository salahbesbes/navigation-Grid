using GridNameSpace;
using UnityEngine;
public class System_Movement_Agent : System_Movement
{
	public override void awake(AgentManager agent)
	{
		//lr = GetComponent<LineRenderer>();
		mAnimator = GetComponent<Animator>();
		AiAgent = agent;
	}

	public override void start()
	{
		foreach (var nodeLink in ActiveFloor.nodeLinks)
		{
			nodeLink.AddObservable(this);
			nodeLink.Destiation.AddObservable(this);
		}
	}
	public override void AgentInputSystem()
	{
		if (Input.GetMouseButtonDown(0) && AiAgent.agent.name == "player")
		{


			Floor newFloor = GetFloorPressed();
			if (newFloor != ActiveFloor)
			{
				pressedOnDifferentFloor = true;
			}
			else
			{
				pressedOnDifferentFloor = false;
			}

			if (pressedOnDifferentFloor)
			{
				newFloor.grid.GetNodeCoord(newFloor, FloorLayer, out destinationX, out destinationY);
				FinalDestination = newFloor.grid.GetNode(destinationX, destinationY);
				if (destinationX >= 0 && destinationY >= 0)
				{
					StopCoroutine("Move");
					//Debug.Log($"dest [x{destinationX}, y{destinationY}]");
					ActiveNodeLink = ClosestNodeLinkAvailable(newFloor);
					Node destination = ActiveNodeLink.node;
					if (curentPositon == destination)
					{
						CrossingToNodeLinkDestination(ActiveNodeLink, AiAgent);
					}
					else
					{
						StartMoving(destination);
					}
				}
				else
				{
					Debug.Log($"dest [x{destinationX}, y{destinationY}]");
				}



			}
			else
			{
				//Debug.Log($"dest [x{destinationX}, y{destinationY}]");
				StopCoroutine("Move");

				ActiveFloor.grid.GetNodeCoord(ActiveFloor, FloorLayer, out destinationX, out destinationY);
				if (destinationX >= 0 && destinationY >= 0)
				{
					if (ActiveFloor.grid.GetNode(destinationX, destinationY).isObstacle)
					{
						Debug.Log($"you clicked on obstacle");
						return;
					}
				}
				FinalDestination = ActiveFloor.grid.GetNode(destinationX, destinationY);

				StartMoving(FinalDestination);
			}

		}
	}

	public override void update()
	{
		updateProperties();
		AgentInputSystem();
	}
}
