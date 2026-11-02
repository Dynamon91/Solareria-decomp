using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Cutscene;
using SPR.Field;
using SPR.L10N;
using SPR.UI.Talk;
using SPR.UI.Upgrade;
using Spine;

namespace SPR;

public class SpecialScript : Script
{
	private readonly string specialActor;

	private readonly string specialKey;

	private readonly string specialValue;

	public override bool IsInstant => false;

	public SpecialScript(IReadOnlyList<string> values)
	{
		specialActor = values[0];
		specialKey = values[1];
		specialValue = values[Script.ScenarioKOIndex];
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		RunSpecialAsync(talkCanvas).GetAwaiter().OnCompleted(talkCanvas.RunNextScript);
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		switch (specialKey)
		{
		case "Disappear":
		{
			Npc npc = Script.FieldController.FindNpc(specialActor);
			if (npc.IsNull())
			{
				DebugLog.ScenarioLogError(specialActor + " 캐릭터를 찾을 수 없습니다.");
				talkCanvas.RunNextScript_Skip();
				return;
			}
			npc.DisappearAsync().Forget();
			break;
		}
		case "TeleportToGate":
			((NpcVerita)Script.FieldController.FindNpc("Verita")).TeleportToGate().Forget();
			break;
		case "DisappearWithFade":
		{
			Npc npc2 = Script.FieldController.FindNpc(specialActor);
			if (npc2.IsNull())
			{
				DebugLog.ScenarioLogError(specialActor + " 캐릭터를 찾을 수 없습니다.");
				return;
			}
			npc2.DisappearWithFade();
			break;
		}
		case "ChasaFightRetry":
		{
			ChasaCutsceneProducer chasaCutsceneProducer = Script.FieldController.FindCutsceneProducer<ChasaCutsceneProducer>();
			if (chasaCutsceneProducer.IsNull())
			{
				DebugLog.ScenarioLogError("차사 컷씬 프로듀서를 찾을 수 없습니다.");
				return;
			}
			chasaCutsceneProducer.StartByDummy(byNpc: false);
			break;
		}
		case "UsePortal":
		{
			if (!Script.FieldController.TryFindInteractable(out Portal target, (Func<Portal, bool>)null))
			{
				DebugLog.ScenarioLogError("현재 사용중인 포탈을 찾을 수 없습니다.");
				return;
			}
			UniTask task = target.MoveSceneAsync();
			Script.InputManager.BlockControlForTask(task, "Move Scene By Portal").Forget();
			break;
		}
		case "FadeWhiteOut":
		{
			float postDelay = float.Parse(specialValue);
			Script.FadeCanvas.FadeWhiteOut(0f, postDelay).Forget();
			break;
		}
		case "FadeIn":
		{
			float postDelay = float.Parse(specialValue);
			Script.FadeCanvas.FadeIn(0f, postDelay).Forget();
			break;
		}
		case "FadeOut":
		{
			float postDelay = float.Parse(specialValue);
			Script.FadeCanvas.FadeOut(0f, postDelay).Forget();
			break;
		}
		}
		talkCanvas.RunNextScript_Skip();
	}

