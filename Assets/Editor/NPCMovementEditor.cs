using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(System_Movement_NPC))]
public class NPCMovementEditor : Editor
{
	private Collider[] Colliders = new Collider[10];

	private void Hide()
	{
		System_Movement_NPC movement = (System_Movement_NPC)target;

		if (movement == null || movement.AiAgent == null)
		{
			return;
		}
		Transform bestTarget = movement.AiAgent.Targests[0];

		int Hits = Physics.OverlapSphereNonAlloc(movement.AiAgent.transform.position, 10, Colliders, movement.AiAgent.coverSystem.CoversLayers);

		for (int i = 0; i < Hits; i++)
		{
			if (NavMesh.SamplePosition(Colliders[i].transform.position - (bestTarget.position - Colliders[i].transform.position).normalized, out NavMeshHit hit, 2f, movement.AiAgent.agent.areaMask))
			{


				if (NavMesh.FindClosestEdge(hit.position, out hit, movement.AiAgent.agent.areaMask))
				{
					Handles.color = Color.red;
					Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit.position, Quaternion.identity, 0.25f, EventType.Repaint);
					Handles.Label(hit.position, $"{i} (hit1) no edge found");
				}
			}
		}
	}

	// public List<Node> GetPossibleNeighbors(Node node)
	//{
	//	foreach (var item in collection)
	//	{

	//	}
	//}


	public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal, Transform unit)
	{
		if (!angleIsGlobal)
		{
			angleInDegrees += unit.transform.eulerAngles.y;
		}
		return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}

	public void OnSceneGUI()
	{
		return;
		System_Movement_NPC movement = (System_Movement_NPC)target;

		Transform bestTarget = movement.AiAgent.Targests[0];

		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i] = null;
		}

		int Hits = Physics.OverlapSphereNonAlloc(movement.AiAgent.transform.position, 10, Colliders, movement.AiAgent.coverSystem.CoversLayers);


		int stepCount = Mathf.RoundToInt(360 * 0.3f);
		float stepAngleSize = 360 / (float)stepCount;

		Handles.color = Color.white;
		Handles.DrawWireArc(movement.transform.position, Vector3.up, Vector3.forward, 360, 12);
		Vector3 viewAngleA = DirFromAngle(-360 / 2, false, movement.transform);
		Vector3 viewAngleB = DirFromAngle(360 / 2, false, movement.transform);

		Handles.DrawLine(movement.transform.position, movement.transform.position + viewAngleA * 12);
		Handles.DrawLine(movement.transform.position, movement.transform.position + viewAngleB * 12);

		Handles.color = Color.red;
		foreach (Collider visibleTarget in Colliders)
		{
			if (visibleTarget == null) continue;
			Handles.DrawLine(movement.transform.position, visibleTarget.transform.position);
		}

		List<ViewCastInfo> viewPoints = new List<ViewCastInfo>();
		for (int i = 0; i <= stepCount; i++)
		{
			Debug.Log($"stepcount {i}");
			float angle = movement.transform.eulerAngles.y - (360 / 2) + stepAngleSize * i;
			Debug.Log($"stepAngleSize {stepAngleSize}");
			ViewCastInfo newViewCast = ViewCast(angle, bestTarget, movement.AiAgent.coverSystem.CoversLayers);

			viewPoints.Add(newViewCast);
		}




		foreach (ViewCastInfo viewPoint in viewPoints)
		{

			Handles.DrawLine(bestTarget.transform.position, viewPoint.point);

			if (viewPoint.hit == true)
			{


				if (NavMesh.SamplePosition(viewPoint.point, out NavMeshHit hit, 2f, movement.AiAgent.agent.areaMask))
				{

					if (NavMesh.FindClosestEdge(hit.position, out hit, movement.AiAgent.agent.areaMask))
					{
						Handles.color = Color.red;
						Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit.position, Quaternion.identity, 0.25f, EventType.Repaint);

					}
				}

				if (NavMesh.SamplePosition(viewPoint.point - (bestTarget.position - hit.position).normalized, out NavMeshHit hit2, 2f, movement.AiAgent.agent.areaMask))
				{

					if (NavMesh.FindClosestEdge(hit.position, out hit, movement.AiAgent.agent.areaMask))
					{
						Handles.color = Color.green;
						Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit2.position, Quaternion.identity, 0.25f, EventType.Repaint);
						// cover spot
					}
				}


			}
		}
	}


	ViewCastInfo ViewCast(float globalAngle, Transform unit, LayerMask HidableLayers)
	{
		Vector3 dir = DirFromAngle(globalAngle, true, unit.transform);
		RaycastHit hit;




		if (Physics.Raycast(unit.transform.position, dir, out hit, 12, HidableLayers))
		{
			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
		}
		else
		{
			return new ViewCastInfo(false, unit.transform.position + dir * 12, 12, globalAngle);
		}
	}


	public struct ViewCastInfo
	{
		public bool hit;
		public Vector3 point;
		public float dst;
		public float angle;

		public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
		{
			hit = _hit;
			point = _point;
			dst = _dst;
			angle = _angle;
		}
	}
	public struct EdgeInfo
	{
		public Vector3 pointA;
		public Vector3 pointB;

		public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
		{
			pointA = _pointA;
			pointB = _pointB;
		}
	}
}