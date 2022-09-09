using GridNameSpace;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class System_Movement_Agent : System_Movement
{
        private void OnDrawGizmos()
        {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 100);
                foreach (RangeNode node in NodeInRange)
                {
                        if (node == null || node.node == null) continue;
                        if (node.firstRange)
                        {
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawSphere(node.node.LocalCoord, 0.2f);
                        }
                        if (node.SecondRange)
                        {
                                Gizmos.color = Color.blue;
                                Gizmos.DrawSphere(node.node.LocalCoord, 0.2f);
                        }
                }
        }
        public override void awake(AgentManager agent)
        {
                lr = GetComponent<LineRenderer>();
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
                        bool newFloorIsAdjacent = false;
                        newFloorIsAdjacent = CheckIfNewFloorClickedIsAdjacent(newFloor);

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

                                Debug.Log($"{ActiveFloor} is adjacent new {newFloor} {newFloorIsAdjacent}");
                                if (newFloorIsAdjacent == false)
                                {

                                        newFloor.grid.GetNodeCoord(newFloor, FloorLayer, out destinationX, out destinationY);
                                        //Debug.Log($"dest [x {destinationX}, y {destinationY}]");

                                        FinalDestination = newFloor.grid.GetNode(destinationX, destinationY);
                                        if (destinationX >= 0 && destinationY >= 0)
                                        {
                                                StopCoroutine("Move");
                                                //Debug.Log($"dest [x{destinationX}, y{destinationY}]");
                                                ActiveNodeLink = ClosestNodeLinkAvailable(newFloor);

                                                if (ActiveNodeLink == null)
                                                {
                                                        Debug.Log($" we cant move their is not way to cross ");
                                                        return;
                                                }
                                                Node destination = ActiveNodeLink.node;
                                                if (CurentPositon == destination)
                                                {
                                                        CrossingToNodeLinkDestination(ActiveNodeLink, AiAgent);
                                                }
                                                else
                                                {
                                                        StartMoving(destination);
                                                }
                                        }
                                }
                                else
                                {
                                        Node ClosetsEdgeNode = ActiveFloor.grid.GetSafeNode(FinalDestination.LocalCoord);

                                        MoveBetweenTwoAdjacentPlatforms(ClosetsEdgeNode, FinalDestination);

                                }



                        }

                        else
                        {
                                //Debug.Log($"dest [x{destinationX}, y{destinationY}]");
                                StopCoroutine("Move");

                                ActiveFloor.grid.GetNodeCoord(ActiveFloor, FloorLayer, out destinationX, out destinationY);
                                //Debug.Log($"dest [x {destinationX}, y {destinationY}]");

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

        private bool CheckIfNewFloorClickedIsAdjacent(Floor newFloor)
        {
                foreach (Node node in ActiveFloor.grid.nodes)
                {
                        if (node.RemoteNodes.Any(el => el.grid.floor == newFloor))
                                return true;
                }
                return false;
        }

        private void MoveBetweenTwoAdjacentPlatforms(Node closetsEdgeNode, Node finalDestination)
        {
                List<Node> firstPart = GetPathFromTo(CurentPositon, closetsEdgeNode);
                Debug.Log($"we are clculating path  from ({CurentPositon}) to closet edge {closetsEdgeNode}   {firstPart.Count}");
                Node RemoteNodeWithSameFloorAsTeDestination = closetsEdgeNode.RemoteNodes.FirstOrDefault(el => el.grid == finalDestination.grid);

                List<Node> secondPart = GetPathFromTo(RemoteNodeWithSameFloorAsTeDestination, finalDestination);
                Debug.Log($"we are calvulating path  from ({RemoteNodeWithSameFloorAsTeDestination}) to final destination {finalDestination}  {secondPart.Count}");

                List<Node> HolePath = firstPart.Union(secondPart).ToList();

                StartCoroutine(Move(HolePath));
        }

        public override void update()
        {
                updateProperties();
                AgentInputSystem();
                NodeInRange = GetRangeOfMevement(CurentPositon, 5);

        }

}