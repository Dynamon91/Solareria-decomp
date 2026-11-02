using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using LapinLibrary;
using Library;
using SPR.Save;
using SPR.UI.Map;
using Spine;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Field;

public class ShadowSproutStone : ServiceObject, IMappable
{
	[Header("Stone")]
	[SerializeField]
	private PlayableAsset noneItemTimeline;

	[SerializeField]
	private PlayableAsset rewardTimeline;

	[SerializeField]
	private List<ParticleSystem> shineEffects;

	[SerializeField]
	private EventReference rewardBigEvent;

	[SerializeField]
	private EventReference rewardSmallEvent;

	[SerializeField]
	private EventReference endEvent;

	[SerializeField]
	private List<EventReference> idleEvents;

	private IReadOnlyList<SproutRewardData> rewardDataList;

	private BoxCollider2D boxColl;

	private PlayableDirector director;

	private CancellationTokenSource interactTokenSource;

	private EventInstance idleInstance;

	private EventInstance interactInstance;

	private bool isInDropSequence;

	public override string Name => $"ShadowSproutStone_{CurrentRegionName}";

	public SproutStoneController Controller { get; private set; }

	private static PlayerController Player => Game.Instance.Player;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static RegionName CurrentRegionName => Game.FieldID.GetRegionName();

