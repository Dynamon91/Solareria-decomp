using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class Lever : DoorTrigger
{
	[Tooltip("열린 문이 자동으로 닫힐 때까지 걸리는 시간. 0이면 다시 닫히지 않음")]
	[SerializeField]
	private int autoCloseTime;

	[SerializeField]
	private Transform barTransform;

	[Tooltip("문이 열려있을 때 때리면 문을 고정시키는 버튼의 위치. (0,0)이면 버튼이 없음")]
	[SerializeField]
	private Vector2 localDoorFixButtonPosition;

	private CancellationTokenSource autoCloseTokenSource;

	private const int BarCloseAngle = -45;

	private const int BarOpenAngle = 45;

	private const float BarRotateDuration = 0.25f;

	public override void AwakeObject(SaveData saveData)
	{
		IsOpen = false;
		barTransform.rotation = Quaternion.Euler(0f, 0f, -45f);
		InstantiateDoor(openOnAwake: false);
		InstantiateFixButton();
	}

	public override void Interact()
	{
		if (IsOpen)
		{
			return;
		}
		IsOpen = true;
		barTransform.DORotate(new Vector3(0f, 0f, 45f), 0.25f);
		foreach (Door connectedDoor in ConnectedDoors)
		{
			connectedDoor.SetDoorOpen(open: true);
			if (autoCloseTime > 0)
			{
				CloseDoorAfterAsync(connectedDoor, autoCloseTime, TaskUtil.RefreshToken(ref autoCloseTokenSource)).Forget();
			}
		}
	}

	public void TryFixDoorOpen()
	{
		if (IsOpen)
		{
			autoCloseTokenSource?.Cancel();
		}
	}

	private async UniTask CloseDoorAfterAsync(Door door, int time, CancellationToken token)
	{
		await UniTaskExtension.WaitForSeconds(time, token);
		if (!token.IsCancellationRequested)
		{
			IsOpen = false;
			barTransform.DORotate(new Vector3(0f, 0f, -45f), 0.25f);
			door.SetDoorOpen(open: false);
		}
	}

	private void InstantiateFixButton()
	{
		if (!(localDoorFixButtonPosition == Vector2.zero))
		{
			Vector3 pos = base.transform.position + (Vector3)localDoorFixButtonPosition;
			Game.Instance.ObjectPoolManager.GetObject<DoorFixButton>("TrapDoorFixButton.prefab", pos, base.transform).Initialize(this);
		}
	}

	protected override void OnDrawGizmos()
	{
		base.OnDrawGizmos();
		if (!(localDoorFixButtonPosition == Vector2.zero))
		{
			Gizmos.DrawWireCube(base.transform.position + (Vector3)localDoorFixButtonPosition, new Vector2(2f, 1f));
		}
	}
}
