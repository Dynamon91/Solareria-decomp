using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR;

[Obsolete("CharacterPlaceHolder를 사용하세요.")]
public class NpcPlaceHolder : MonoBehaviour
{
	[SerializeField]
	private string address;

	[SerializeField]
	private List<string> scenarioIds;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawCube(base.transform.position, Vector3.one * 2f);
	}

	public async UniTask CreateNpcAsync(int sortingOrder, List<Npc> npcList)
	{
		if (string.IsNullOrEmpty(address))
		{
			DebugLog.GeneralLogError(base.gameObject.name + " : Address를 입력하지 않았습니다. ");
			return;
		}
		AsyncOperationHandle<GameObject> opHandle = Addressables.LoadAssetAsync<GameObject>(address);
		await opHandle.Task;
		if (opHandle.Status != AsyncOperationStatus.Succeeded)
		{
			DebugLog.GeneralLogError("NPC를 생성할 수 없습니다. 키:\"" + address + "\"");
			return;
		}
		Npc component = UnityEngine.Object.Instantiate(opHandle.Result, base.transform.position, Quaternion.identity, base.transform.parent).GetComponent<Npc>();
		component.SortingOrder = sortingOrder;
		npcList.Add(component);
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
