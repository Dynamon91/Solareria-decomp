using System;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR;

public class EnemyPlaceHolder : MonoBehaviour
{
	[SerializeField]
	private string address;

	[SerializeField]
	private bool activeOnInstantiate = true;

	[SerializeField]
	private bool freePlacement;

	[SerializeField]
	private bool isAmbush;

	[Tooltip("100% 확률로 드랍해야하는 아이템 이름. 고정 드랍이 없을 경우 빈칸으로 두세요.")]
	[SerializeField]
	private string dropItemName;

	[SerializeField]
	private bool disableItemDrop;

	private Action<Enemy> onInstantiated;

	public bool IsAlreadyDeadCreature
	{
		get
		{
			if (!Game.Instance.RuntimeData.IsAlreadyDieCreature(Game.Instance.CurrentFieldID, base.transform.position))
			{
				return Game.Instance.SaveData.WorldData.IsEliminatedCreature(Game.Instance.CurrentFieldID, base.transform.position);
			}
			return true;
		}
	}

	private bool IsAmbushEliminated => Game.Instance.SaveData.WorldData.IsEliminatedAmbushCreature(Game.Instance.CurrentFieldID, base.transform.position);

	public void AddInstantiateCallback(Action<Enemy> action)
	{
		onInstantiated = (Action<Enemy>)Delegate.Combine(onInstantiated, action);
	}

	public virtual async UniTask<Enemy> InstantiateCharacterAsync(int sortingOrder)
	{
		if (string.IsNullOrEmpty(address))
		{
			DebugLog.GeneralLogWarning(base.gameObject.name + " : Address를 입력하지 않았습니다. ");
			return null;
		}
		AsyncOperationHandle<GameObject> opHandle = Addressables.LoadAssetAsync<GameObject>(address);
		await opHandle.Task;
		if (opHandle.Status != AsyncOperationStatus.Succeeded)
		{
			DebugLog.GeneralLogError("몬스터를 생성할 수 없습니다. 키:\"" + address + "\"");
			return null;
		}
		Enemy component = UnityEngine.Object.Instantiate(opHandle.Result, base.transform.position, Quaternion.identity, base.transform.parent).GetComponent<Enemy>();
		if (base.transform.localScale.x < 0f)
		{
			component.Flip();
		}
		component.InitializeOnInstantiate(sortingOrder, base.transform.position);
		if (component is Creature creature)
		{
			creature.SetDiableItemDrop(disableItemDrop);
			creature.SetFixedDropItemName(dropItemName);
			if (isAmbush && !IsAmbushEliminated)
			{
				creature.IsAmbushCreature = true;
				creature.StateMachine.ChangeState(creature.AmbushState);
			}
		}
		component.gameObject.SetActive(activeOnInstantiate);
		UnityEngine.Object.Destroy(base.gameObject);
		onInstantiated?.Invoke(component);
		onInstantiated = null;
		return component;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawCube((Vector2)base.transform.position + Vector2.up, Vector3.one * 2f);
	}

	protected virtual void OnDrawGizmosSelected()
	{
		if (!freePlacement)
		{
			base.transform.position = base.transform.position.RoundToInt();
		}
	}
}
