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

        public delegate void StartCrossing(NodeLink nodeLink, AgentManager player);
        public StartCrossing OnStartCrossing;

        public delegate void ReachDestination(NodeLink nodeLink, AgentManager player);
        public StartCrossing OnReachDestination;


        public void AddObservable(System_Movement unit)
        {
                OnStartCrossing += unit.CrossingToNodeLinkDestination;
                OnReachDestination += unit.WhenReachNodeLinkDestination;
        }


        private void OnTriggerEnter(Collider other)
        {

                if (other.transform.CompareTag("IgnoreFromTrigger")) return;

                AgentManager unit = other.GetComponent<AgentManager>();
                if (unit == null) return;
                if (unit.LocomotionSystem.ActiveNodeLink != this) return;
                // if unit is nor crossing and both floor is the same dont trigger 
                if (unit.LocomotionSystem.FinalDestination?.grid.floor == floor && unit.LocomotionSystem.crossing == null) return;



                // if crossing != null and the floores are different than mean unit is Crossing and he want to trigger reachDestination
                if (unit.LocomotionSystem.crossing == null)
                {
                        OnStartCrossing.Invoke(this, unit);
                }
                else
                {
                        OnReachDestination.Invoke(this, unit);
                }
        }


        internal void RemoveUnitObservable(System_Movement unit)
        {
                OnStartCrossing -= unit.CrossingToNodeLinkDestination;
                OnReachDestination -= unit.WhenReachNodeLinkDestination;
        }
}