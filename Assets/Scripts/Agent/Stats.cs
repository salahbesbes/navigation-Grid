using UnityEngine;

public class Stats : MonoBehaviour
{

	public float Health { get; private set; } = 100;
	public float ActionPoint { get; private set; } = 2;
	public float Defense { get; private set; } = 4;
	public float AvailableActionPoint { get; internal set; } = 2;
	public float AvailableAmmo { get; internal set; } = 2;
}