	private async UniTask RunSpecialAsync(TalkCanvas talkCanvas)
	{
		talkCanvas.IsTalking = false;
		switch (specialKey)
		{
		case "HideTalkBoard":
			if (specialValue.Equals("wait"))
			{
				await talkCanvas.DeactivateTalkBoardAsync();
			}
			else
			{
				talkCanvas.DeactivateTalkBoardAsync().Forget();
			}
			break;
		case "Disappear":
		{
			Npc npc2 = Script.FieldController.FindNpc(specialActor);
			if (npc2.IsNull())
			{
				DebugLog.ScenarioLogError(specialActor + " 캐릭터를 찾을 수 없습니다.");
			}
			else
			{
				await npc2.DisappearAsync();
			}
			break;
		}
		case "OpenStore":
		{
			StoreNPC storeNPC = (StoreNPC)Script.FieldController.FindNpc(specialActor);
			if (storeNPC.IsNull())
			{
				DebugLog.ScenarioLogError(specialActor + " 캐릭터를 찾을 수 없습니다.");
				break;
			}
			UniTask task = storeNPC.OpenServiceAsync();
			Script.InputManager.BlockControlForTask(task, "Open " + specialActor + " Store").Forget();
			break;
		}
		case "WarningMarkerSoldOut":
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_Store_Warning_HasNoSpecialMarker");
			break;
		case "OpenWarmhornCountUpgradePanel":
		{
			Lunya lunya = (Lunya)Script.FieldController.FindNpc(specialActor);
			if (lunya.IsNull())
			{
				DebugLog.ScenarioLogError("웜혼 개수 강화 패널을 열려고 하는데 " + specialActor + " 캐릭터를 찾을 수 없습니다.");
			}
			else
			{
				lunya.OpenCountUpgradePanel();
			}
			break;
		}
		case "OpenCardEngravingPanel":
		{
			Opus opus = (Opus)Script.FieldController.FindNpc(specialActor);
			if (opus.IsNull())
			{
				DebugLog.ScenarioLogError("카드 세공 패널을 열려고 하는데 필드에서 오푸스를 찾을 수 없습니다.");
				break;
			}
			UniTask task3 = opus.OpenServiceAsync();
			Script.InputManager.BlockControlForTask(task3, "Open " + specialActor + " Store").Forget();
			break;
		}
		case "OpenHoningPanel":
		{
			Flutis flutis = (Flutis)Script.FieldController.FindNpc("Flutis");
			if (flutis.IsNull())
			{
				DebugLog.ScenarioLogError("세공 패널을 열려고 하는데 Flutis 캐릭터를 찾을 수 없습니다.");
			}
			else
			{
				await flutis.OpenHoningPanelAsync();
			}
			break;
		}
		case "OpenUpgrade":
			((NpcVerita)Script.FieldController.FindNpc("Verita")).OpenServiceAsync().Forget();
			break;
		case "UpgradeHeat":
			await ((NpcVerita)Script.FieldController.FindNpc("Verita")).UpgradeHeatAsync();
			break;
		case "WarningChoice":
			Script.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.UpgradeWarning(UpgradeCheckResult.NotEnoughMaterial));
			await talkCanvas.ShakeChoiceOptionAsync();
			break;
		case "RemoveChoice":
			await talkCanvas.RemoveChoiceBoardAsync();
			break;
		case "TeleportToGate":
			await ((NpcVerita)Script.FieldController.FindNpc("Verita")).TeleportToGate();
			break;
		case "VeritaWhiteOut":
			await ((NpcVerita)Script.FieldController.FindNpc("Verita")).WhiteOutAsync();
			break;
		case "DisappearWithFade":
		{
			Npc npc = Script.FieldController.FindNpc(specialActor);
			if (npc.IsNull())
			{
				DebugLog.ScenarioLogError(specialActor + " 캐릭터를 찾을 수 없습니다.");
			}
			else
			{
				npc.DisappearWithFade();
			}
			break;
		}
		case "ChasaFightRetry":
		{
			ChasaCutsceneProducer chasaCutsceneProducer = Script.FieldController.FindCutsceneProducer<ChasaCutsceneProducer>();
			if (chasaCutsceneProducer.IsNull())
			{
				DebugLog.ScenarioLogError("차사 컷씬 프로듀서를 찾을 수 없습니다.");
			}
			else
			{
				chasaCutsceneProducer.StartByDummy(byNpc: false);
			}
			break;
		}
		case "UsePortal":
		{
			if (!Script.FieldController.TryFindInteractable(out Portal target, (Func<Portal, bool>)null))
			{
				DebugLog.ScenarioLogError("현재 사용중인 포탈을 찾을 수 없습니다.");
				break;
			}
			UniTask task2 = target.MoveSceneAsync();
			Script.InputManager.BlockControlForTask(task2, "Move Scene By Portal").Forget();
			break;
		}
		case "Delay":
			await UniTask.WaitForSeconds(float.Parse(specialValue));
			break;
		case "ChasaSpawnDummy":
		{
			PlayerMover mover = Game.Instance.Player.mover;
			mover.SetAnimation("turn_back", loop: false);
			mover.AddAnimation("idle_back");
			UniTaskExtension.WaitForFixedUpdatesOrCancel(300).GetAwaiter().OnCompleted(delegate
			{
				mover.SetAnimation("turn_front", loop: false);
				mover.AddAnimation("idle", loop: false);
				mover.AddAnimation("cam_up");
			});
			await Script.FieldController.FindNpc("Chasa").CastObject<Chasa>().SpawnDummy();
			break;
		}
		case "FadeWhiteOut":
		{
			float duration = float.Parse(specialValue);
			if (specialActor.IsNullOrEmpty())
			{
				await Script.FadeCanvas.FadeWhiteOut(duration);
			}
			else
			{
				Script.FadeCanvas.FadeWhiteOut(duration).Forget();
			}
			break;
		}
		case "FadeIn":
		{
			float duration = float.Parse(specialValue);
			if (specialActor.IsNullOrEmpty())
			{
				await Script.FadeCanvas.FadeIn(duration);
			}
			else
			{
				Script.FadeCanvas.FadeIn(duration).Forget();
			}
			break;
		}
		case "FadeOut":
		{
			float duration = float.Parse(specialValue);
			if (specialActor.IsNullOrEmpty())
			{
				await Script.FadeCanvas.FadeOut(duration);
			}
			else
			{
				Script.FadeCanvas.FadeOut(duration).Forget();
			}
			break;
		}
		case "Give":
		{
			talkCanvas.DeactivateTalkBoardAsync().Forget();
			PlayerMover playerMover = Game.Instance.Player.mover;
			PlayerItemFlame playerItemFlame = await AddressableHelper.InstantiateAsync<PlayerItemFlame>("Player Item Flame.prefab");
			playerItemFlame.Initialize(playerMover.VisualCenter);
			Npc npc = Script.FieldController.FindNpc(specialActor);
			if (npc.IsNull())
			{
				DebugLog.ScenarioLogError(specialActor + " 캐릭터를 찾을 수 없습니다.");
				break;
			}
			playerItemFlame.PlayAsync(70, 2f, npc.VisualCenter).Forget();
			TrackEntry currentTrack = playerMover.GetCurrentTrack();
			string animName = currentTrack.Animation.Name;
			bool loop = currentTrack.Loop;
			string text = ((npc is Cardamine) ? "_low" : "");
			playerMover.SetAnimation("hand_over_start" + text, loop: false);
			playerMover.AddAnimation("hand_over_loop" + text, loop: false);
			playerMover.AddAnimation("hand_over_end" + text, loop: false);
			playerMover.AddAnimation(animName, loop);
			await UniTask.WaitUntil(() => playerMover.GetCurrentAnim(0).Name == animName);
			break;
		}
		}
	}
}
