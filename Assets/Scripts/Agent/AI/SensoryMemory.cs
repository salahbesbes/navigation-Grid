using System;
using System.Collections.Generic;
using UnityEngine;

public class AiMemory
{
	public float Age { get { return Time.time - lastSeen; } }
	public GameObject gameobject;
	public Vector3 posistion;
	public Vector3 direction;
	public float distance;
	public float angle;
	public float lastSeen;
	public float score;
}
public class SensoryMemory
{
	public List<AiMemory> memories = new List<AiMemory>();
	GameObject[] charachters;
	public AiMemory BestMemory;
	public SensoryMemory(int paxPlayers)
	{
		charachters = new GameObject[paxPlayers];
	}
	public AiMemory FetchMemory(GameObject memoGameObject)
	{
		AiMemory memory = memories.Find(el => el.gameobject.Equals(memoGameObject));
		if (memory == null)
		{
			memory = new AiMemory();
			memories.Add(memory);
		}
		return memory;
	}


	public void RefreshMemory(GameObject npc, GameObject target)
	{
		AiMemory memory = FetchMemory(target);
		memory.gameobject = target;
		memory.posistion = target.transform.position;
		memory.direction = target.transform.position - npc.transform.position;
		memory.distance = memory.direction.magnitude;
		memory.angle = Vector3.Angle(npc.transform.forward, memory.direction);
		memory.lastSeen = Time.time;
	}

	public void updateLineOfSigt(LineOfSight_System sensor, GameObject agent)
	{
		int count = sensor.Filter(charachters, "Player");

		for (int i = 0; i < count; i++)
		{
			GameObject target = charachters[i];
			RefreshMemory(sensor.gameObject, target);
		}
	}

	public void ForgetMemories(int time)
	{
		memories.RemoveAll(el => el.Age >= time);
		memories.RemoveAll(el => !el.gameobject);

		//memories.RemoveAll(el => el.isDead);
	}



	public void EvaluateMemories(Action<AiMemory> claculateScore, List<GameObject> TargetInRange)
	{
		if ((BestMemory == null || TargetInRange.Count == 1) && memories.Count > 0)
		{
			BestMemory = memories.Find(el => el.gameobject == TargetInRange[0]);
			claculateScore(BestMemory);
			return;
		}
		if (!TargetInRange.Contains(BestMemory.gameobject))
		{
			BestMemory.score = -1;
		}


		foreach (var obj in TargetInRange)
		{

			AiMemory memo = memories.Find(el => el.gameobject == obj);
			if (memo == null) continue;

			claculateScore(memo);


			if (memo.score > BestMemory?.score)
			{
				BestMemory = memo;
			}
		}
	}
}
