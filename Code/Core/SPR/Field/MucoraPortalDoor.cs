using System;
using Cysharp.Threading.Tasks;
using FMODUnity;
using SPR.Save;
using Spine;
using UnityEngine;

namespace SPR.Field;

public class MucoraPortalDoor : ServiceObject, IStateSavable
{
	private enum State
	{
		Closed,
		Opened
	}

	[Header("SFX")]
	[SerializeField]
	private EventReference doorOpen;

	private BoxCollider2D box2D;

	private bool interacted;

	public string ID => Util.GetID(Game.FieldID, "MucoraPortalDoor");

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		AddAnimationEvent(AnimState.Complete, delegate(TrackEntry t)
		{
			if (t.Animation.Name == "open_2")
			{
				BoxCollider2D[] componentsInChildren = GetComponentsInChildren<BoxCollider2D>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = false;
				}
			}
		});
	}

	public void LoadState(int state)
	{
		switch ((State)state)
		{
		case State.Closed:
			SetAnimation("static", loop: false);
			break;
		case State.Opened:
		{
			Box2D.enabled = false;
			BoxCollider2D[] componentsInChildren = GetComponentsInChildren<BoxCollider2D>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			SetAnimation("open_loop", loop: false);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public override void Interact()
	{
		if (Game.Instance.SaveData.PlayerData.GetAcquiredItemCount("MucoraPortalDoorKey") > 0)
		{
			interacted = true;
			base.Interact();
		}
		else
		{
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_MucoraPortalDoor_Warning");
		}
	}

	public override void InteractEnter()
	{
		base.InteractEnter();
		SetAnimationInstantly("inspect", loop: false);
		EnableVFX("inspect");
	}

	public override void InteractExit()
	{
		base.InteractExit();
		if (!interacted)
		{
			SetAnimation("static", loop: false);
		}
	}

	public async UniTask OpenDoor()
	{
		Game.Instance.SaveData.PlayerData.TryRemoveAcquiredItem("MucoraPortalDoorKey");
		ChangeState(1);
		Box2D.enabled = false;
		SetAnimation("open_1", loop: false);
		ServiceObject.SoundManager.PlayOneShotAttached(doorOpen, base.transform);
		await UniTask.WaitUntil(() => IsAnimComplete(0));
		SetAnimation("open_2", loop: false);
		await UniTask.WaitUntil(() => IsAnimComplete(0));
	}
}
