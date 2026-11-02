using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Field;
using SPR.Save;
using Spine;
using UnityEngine;

namespace SPR;

public class Haechi : Npc
{
	[SerializeField]
	private float sleepTailwagDuration;

	[SerializeField]
	private float idleFaceRubDuration;

	[SerializeField]
	private EventReference barkEvent;

	[SerializeField]
	private EventReference cuteEvent;

	[SerializeField]
	private EventReference howlEvent;

	private bool isSleeping = true;

	private EventInstance barkInstance;

	private Collider2D col;

	public override int InteractableOrder { get; }

	private Collider2D Col => col ?? (col = GetComponent<Collider2D>());

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		isSleeping = !WanderingData.HasHaechiReward();
		SetAnimation(isSleeping ? "curl" : "idle");
	}

	public override void Interact()
	{
		if (!isSleeping)
		{
			SetAnimation("idle_tailwag");
		}
		base.Interact();
	}

	public override void InteractEnter()
	{
		if (!isSleeping && !barkInstance.isValid())
		{
			barkInstance = Npc.SoundManager.PlayOneShotAttached(barkEvent, base.transform);
		}
		base.InteractEnter();
	}

	protected override void StartTalk()
	{
		Col.enabled = false;
		UniTask task = (isSleeping ? InteractWhileSleepingAsync() : InteractIdleAsync());
		Npc.InputManager.BlockControlForTask(task, "Haechi Idle").Forget();
		if (Npc.TalkData.GetInteractCount(npcName) == 0 && !WanderingData.IsWatchedCutscene("HaechiFlyCutsceneProducer"))
		{
			WanderingData.EnqueueResonanceAfterMoveScene("HaechiFly_Resonance");
		}
	}

	protected override void EnterIdleAnimation()
	{
		SetAnimation(isSleeping ? "curl" : "idle");
	}

	private async UniTask InteractIdleAsync()
	{
		Npc.FieldCanvas.HideAllAsync(instant: false).Forget();
		Npc.Player.mover.SetAnimation("scene/heachi_bye_start", loop: false);
		await UniTask.WaitForSeconds(0.65f);
		SetAnimation("face_rub");
		Npc.Player.mover.SetAnimation("scene/heachi_bye_loop");
		Npc.SoundManager.PlayOneShotAttached(cuteEvent, base.transform);
		await UniTask.WaitForSeconds(idleFaceRubDuration);
		SetAnimation("idle_tailwag");
		Npc.Player.mover.SetAnimation("scene/heachi_bye_end", loop: false);
		Npc.Player.mover.AddAnimation("idle");
		await UniTask.WaitForSeconds(1f);
		SetAnimation("howling_end", loop: false);
		AddAnimation("idle");
		await UniTask.WaitForSeconds(0.3f);
		Npc.SoundManager.PlayOneShotAttached(howlEvent, base.transform);
		await UniTask.WaitForSeconds(0.3f);
		Npc.Player.mover.SetAnimation("cam_up");
		await UniTask.WaitForSeconds(3.4f);
		await InstantiateRewardAsync();
		await UniTask.WaitForSeconds(0.5f);
		if (!WanderingData.HasHaechiReward())
		{
			await Npc.FadeCanvas.FadeOut(1f);
			isSleeping = true;
			EnterIdleAnimation();
			base.SkeletonAnimation.Skeleton.UpdateWorldTransform(Skeleton.Physics.Reset);
			Npc.Player.mover.SetAnimation("idle");
			await Npc.FadeCanvas.FadeIn(1f, 1.5f);
		}
		else
		{
			Npc.Player.mover.SetAnimation("idle");
		}
		FinishTalk();
		Col.enabled = true;
		Npc.FieldCanvas.ShowAllAsync(instant: false).Forget();
	}

	private async UniTask InteractWhileSleepingAsync()
	{
		SetAnimation("curl_tailwag");
		Npc.SoundManager.PlayOneShotAttached(cuteEvent, base.transform);
		await UniTask.WaitForSeconds(sleepTailwagDuration);
		SetAnimation("curl");
		FinishTalk();
		Col.enabled = true;
	}

	private async UniTask InstantiateRewardAsync()
	{
		IReadOnlyList<string> rewardList = WanderingData.GetHaechiRewards();
		int rewardCount = rewardList.Count;
		if (rewardCount == 0)
		{
			return;
		}
		Vector2 vector = base.transform.position;
		Vector2 startPos = vector + Vector2.up;
		float endPosY = Npc.Player.mover.VisualCenter.y;
		float num = base.InteractPositionX / 3f;
		float num2 = vector.x + num;
		List<float> endPosList = new List<float>(rewardCount);
		for (int i = 0; i < rewardCount; i++)
		{
			float num3 = (float)CryptoRandom.Range(0, 10) * 0.1f - 0.5f;
			endPosList.Add(num2 + num3);
			num2 += num;
			if (i == 1)
			{
				num2 += num;
			}
		}
		for (int j = 0; j < rewardCount; j++)
		{
			Vector2Int endPos = new Vector2(endPosList[j], endPosY).ToVector2Int();
			ItemObject.DropItemAsync(rewardList[j], startPos, endPos).Forget();
			await UniTaskExtension.WaitForSeconds(0.4f);
		}
		await UniTaskExtension.WaitForSeconds(0.6f);
	}
}
