using Cysharp.Threading.Tasks;
using FMODUnity;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class SolinLabEntranceDoor : ServiceObject, IStateSavable
{
	private enum State
	{
		Closed,
		Opened
	}

	[Header("Solin Lab Entrance Door")]
	[SerializeField]
	private BoxCollider2D doorCollider;

	[SerializeField]
	private EventReference openEvent;

	private BoxCollider2D interactCollider;

	private const string ObjectName = "SolinLabEntranceDoor";

	private BoxCollider2D InteractCollider => interactCollider ?? (interactCollider = GetComponent<BoxCollider2D>());

	public string ID => Util.GetID(Game.FieldID, "SolinLabEntranceDoor");

	public void LoadState(int state)
	{
		bool flag = (base.enabled = state == 0);
		InteractCollider.enabled = flag;
		doorCollider.enabled = flag;
		SetAnimation(flag ? "close_loop" : "open_loop", loop: false);
	}

	public void ChangeState(int state)
	{
		bool flag = (base.enabled = state == 0);
		InteractCollider.enabled = flag;
		doorCollider.enabled = flag;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public async UniTask OpenDoorAsync()
	{
		base.ServiceVcam.SetActive(value: true);
		ChangeState(1);
		ServiceObject.SoundManager.PlayOneShotAttached(openEvent, base.transform);
		await SetAnimationAsync("open");
		AddAnimation("open_loop", loop: false);
		ServiceObject.PlayerMover.SetAnimation("idle");
		base.ServiceVcam.SetActive(value: false);
	}
}
