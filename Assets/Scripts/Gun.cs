using System.Collections;
using UnityEngine;

public enum WeaponType
{
	none, shortRange, midRange, LongRange, heavyWeapon
}
[RequireComponent(typeof(Animator))]
public class Gun : MonoBehaviour
{
	[SerializeField]
	public float MaxRange = 10f;
	[SerializeField] protected AnimationCurve Responsecurve;

	public WeaponType type = WeaponType.none;
	[SerializeField]
	private bool AddBulletSpread = true;
	[SerializeField]
	private Vector3 BulletSpreadVariance = new Vector3(0.0f, 0.0f, 0.0f);
	[SerializeField]
	private ParticleSystem ShootingSystem;
	[SerializeField]
	private Transform BulletSpawnPoint;
	[SerializeField]
	private ParticleSystem ImpactParticleSystem;
	[SerializeField]
	private TrailRenderer BulletTrail;
	[SerializeField]
	public float ShootDelay = 0.5f;
	[SerializeField]
	private LayerMask Mask;
	[SerializeField]
	private float BulletSpeed = 100;

	private float LastShootTime;

	private AgentManager AiAgent;

	public float Damage { get; internal set; } = 40;

	public void awake(AgentManager AiAgent)
	{
		type = WeaponType.shortRange;
		this.AiAgent = AiAgent;
		AiAgent.weapon = this;
	}

	public void Shoot(Transform Target)
	{
		if (LastShootTime + ShootDelay < Time.time)
		{
			// Use an object pool instead for these! To keep this tutorial focused, we'll skip implementing one.
			// For more details you can see: https://youtu.be/fsDE_mO4RZM or if using Unity 2021+: https://youtu.be/zyzqA_CPz2E

			ShootingSystem.Play();
			Vector3 direction = GetDirection(Target);


			if (Physics.Raycast(BulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue, Mask))
			{
				TrailRenderer trail = Instantiate(BulletTrail, BulletSpawnPoint.position, Quaternion.identity);

				StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, true));

			}
			LastShootTime = Time.time;


		}
	}

	private Vector3 GetDirection(Transform Target)
	{
		Vector3 target = BulletSpawnPoint.position;
		//target = new Vector3(target.x, target.y * 1.5f, target.z);

		Vector3 direction = Target.position - target;


		if (AddBulletSpread)
		{
			direction += new Vector3(
			    Random.Range(-BulletSpreadVariance.x, BulletSpreadVariance.x),
			    Random.Range(-BulletSpreadVariance.y, BulletSpreadVariance.y),
			    Random.Range(-BulletSpreadVariance.z, BulletSpreadVariance.z)
			);

			//direction.Normalize();
		}

		return direction;
	}

	private IEnumerator SpawnTrail(TrailRenderer Trail, Vector3 HitPoint, Vector3 HitNormal, bool MadeImpact)
	{
		// This has been updated from the video implementation to fix a commonly raised issue about the bullet trails
		// moving slowly when hitting something close, and not
		Vector3 startPosition = Trail.transform.position;
		float distance = Vector3.Distance(Trail.transform.position, HitPoint);
		float remainingDistance = distance;

		while (remainingDistance > 0)
		{
			Trail.transform.position = Vector3.Lerp(startPosition, HitPoint, 1 - (remainingDistance / distance));

			remainingDistance -= BulletSpeed * Time.deltaTime;

			yield return null;
		}
		Trail.transform.position = HitPoint;
		if (MadeImpact)
		{
			Instantiate(ImpactParticleSystem, HitPoint, Quaternion.LookRotation(HitNormal));
		}

		Destroy(Trail.gameObject, Trail.time);
	}

	public IEnumerator StartShoting(Transform Target)
	{
		float elapcedTime = 0;

		while (elapcedTime < 3)
		{
			elapcedTime += Time.deltaTime;
			Shoot(Target);
			yield return null;
		}
	}

	private void OnDrawGizmos()
	{
		//if (AiAgent?.Target == null || BulletSpawnPoint == null) return;

		//Vector3 direction = GetDirection();

		//Gizmos.DrawRay(BulletSpawnPoint.position, direction);
	}

	public float Evaluate(float cover_Target_Distance)
	{
		return Responsecurve.Evaluate(cover_Target_Distance);
	}
}
