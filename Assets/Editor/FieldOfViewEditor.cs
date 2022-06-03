using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConeOfSightRenderer))]
public class FieldOfViewEditor : Editor
{

	void OnSceneGUI()
	{
		ConeOfSightRenderer fow = (ConeOfSightRenderer)target;
		Handles.color = Color.white;
		Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.ViewDistance);
		Vector3 viewAngleA = fow.DirFromAngle(-fow.ViewAngle / 2, false);
		Vector3 viewAngleB = fow.DirFromAngle(fow.ViewAngle / 2, false);

		Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleA * fow.ViewDistance);
		Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleB * fow.ViewDistance);

		Handles.color = Color.red;
		foreach (Transform visibleTarget in fow.targetsInViewRadius)
		{
			Vector3 targetOffset = Vector3.zero;
			Vector3 CamOffset = Vector3.zero;

			float dotprod = Vector3.Dot(fow.ViewCamera.transform.right.normalized, visibleTarget.position.normalized);
			if (dotprod < 0)
			{

				targetOffset = Vector3.left * visibleTarget.transform.GetComponent<Renderer>().bounds.size.x / 1.7f;
				CamOffset = Vector3.right * 0.2f;
				//Debug.Log($"target {visibleTarget.name} is on left of the cam, dot {dotprod}");
			}
			else
			{
				targetOffset = Vector3.right * visibleTarget.transform.GetComponent<Renderer>().bounds.size.x / 1.7f;
				CamOffset = Vector3.left * 0.2f;

				//Debug.Log($"target {visibleTarget.name} is on right of the cam dot {dotprod}");


			}
			targetOffset += Vector3.up * visibleTarget.transform.GetComponent<Renderer>().bounds.size.y / (2 - 0.5f);



			Vector3 modifiedTargerPos = visibleTarget.position + targetOffset;




			Vector3 CamPositionModified = fow.ViewCamera.transform.position + CamOffset;


			Vector3 dir = modifiedTargerPos - CamPositionModified;
			Vector3 DirSameHeightOFTheCam = new Vector3(dir.x, 0, dir.z);





			//Handles.color = Color.yellow;
			//Handles.DrawLine(fow.ViewCamera.transform.position, fow.ViewCamera.transform.position + dir * 5);

			//if (Physics.Raycast(CamPositionModified, dir, out RaycastHit hit, fow.obstacleMask))
			//{

			//	Handles.color = Color.yellow;
			//	Handles.DrawLine(CamPositionModified, hit.point);
			//	Handles.color = Color.black;
			//	Handles.DrawLine(hit.point, modifiedTargerPos);


			//}
			//else
			//{
			//	Handles.color = Color.black;
			//	Handles.DrawLine(CamPositionModified, modifiedTargerPos);

			//}





		}
	}

}
