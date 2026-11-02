using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.L10N;
using SPR.Library;
using SPR.Save;
using SPR.UI.Map;
using Spine.Unity;
using Spine.Unity.Playables;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public abstract class BossCutsceneProducer : CutsceneProducer, IMappable
{
	[Header("Memory Crystal")]
	[SerializeField]
	protected Transform bossFigure;

	[SerializeField]
	private BossName bossName;

	[Tooltip("컷신 이후에 활성화하고자 하는 플레이어 카드 ID. 0일 경우엔 아무 카드도 활성화하지 않습니다.")]
	[SerializeField]
	private int cardID;

	[SerializeField]
	protected GameObject bossCorpse;

	[SerializeField]
	protected float resonanceDelay = 2f;

	[SerializeField]
	private Vector2 mapIconOffset;

	private BossCharacter boss;

	private SkeletonAnimation bossSpine;

	private Area fightArea;

	protected Area OriginArea;

	private float fightTime;

	protected bool SetConfinePlayerMovement;

	protected virtual BossCharacter Boss => boss ?? (boss = FightArea.GetComponentInChildren<BossCharacter>(includeInactive: true));

	protected SkeletonAnimation BossSpine => bossSpine ?? (bossSpine = bossFigure.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	protected Area FightArea => fightArea ?? (fightArea = GetComponentInParent<Area>());

	protected virtual Vector2 KnockBackVelocity => new Vector2(50f, 12f);

	protected virtual float KnockBackHoldTime => 0f;

	public BossName BossName => bossName;

	public bool IsFighting => fightTime > 0f;

	protected virtual string ResonanceID => LocaleKey.BossClearResonance(BossName);

	protected void StartFight()
	{
		fightTime = Time.time;
		CutsceneProducer.EnemyFightData.AddBossTry(bossName);
	}

	protected void ClearFight(bool updateMemory = true, bool stopBgm = true, bool getHaechiReward = true)
	{
		CutsceneProducer.EnemyFightData.TryBreakBossClearRecord(bossName, Time.time - fightTime);
		fightTime = 0f;
		if (stopBgm)
		{
			CutsceneProducer.SoundManager.StopBGM();
		}
		if (getHaechiReward)
		{
			CutsceneProducer.WanderingData.AddNewHaechiReward(IsFiveWarriors(BossName));
		}
		CutsceneProducer.UIManager.MenuCanvas.MemoryPanel.UpdateMemory(bossName, isClear: true, updateMemory);
	}

	protected void OnBeforeSkipTimeline(bool resetBossAnimation = true)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		if (resetBossAnimation)
		{
			BossSpine.SetAnimationInstantly("idle");
		}
		CutsceneProducer.Player.mover.CancelIfKnockBack(instantly: true);
	}

	protected void BindTracksToRealBoss(bool tween = true, bool animator = false)
	{
		Boss.IsImmortal = false;
		Boss.gameObject.SetActive(value: true);
		base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", Boss.SkeletonAnimation);
		if (tween)
		{
			base.PlayableDirector.TrackBinding<TweenTrack>("Tween Track", Boss.transform);
		}
		if (animator)
		{
			base.PlayableDirector.TrackBinding<AnimationTrack>("Animation Track", Boss.SkeletonAnimation.GetComponent<Animator>());
		}
	}

	protected void UnbindTracks()
	{
		base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", null);
		base.PlayableDirector.TrackBinding<TweenTrack>("Tween Track", null);
		base.PlayableDirector.TrackBinding<AnimationTrack>("Animation Track", null);
	}

	protected void SwitchToBossFigure()
	{
		bossFigure.SynchronizePositionDirection(Boss.transform);
		SwitchToFakeSpine(Boss, bossFigure, BossSpine);
	}

	protected void SwitchToBossFigure(Transform figure, BossCharacter bossCharacter)
	{
		figure.SynchronizePositionDirection(bossCharacter.transform);
		SwitchToFakeSpine(bossCharacter, figure, figure.GetComponentInChildren<SkeletonAnimation>());
	}

	protected void SwitchToRealBoss()
	{
		Boss.SynchronizeAnimation(BossSpine);
		Boss.ExitCutsceneState(bossFigure);
		bossFigure.gameObject.SetActive(value: false);
	}

	protected void ExitBossCutsceneState()
	{
		Boss.SynchronizeSelf("idle");
		Boss.ExitCutsceneState(Boss.transform);
	}

	protected static void AlignTransformXs(IEnumerable<Transform> transforms, float diffX, bool flipCondition, float flipOrigin = 0f)
	{
		foreach (Transform transform in transforms)
		{
			transform.Translate(diffX, 0f, 0f);
			if (flipCondition)
			{
				transform.ReflectXWithFlip(flipOrigin);
			}
		}
	}

	protected Direction FlipBossToTarget(float x)
	{
		Direction direction = ((!(Boss.transform.position.x >= x)) ? Direction.Right : Direction.Left);
		Boss.Flip(direction);
		return direction;
	}

	protected void ApplyStompForceAgainstBoss(int frames, float force)
	{
		Vector3 direction = (((CutsceneProducer.Player.mover.VisualBottom - Boss.VisualBottom).x < 0f) ? Vector3.left : Vector3.right);
		StompForce.ApplyStompForce(CutsceneProducer.Player.gameObject, frames, force, direction);
	}

	protected static UniTask WaitPlayerAnim(string animName = "idle")
	{
		return UniTask.WaitUntil(() => CutsceneProducer.Player.mover.GetCurrentAnim(0).Name == animName);
	}

	protected static void InvokeAfterWaitPlayerAnim(Action action, string animName = "idle")
	{
		WaitPlayerAnim(animName).GetAwaiter().OnCompleted(action);
	}

	public void FlipBossToPlayer()
	{
		FlipBossToTarget(CutsceneProducer.Player.transform.position.x);
	}

	public void FlipFigureToPlayer()
	{
		Vector3 localScale = bossFigure.localScale;
		int num = ((CutsceneProducer.Player.transform.position.x > bossFigure.position.x) ? 1 : (-1));
		bossFigure.localScale = new Vector3((float)num * Mathf.Abs(localScale.x), localScale.y, localScale.z);
	}

	public void FlipPlayerToFigure()
	{
		CutsceneProducer.Player.mover.Flip(bossFigure.position.x - CutsceneProducer.Player.mover.VisualBottom.x);
	}

	public void KnockBackPlayer()
	{
		CutsceneProducer.Player.mover.ApplyKnockBack(bossFigure, KnockBackVelocity, KnockBackHoldTime);
	}

	public void ChangeArea()
	{
		if ((object)CutsceneProducer.Field.CurrentArea != FightArea)
		{
			OriginArea = CutsceneProducer.Field.CurrentArea;
			CutsceneProducer.Field.ChangeAreaTo(FightArea);
		}
	}

	public void SetBossImmortal(bool value)
	{
		Boss.IsImmortal = value;
	}

	public void LookUpAfterIdle()
	{
		InvokeAfterWaitPlayerAnim(base.PlayerLookUp);
	}

	protected void ActivateCorpse(bool activateChildren = false)
	{
		if (bossCorpse.IsNull())
		{
			DebugLog.GeneralLogWarning(base.gameObject.name + "의 시체 오브젝트가 존재하지 않습니다! 일단 무시하고 넘어갑니다.");
			return;
		}
		bossCorpse.SetActive(value: true);
		if (activateChildren)
		{
			for (int i = 0; i < bossCorpse.transform.childCount; i++)
			{
				bossCorpse.transform.GetChild(i).gameObject.SetActive(value: true);
			}
		}
		PositionDirectionSaver[] componentsInChildren = bossCorpse.GetComponentsInChildren<PositionDirectionSaver>(includeInactive: true);
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			((IStateSavable)componentsInChildren[j]).InitializeState();
		}
	}

	protected void EarnMoney()
	{
		CutsceneProducer.Player.PlayerStat.AddMoney(Boss.Money);
	}

	protected void ShowResonance(Action onCompleted = null)
	{
		if (Game.Instance.SaveData.TalkData.GetInteractCount(ActorName.Verita) != 0 && !VeritaCutsceneProducer.IsVeritaInfected)
		{
			CutsceneProducer.TalkCanvas.ShowSpecialTalk(ResonanceID, resonanceDelay).GetAwaiter().OnCompleted(onCompleted);
		}
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 vector = ((BossName == BossName.Imahen) ? (bossFigure.position.ToVector2() - center) : (UnityEngine.Object.FindObjectOfType<EnemyPlaceHolder>().transform.position.ToVector2() - center));
		return new MapObjectInfo(IsFiveWarriors(BossName) ? MapObjectType.FiveWarriors : MapObjectType.Boss, vector + mapIconOffset, (int)BossName);
	}

	protected void ConfinePlayerMovement()
	{
		if (SetConfinePlayerMovement)
		{
			float orthographicSize = GraphicHelper.MainCamera.orthographicSize;
			float num = orthographicSize * GraphicHelper.MainCamera.aspect;
			Vector3 position = GraphicHelper.MainCamera.transform.position;
			Vector3 vector = new Vector3(position.x + num, position.y + orthographicSize);
			Vector3 vector2 = new Vector3(position.x - num, position.y - orthographicSize);
			Vector3 position2 = CutsceneProducer.Player.transform.position;
			float x = Mathf.Clamp(position2.x, vector2.x, vector.x);
			Mathf.Clamp(position2.y, vector2.y, vector.y);
			CutsceneProducer.Player.transform.position = new Vector3(x, position2.y);
		}
	}

	private static bool IsFiveWarriors(BossName bossName)
	{
		if (bossName != BossName.DarkChasa && bossName != BossName.Baran && bossName != BossName.Soowoo && bossName != BossName.Moldin)
		{
			return bossName == BossName.Gaia;
		}
		return true;
	}
}