	protected override void Update()
	{
		base.Update();
		Util.CheckShownInCamera(Name, VisualBottom);
	}

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		boxColl = GetComponent<BoxCollider2D>();
		director = GetComponent<PlayableDirector>();
		rewardDataList = Util.GetSproutRewardDataList(CurrentRegionName);
		Controller = new SproutStoneController(this);
		SetStateByReward();
	}

	private void OnDisable()
	{
		StopIdleSound();
	}

	public override void Interact()
	{
		base.Interact();
		base.ServiceVcam.SetActive(value: true);
	}

	public override void OnServiceClosing()
	{
		Game.Instance.Player.mover.SetAnimation("idle");
		base.ServiceVcam.SetActive(value: false);
	}

	public bool HasReward()
	{
		return PlayerData.GetAcquiredItemCount(ShadowSprout.ItemName) > 0;
	}

	public void PlayPlayerGiveAnimation()
	{
		int acquiredItemCount = PlayerData.GetAcquiredItemCount(ShadowSprout.ItemName);
		PlayerStateMachine pSM = Player.mover.PSM;
		pSM.ChangeState(new PlayerSproutInteractState(pSM, acquiredItemCount > 2));
	}

	public async UniTask PlayInteractTimelineAsync()
	{
		CancellationToken token = TaskUtil.RefreshToken(ref interactTokenSource);
		int acquiredItemCount = PlayerData.GetAcquiredItemCount(ShadowSprout.ItemName);
		bool num = acquiredItemCount > 0;
		PlayableAsset playableAsset = (num ? rewardTimeline : noneItemTimeline);
		director.playableAsset = playableAsset;
		director.Play();
		StopIdleSound();
		if (num)
		{
			interactInstance = ServiceObject.SoundManager.PlayOneShotAttached((acquiredItemCount > 2) ? rewardBigEvent : rewardSmallEvent, base.transform);
		}
		await UniTask.WaitUntil(() => isInDropSequence, PlayerLoopTiming.Update, token);
		await UniTask.WaitUntil(() => !isInDropSequence, PlayerLoopTiming.Update, token);
	}

	public void SkipTimeline()
	{
		interactTokenSource?.Cancel();
		director.time = director.duration;
		director.Stop();
		if (interactInstance.isValid())
		{
			interactInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			interactInstance.release();
		}
		if (!isInDropSequence)
		{
			InstantiateRewardAsync(default(CancellationToken)).Forget();
		}
		EndTimeline();
	}

	public void EndTimeline()
	{
		SetStateByReward();
	}

	private void SetStateByReward()
	{
		int shadowRewardCount = PlayerData.GetShadowRewardCount(CurrentRegionName);
		int count = rewardDataList.Count;
		SetDefaultAnimation(count - shadowRewardCount);
		SetDefaultFX(shadowRewardCount, count);
	}

	public void DropReward()
	{
		InstantiateRewardAsync(interactTokenSource.Token).Forget();
	}

	private async UniTask InstantiateRewardAsync(CancellationToken token)
	{
		isInDropSequence = true;
		int preRewardCount = PlayerData.GetShadowRewardCount(CurrentRegionName);
		int acquiredCount = PlayerData.GetAcquiredItemCount(ShadowSprout.ItemName);
		int count = rewardDataList.Count;
		if (preRewardCount + acquiredCount > count)
		{
			return;
		}
		PlayerData.TryRemoveAcquiredItem(ShadowSprout.ItemName, acquiredCount);
		PlayerData.AddShadowRewardCount(CurrentRegionName, acquiredCount);
		Vector2 vector = base.transform.position;
		Vector2 startPos = vector + Vector2.up;
		Vector3 playerPos = Player.transform.position;
		float num = 4f - (float)acquiredCount * 0.3f;
		float num2 = (vector + boxColl.offset).x - boxColl.size.x / 2f + 1f - (float)(acquiredCount - 1) * 0.125f;
		List<float> endPosList = new List<float>(acquiredCount);
		for (int i = 0; i < acquiredCount; i++)
		{
			endPosList.Add(num2);
			num2 += num + (float)CryptoRandom.Range(0, 10) * 0.1f - 0.5f;
		}
		for (int j = 0; j < acquiredCount; j++)
		{
			int id = preRewardCount + j + 1;
			SproutRewardData sproutRewardData = rewardDataList.FirstOrDefault((SproutRewardData data) => data.ID == id);
			ItemObject.DropItemAsync(endPos: new Vector2(endPosList[j], playerPos.y).ToVector2Int(), itemName: sproutRewardData.ItemID, startPos: startPos, token: token).Forget();
			await UniTaskExtension.WaitForSeconds(0.4f, token);
		}
		isInDropSequence = false;
	}

	private void SetDefaultAnimation(int remainRewardCount)
	{
		if (remainRewardCount > 0)
		{
			base.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
			return;
		}
		TrackEntry trackEntry = base.SkeletonAnimation.AnimationState.SetAnimation(0, "end", loop: false);
		trackEntry.TrackTime = trackEntry.AnimationEnd;
	}

	private void SetDefaultFX(int currentRewardCount, int maxRewardCount)
	{
		int num = maxRewardCount - currentRewardCount;
		if (num <= 0)
		{
			StopIdleSound();
			boxColl.enabled = false;
			shineEffects.ForEach(delegate(ParticleSystem p)
			{
				p.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			});
			return;
		}
		int num2 = 0;
		if ((double)num >= (double)maxRewardCount * 0.7)
		{
			num2 = 0;
		}
		else if ((double)num >= (double)maxRewardCount * 0.4)
		{
			num2 = 1;
		}
		else if ((double)num >= (double)maxRewardCount * 0.1)
		{
			num2 = 2;
		}
		for (int num3 = 0; num3 < shineEffects.Count; num3++)
		{
			if (num3 != num2)
			{
				shineEffects[num3].Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			}
			else
			{
				shineEffects[num3].Play();
			}
		}
		StopIdleSound();
		idleInstance = ServiceObject.SoundManager.PlayOneShotAttached(idleEvents[num2], base.transform);
	}

	public void PlayEndSound()
	{
		if (rewardDataList.Count == PlayerData.GetShadowRewardCount(CurrentRegionName))
		{
			ServiceObject.SoundManager.PlayOneShotAttached(endEvent, base.transform);
		}
	}

	private void StopIdleSound()
	{
		if (idleInstance.isValid())
		{
			idleInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			idleInstance.release();
		}
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = base.transform.position.ToVector2() + new Vector2(0f, -6f) - center;
		return new MapObjectInfo(MapObjectType.SproutStone, position, Name);
	}
}
