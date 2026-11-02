using System.Collections.Generic;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class WaterDragonCanon : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem beam1;

	[SerializeField]
	private ParticleSystem beam2;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private float angle;

	public string PrefabKey => "Water Dragon Canon.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "WaterDragonCanon";

	public Direction FacingDirection => Direction.Right;

	public void InitializeBeforeUse()
	{
		damageableList.Clear();
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void UpdateSize(Vector2 start, Vector2 end)
	{
		base.transform.position = start;
		float num = Vector2.Distance(start, end);
		Vector2 normalized = (end - start).normalized;
		angle = Vector2.SignedAngle(Vector2.up, normalized);
		base.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
		ParticleSystem.MainModule main = beam1.main;
		ParticleSystem.MainModule main2 = beam2.main;
		main.startSizeYMultiplier = num;
		main2.startSizeYMultiplier = num;
		Vector3 vector = base.transform.position + base.transform.rotation * new Vector3(0f, num / 2f);
		AttackUtility.AttackBox(size: new Vector2(main.startSizeXMultiplier, main.startSizeYMultiplier), center: vector, angle: angle, hitInfo: new HitInfo(this, string.Empty), attackLayer: LayerUtility.PlayerLayer, attackedList: damageableList);
	}
}
