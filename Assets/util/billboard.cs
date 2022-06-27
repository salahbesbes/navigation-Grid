using TMPro;
using UnityEngine;
public class billboard : MonoBehaviour
{
	Camera cam;
	public Transform thisUnit;
	public TextMeshProUGUI TextName;
	// Start is called before the first frame update


	void OnAimEvent(float aim, Transform sender)
	{
		if (thisUnit == sender)
		{
			TextName.text = $"\n aim: {aim}";
		}
	}

	private void OnEnable()
	{
		System_Cover.AimEvent += OnAimEvent;
	}
	void Start()
	{
		cam = Camera.main;
		TextName.text = $"{transform.parent.name}";
	}

	// Update is called once per frame
	void Update()
	{
		transform.LookAt(cam.transform.position + Vector3.up);
	}
}
