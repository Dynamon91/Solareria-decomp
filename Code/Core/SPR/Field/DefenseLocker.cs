using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class DefenseLocker : InteractableLever
{
	[Space]
	[Tooltip("게임 실행 시 해당 위치에 방해물을 배치합니다. 위치 값은 월드 좌표입니다.")]
	[SerializeField]
	private Vector2 defensePosition;

	private DefenseObject defenseObject;

	public override string ID => $"DefenseLocker_{base.transform.position.ToVector2Int()}";

	public override void LoadState(int state)
	{
		InstantiateAsync(state == 0);
		base.LoadState(state);
	}

	private async void InstantiateAsync(bool isLocked)
	{
		Area area;
		Area objectArea = (Game.Instance.FieldController.TryFindAreaByPosition(defensePosition, out area) ? area : base.Area);
		defenseObject = await AddressableHelper.InstantiateAsync<DefenseObject>("Defense Object.prefab", defensePosition, 0f, objectArea.transform);
		int colliderHeight = PhysicsUtility.GetColliderHeight(defensePosition);
		defenseObject.Initialize(colliderHeight, isLocked, objectArea.ConfineCollider);
	}

	protected override async UniTask OnActivateAsync()
	{
		ChangeState(1);
		await Game.Instance.InputManager.BlockControlForTask(defenseObject.ActivateAsync(), "Defense Locker");
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.magenta;
		int colliderHeight = PhysicsUtility.GetColliderHeight(defensePosition);
		Gizmos.DrawWireCube(size: new Vector2(1f, colliderHeight), center: defensePosition);
	}

	private void OnValidate()
	{
		if (defensePosition == Vector2.zero)
		{
			defensePosition = base.transform.position;
		}
	}
}
