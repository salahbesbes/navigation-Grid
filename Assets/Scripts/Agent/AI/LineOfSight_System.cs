using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSight_System : MonoBehaviour
{
	private static readonly int sViewDepthTexturedID = Shader.PropertyToID("_ViewDepthTexture");
	private static readonly int sViewSpaceMatrixID = Shader.PropertyToID("_ViewSpaceMatrix");

	public Camera ViewCamera;
	private Material mMaterial;

	[Range(0, 360)]
	public float ViewAngle;
	public float ViewDistance;
	public LayerMask targetMask;
	public LayerMask obstacleMask;
	public Transform[] targetsInViewRadius;
	public List<GameObject> visibleTargets = new List<GameObject>();

	SensoryMemory memory = new SensoryMemory(10);
	AgentManager AiAgent;





	public bool HasTarget { get { return memory.BestMemory != null; } }
	public AiMemory Target { get { return memory.BestMemory; } }

	public void awake(AgentManager agent)
	{
		AiAgent = agent;
	}

	private void Start()
	{
		MeshRenderer renderer = GetComponent<MeshRenderer>();
		mMaterial = renderer.material;  // This generates a copy of the material
		renderer.material = mMaterial;

		RenderTexture depthTexture = new RenderTexture(ViewCamera.pixelWidth, ViewCamera.pixelHeight, 24, RenderTextureFormat.Depth);
		ViewCamera.targetTexture = depthTexture;
		ViewCamera.farClipPlane = ViewDistance;
		ViewCamera.fieldOfView = ViewAngle;

		transform.localScale = new Vector3(ViewDistance / transform.parent.localScale.x * 2, transform.localScale.y, ViewDistance / transform.parent.localScale.z * 2);

		mMaterial.SetTexture(sViewDepthTexturedID, ViewCamera.targetTexture);
		mMaterial.SetFloat("_ViewAngle", ViewAngle);

		StartCoroutine(FindTargetsWithDelay(0.01f));



	}


	private void Update()
	{

		//memory.updateLineOfSigt(this, gameObject);
		//memory.ForgetMemories(3);
		//memory.EvaluateMemories(CalculateScore, visibleTargets);

	}


	private void CalculateScore(AiMemory memo)
	{
		if (memo == null) return;
		float distanceScore = (float)memo.distance / ViewDistance;
		distanceScore = 1 - distanceScore;


		memo.score = distanceScore;
	}


	private void LateUpdate()
	{
		ViewCamera.Render();
		mMaterial.SetMatrix(sViewSpaceMatrixID, ViewCamera.projectionMatrix * ViewCamera.worldToCameraMatrix);
	}

	public int Filter(GameObject[] buffer, string LanyerName)
	{
		int layer = LayerMask.NameToLayer(LanyerName);
		int count = 0;
		foreach (GameObject obj in visibleTargets)
		{
			if (obj.layer == layer)
			{
				buffer[count++] = obj;
			}
			if (buffer.Length == count)
				break;
		}
		return count;
	}

	IEnumerator FindTargetsWithDelay(float delay)
	{
		while (true)
		{
			yield return new WaitForSeconds(delay);
			FindVisibleTargets();
		}
	}


	void FindVisibleTargets()
	{

		visibleTargets.Clear();
		Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, ViewDistance, targetMask);
		for (int i = 0; i < targetsInViewRadius.Length; i++)
		{
			Transform target = targetsInViewRadius[i].transform;




			Vector3 targetOffset = Vector3.zero;
			Vector3 CamOffset = Vector3.zero;

			float dotprod = Vector3.Dot(ViewCamera.transform.right.normalized, target.position.normalized);
			if (dotprod < 0)
			{

				targetOffset = Vector3.left * target.transform.GetComponent<Renderer>().bounds.size.x / 2.2f;
				CamOffset = Vector3.right * 0.2f;
				//Debug.Log($"target {target.name} is on left of the cam, dot {dotprod}");
			}
			else
			{
				targetOffset = Vector3.right * target.transform.GetComponent<Renderer>().bounds.size.x / 2.2f;
				CamOffset = Vector3.left * 0.2f;

				//Debug.Log($"target {target.name} is on right of the cam dot {dotprod}");


			}
			//targetOffset += Vector3.up * target.transform.GetComponent<Renderer>().bounds.size.y / (2 - 0.5f);



			Vector3 modifiedTargerPos = target.position + targetOffset;


			Vector3 CamPositionModified = ViewCamera.transform.position + CamOffset;


			Vector3 dir = modifiedTargerPos - CamPositionModified;
			float distance = dir.magnitude;

			Vector3 DirSameHeightOFTheCam = new Vector3(dir.x, 0, dir.z);
			//Debug.Log($"old angle is {Vector3.Angle(ViewCamera.transform.forward, dir) } new Angle {Vector3.Angle(ViewCamera.transform.forward, DirSameHeightOFTheCam) }  comp to {ViewAngle / 2}");
			dir = target.transform.position - ViewCamera.transform.position;
			if (Vector3.Angle(ViewCamera.transform.forward, DirSameHeightOFTheCam) < ViewAngle / 2)
			{
				if (Physics.Raycast(ViewCamera.transform.position, dir, out RaycastHit hit, distance, obstacleMask))
				{
					// in some cases when no Cover/Obstacle infront of the target this raycas is triggered and the hit is the target itSelf 
					Debug.DrawLine(ViewCamera.transform.position, ViewCamera.transform.position + dir, Color.black);
					if (hit.transform.Equals(target))
					{
						Debug.Log($" target {target.name} is behid some object {hit.collider.name}");
						visibleTargets.Add(target.gameObject);
						continue;
					}
					Debug.Log($"{hit.collider.name}");
				}
				else
				{
					visibleTargets.Add(target.gameObject);
				}

			}
			else
			{
				Debug.DrawLine(CamPositionModified, CamPositionModified + dir * 3, Color.magenta);
				//Debug.Log($"cant see {target} weired angle");

			}




		}
	}



	//void AddTargetToLineOfSightList(GameObject target)
	//{
	//	float distance = (target.transform.position - ViewCamera.transform.position).magnitude;


	//	visibleTargets.Add(target);
	//}
