using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Cutscene;
using SPR.Field;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class Chasa : Npc
{
	public const string SitAnim = "normal/sit";

	private const string SitLookAnim = "normal/sit_looking";

	[Header("Event SFX")]
	[SerializeField]
	private GameObject whistleEmitter;

	[SerializeField]
	private EventReference wipeEvent;

	[SerializeField]
	private int wipePeriodFrames;

	[Space]
	[SerializeField]
	private EventReference dummyEvent;

	private float wipeTimer;

	private bool canWhistle = true;

	private Dummy[] dummies;

	private static bool IsSR02 => Npc.CurrentFieldID.Equals(SectorName.SilentRuins, 2);

	private static bool IsAH05 => Npc.CurrentFieldID.Equals(SectorName.AshHills, 5);

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetEventSfx(IsAH05);
		if (IsFadedCastle())
		{
			SetAnimation("scene/look_b_loop", loop: true, 1f, 0, 0f, vfxEnable: false);
		}
		if (base.gameObject.activeSelf)
		{
			HideDummy();
		}
	}

	public override void Interact()
	{
		if (IsAH05 && Trigger.CheckTrigger("Trigger_ChasaRetry"))
		{
			Game.Instance.FieldController.FindCutsceneProducer<ChasaCutsceneProducer>().StartByDummy(byNpc: true);
		}
		else
		{
			base.Interact();
		}
	}

	public override async UniTask DisappearAsync()
	{
		SetAnimation("scene/teleport_b", loop: false);
		await UniTaskExtension.WaitForSeconds(2f);
		base.gameObject.SetActive(value: false);
	}

	protected override async UniTask MovePlayerBeforeTalkAsync()
	{
		if (IsFadedCastle() && Npc.Player.mover.IsOnOneWayPlatform)
		{
			Npc.Player.mover.PSM.ChangeState(Npc.Player.mover.PSM.JumpDownState);
			await UniTask.WaitUntil(() => Npc.Player.mover.IsOnGround);
		}
		await base.MovePlayerBeforeTalkAsync();
	}

	protected override void EnterTalkAnimation()
	{
		if (IsFadedCastle())
		{
			SetAnimation("scene/talk_c[GREEN]_look_b", loop: false);
			AddAnimation("scene/talk_c_loop");
		}
		else if (GetCurrentAnim(0).Name.Equals("normal/sit"))
		{
			SetAnimation("normal/sit_looking");
		}
		else
		{
			base.EnterTalkAnimation();
		}
		SetEventSfx(value: false);
		canWhistle = false;
	}

	protected override void EnterIdleAnimation()
	{
		if (IsFadedCastle())
		{
			SetAnimation("scene/look_c", loop: false);
			AddAnimation("scene/look_c_loop");
		}
		else
		{
			base.EnterIdleAnimation();
		}
	}

	protected override void OnFinishTalk()
	{
		canWhistle = true;
		if (IsSR02 || IsAH05)
		{
			base.ShouldChaseSight = false;
			Dummy[] array = dummies;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ChangeState(0);
			}
		}
		else
		{
			base.OnFinishTalk();
		}
	}

	public void SetEventSfx(bool value)
	{
		if (!(!canWhistle && value) && (!canWhistle || value))
		{
			DebugLog.GeneralLog($"SetEventSfx : {value}");
			whistleEmitter.SetActive(value);
			wipeTimer = 0f;
			canWhistle = !value;
		}
	}

	public async UniTask SpawnDummy()
	{
		SetAnimation("normal/ground_push", loop: false);
		Npc.SoundManager.PlayOneShotAttached(dummyEvent, base.transform);
		await UniTask.WaitForSeconds(Util.FrameToSeconds(80f));
		Dummy[] array = dummies;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ChangeState(1);
		}
		await UniTask.WaitUntil(() => IsAnimComplete(0, "normal/ground_push"));
		SetAnimation("scene/idle_look_weapon_f");
	}

	protected override void Update()
	{
		base.Update();
		if (whistleEmitter.activeSelf)
		{
			if (wipeTimer <= 0f)
			{
				Npc.SoundManager.PlayOneShotAttached(wipeEvent, base.transform);
				wipeTimer = (float)wipePeriodFrames / 60f;
			}
			wipeTimer -= Time.deltaTime;
		}
	}

	private static bool IsFadedCastle()
	{
		return Npc.CurrentFieldID.Equals(SectorName.FadedCastle, 11);
	}

	private void HideDummy()
	{
		if (!base.transform.parent.TryGetComponent<Area>(out var component))
		{
			return;
		}
		dummies = component.GetComponentsInChildren<Dummy>();
		if (IsSR02)
		{
			Dummy[] array = dummies;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ChangeState(2);
			}
		}
	}
}
