using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI;

public class StunPointGroup : MonoBehaviour
{
	public enum Type
	{
		Creature,
		Boss
	}

	public const string BossStunPoint = "Stun Point_Boss.prefab";

	private readonly List<StunPoint> stunPoints = new List<StunPoint>();

	public async UniTaskVoid SetCount(Type type, int count)
	{
		if (stunPoints.Count == count && count > 0)
		{
			return;
		}
		stunPoints.Clear();
		foreach (Transform item in base.transform)
		{
			Object.Destroy(item.gameObject);
		}
		for (int i = 0; i < count; i++)
		{
			StunPoint stunPoint = await AddressableHelper.InstantiateAsync<StunPoint>("Stun Point_Boss.prefab", base.transform, 45f);
			stunPoint.transform.localScale = Vector3.one;
			stunPoint.SetActive(active: true);
			stunPoint.SetType(type);
			stunPoints.Add(stunPoint);
		}
	}

	public void UpdateCount(int value)
	{
		for (int i = 0; i < stunPoints.Count; i++)
		{
			stunPoints[i].SetActive(i < value);
		}
	}

	public void ResetCount()
	{
		foreach (StunPoint stunPoint in stunPoints)
		{
			stunPoint.SetActive(active: true);
		}
	}
}