#if UNITY_EDITOR
#endif

	public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
	{
		if (!angleIsGlobal)
		{
			angleInDegrees += transform.eulerAngles.y;
		}
		return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}


	private void OnDrawGizmos()
	{
		Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1f, 0f, 1f));
		Gizmos.DrawWireSphere(Vector3.zero, ViewDistance);
		Gizmos.matrix = Matrix4x4.identity;



		foreach (GameObject visibleTarget in visibleTargets)
		{


			Transform target = visibleTarget.transform;





			Vector3 targetOffset = Vector3.zero;
			Vector3 CamOffset = Vector3.zero;

			float dotprod = Vector3.Dot(ViewCamera.transform.right.normalized, target.position.normalized);
			if (dotprod < 0)
			{

				targetOffset = Vector3.left * target.transform.GetComponent<Renderer>().bounds.size.x / 1.7f;
				CamOffset = Vector3.right * 0.2f;
				//Debug.Log($"target {target.name} is on left of the cam, dot {dotprod}");
			}
			else
			{
				targetOffset = Vector3.right * target.transform.GetComponent<Renderer>().bounds.size.x / 1.7f;
				CamOffset = Vector3.left * 0.2f;

				//Debug.Log($"target {target.name} is on right of the cam dot {dotprod}");


			}
			targetOffset += Vector3.up * target.transform.GetComponent<Renderer>().bounds.size.y / (2 - 0.5f);



			Vector3 modifiedTargerPos = target.position + targetOffset;


			Vector3 CamPositionModified = ViewCamera.transform.position + CamOffset;


			Vector3 dir = modifiedTargerPos - CamPositionModified;


			Vector3 DirSameHeightOFTheCam = new Vector3(dir.x, 0, dir.z);



			Gizmos.color = Color.green;
			Gizmos.DrawLine(CamPositionModified, CamPositionModified + dir);

		}
		float max = int.MinValue;

		foreach (var item in memory.memories)
		{
			if (item.score > max)
				max = item.score;
		}


		foreach (var memo in memory.memories)
		{
			Color green = Color.black;
			green.a = memo.score / max;
			//green.a = Mathf.Clamp(green.a, 0.3f, 1);
			Gizmos.color = green;
			if (memory.BestMemory != null && memory.BestMemory == memo)
			{
				Gizmos.color = Color.yellow;

			}

			Gizmos.DrawSphere(memo.posistion + Vector3.down * 0.5f, 0.25f);
		}
	}


}