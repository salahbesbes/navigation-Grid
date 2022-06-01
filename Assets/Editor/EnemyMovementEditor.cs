using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(EnemyMovement))]
public class EnemyMovementEditor : Editor
{
	private Collider[] Colliders = new Collider[10];
	/*
	private void OnSceneGUI()
	{
		EnemyMovement movement = (EnemyMovement)target;
		if (movement == null || movement.Player == null)
		{
			return;
		}

		int Hits = Physics.OverlapSphereNonAlloc(movement.Agent.transform.position, movement.LineOfSightChecker.Collider.radius, Colliders, movement.HidableLayers);
		if (Hits > 0)
		{
			int HitReduction = 0;
			for (int i = 0; i < Hits; i++)
			{
				if (Vector3.Distance(hit.position, movement.Player.position) < movement.MinPlayerDistance)
				{
					Handles.color = Color.red;
					Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), Colliders[i].transform.position, Quaternion.identity, 0.25f, EventType.Repaint);
					Handles.Label(Colliders[i].transform.position, $"{i} too close to target");
					Colliders[i] = null;
					HitReduction++;
				}
				else if (Colliders[i].bounds.size.y < movement.MinObstacleHeight)
				{
					Handles.color = Color.red;
					Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), Colliders[i].transform.position, Quaternion.identity, 0.25f, EventType.Repaint);
					Handles.Label(Colliders[i].transform.position, $"{i} too small");
					Colliders[i] = null;
					HitReduction++;
				}

			}
			Hits -= HitReduction;

			System.Array.Sort(Colliders, movement.ColliderArraySortComparer);

			bool FoundTarget = false;

			for (int i = 0; i < Hits; i++)
			{
				if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, 2f, movement.Agent.areaMask))
				{
					if (!NavMesh.FindClosestEdge(hit.position, out hit, movement.Agent.areaMask))
					{
						Handles.color = Color.red;
						Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit.position, Quaternion.identity, 0.25f, EventType.Repaint);
						Handles.Label(hit.position, $"{i} (hit1) no edge found");
					}

					if (Vector3.Dot(hit.normal, (movement.Player.position - hit.position).normalized) < movement.HideSensitivity)
					{
						Handles.color = FoundTarget ? Color.yellow : Color.green;
						FoundTarget = true;
						Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit.position, Quaternion.identity, 0.25f, EventType.Repaint);
						Handles.Label(hit.position, $"{i} (hit1) dot: {Vector3.Dot(hit.normal, (movement.Player.position - hit.position).normalized)}");
					}
					else
					{
						Handles.color = Color.red;
						Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit.position, Quaternion.identity, 0.25f, EventType.Repaint);
						Handles.Label(hit.position, $"{i} (hit1) dot: {Vector3.Dot(hit.normal, (movement.Player.position - hit.position).normalized)}");

						if (NavMesh.SamplePosition(Colliders[i].transform.position - (movement.Player.position - hit.position).normalized * 2, out NavMeshHit hit2, 2f, movement.Agent.areaMask))
						{
							if (!NavMesh.FindClosestEdge(hit2.position, out hit2, movement.Agent.areaMask))
							{
								Handles.color = Color.red;
								Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit2.position, Quaternion.identity, 0.25f, EventType.Repaint);
								Handles.Label(hit.position, $"{i} (hit2) no edge found");
							}

							if (Vector3.Dot(hit2.normal, (movement.Player.position - hit2.position).normalized) < movement.HideSensitivity)
							{
								Handles.color = FoundTarget ? Color.yellow : Color.green;
								FoundTarget = true;
								Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit2.position, Quaternion.identity, 0.25f, EventType.Repaint);
								Handles.Label(hit2.position, $"{i} (hit2) dot: {Vector3.Dot(hit2.normal, (movement.Player.position - hit2.position).normalized)}");
							}
							else
							{
								Handles.color = Color.red;
								Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit2.position, Quaternion.identity, 0.25f, EventType.Repaint);
								Handles.Label(hit2.position, $"{i} (hit2) dot: {Vector3.Dot(hit2.normal, (movement.Player.position - hit2.position).normalized)}");
							}
						}
						else
						{
							Handles.color = Color.red;
							Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit2.position, Quaternion.identity, 0.25f, EventType.Repaint);
							Handles.Label(hit.position, $"{i} Hit 2 could not sampleposition");
						}
					}
				}
			}
		}
	}
	*/
	private void Hide()
	{
		EnemyMovement movement = (EnemyMovement)target;

		if (movement == null || movement.Player == null)
		{
			return;
		}

		int Hits = Physics.OverlapSphereNonAlloc(movement.Agent.transform.position, movement.LineOfSightChecker.Collider.radius, Colliders, movement.HidableLayers);

		for (int i = 0; i < Hits; i++)
		{
			if (NavMesh.SamplePosition(Colliders[i].transform.position - (movement.Player.position - Colliders[i].transform.position).normalized, out NavMeshHit hit, 2f, movement.Agent.areaMask))
			{


				if (NavMesh.FindClosestEdge(hit.position, out hit, movement.Agent.areaMask))
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
		EnemyMovement movement = (EnemyMovement)target;


		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i] = null;
		}

		int Hits = Physics.OverlapSphereNonAlloc(movement.Agent.transform.position, movement.LineOfSightChecker.Collider.radius, Colliders, movement.HidableLayers);


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
			ViewCastInfo newViewCast = ViewCast(angle, movement.Target, movement.HidableLayers);

			viewPoints.Add(newViewCast);
		}




		foreach (ViewCastInfo viewPoint in viewPoints)
		{

			Handles.DrawLine(movement.Target.transform.position, viewPoint.point);

			if (viewPoint.hit == true)
			{


				if (NavMesh.SamplePosition(viewPoint.point, out NavMeshHit hit, 2f, movement.Agent.areaMask))
				{

					if (NavMesh.FindClosestEdge(hit.position, out hit, movement.Agent.areaMask))
					{
						Handles.color = Color.red;
						Handles.SphereHandleCap(GUIUtility.GetControlID(FocusType.Passive), hit.position, Quaternion.identity, 0.25f, EventType.Repaint);

					}
				}

				if (NavMesh.SamplePosition(viewPoint.point - (movement.Player.position - hit.position).normalized, out NavMeshHit hit2, 2f, movement.Agent.areaMask))
				{

					if (NavMesh.FindClosestEdge(hit.position, out hit, movement.Agent.areaMask))
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