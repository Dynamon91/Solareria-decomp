using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class MoldinThorn : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private GameObject warning;

	public const string Address = "Moldin_Thorn.prefab";

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	public string PrefabKey => "Moldin_Thorn.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "MoldinThorn";

	public Direction FacingDirection => Direction.None;

	public void InitializeBeforeUse()
	{
		warning.SetActive(value: true);
		Vector3 localScale = base.transform.localScale;
		base.transform.localScale = new Vector3(localScale.x, 1f, localScale.z);
		base.gameObject.SetActive(value: true);
		damageableList.Clear();
	}

	public void ReturnToPool()
	{
		warning.SetActive(value: false);
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public async UniTaskVoid Rise(int warningFrame, int riseFrame, float height)
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(warningFrame);
		warning.SetActive(value: false);
		base.transform.DOScaleY(height, Util.FrameToSeconds(riseFrame)).SetEase(Ease.Linear).OnUpdate(HandleOverlap)
			.OnComplete(ReturnToPool);
	}

	private void HandleOverlap()
	{
		Vector3 localScale = base.transform.localScale;
		AttackUtility.AttackBox(base.transform.position + new Vector3(0f, localScale.y / 2f), localScale, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer, damageableList);
	}
}
