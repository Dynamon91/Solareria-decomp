using Cysharp.Threading.Tasks;
using Library;
using SPR.Achievement;
using SPR.UI.Map;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class ShadowSprout : StateSaveBreakableObject, IMappable
{
	private class SafeParticleSystem
	{
		private ParticleSystem particleReference;

		public SafeParticleSystem(ParticleSystem particle)
		{
			particleReference = particle;
		}

		public void SetActive(bool value)
		{
			if (!particleReference.IsNull())
			{
				particleReference.gameObject.SetActive(value);
			}
		}

		public void SetLoop(bool value)
		{
			if (!particleReference.IsNull())
			{
				ParticleSystem.MainModule main = particleReference.main;
				main.loop = value;
			}
		}
	}

	[Header("References")]
	[SerializeField]
	private ParticleSystem loopParticle;

	[SerializeField]
	private ParticleSystem hitParticle;

	[SerializeField]
	private ParticleSystem burstParticle;

	[SerializeField]
	private SkeletonAnimation vineAnimation;

	private SafeParticleSystem loopParticleSystem;

	private SafeParticleSystem hitParticleSystem;

	private SafeParticleSystem burstParticleSystem;

	public static string ItemName => CurrentRegionName.ToString() + "Sprout";

	private static RegionName CurrentRegionName => Game.FieldID.GetRegionName();

	public override string ID => Util.GetID(Game.FieldID, "PlagueSprout");

	protected override string BreakAnimationName => "burst";

	protected override void Initialize()
	{
		loopParticleSystem = new SafeParticleSystem(loopParticle);
		hitParticleSystem = new SafeParticleSystem(hitParticle);
		burstParticleSystem = new SafeParticleSystem(burstParticle);
	}

	private void Update()
	{
		Util.CheckShownInCamera(ID, base.transform.position);
	}

	protected override void OnLoadBrokenState()
	{
		loopParticleSystem.SetActive(value: false);
		TrackEntry trackEntry = base.SkeletonAnimation.AnimationState.SetAnimation(0, BreakAnimationName, loop: false);
		trackEntry.TrackTime = trackEntry.AnimationEnd;
		if (!vineAnimation.IsNull())
		{
			vineAnimation.AnimationState.SetAnimation(0, BreakAnimationName, loop: false).TrackTime = trackEntry.AnimationEnd;
		}
	}

	protected override void OnLoadNormalState()
	{
		base.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
		if (!vineAnimation.IsNull())
		{
			vineAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
		}
	}

	protected override void OnDamaged()
	{
		base.OnDamaged();
		hitParticleSystem.SetActive(value: true);
		if (HitCount > 1)
		{
			base.SkeletonAnimation.AnimationState.SetAnimation(0, "hit_blend", loop: false);
			base.SkeletonAnimation.AnimationState.AddAnimation(0, "idle", loop: true, 0f);
		}
	}

	protected override void OnBreak()
	{
		if (!vineAnimation.IsNull())
		{
			vineAnimation.AnimationState.SetAnimation(0, BreakAnimationName, loop: false);
		}
		BreakAsync().Forget();
		async UniTask BreakAsync()
		{
			loopParticleSystem.SetLoop(value: false);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(43);
			burstParticleSystem.SetActive(value: true);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(40);
			new PureItem(ItemName).AddToPlayerData();
			LazySingleton<MessageSystem>.Instance.Publish(new SproutTutorialEvent());
			if (HasAllSprout())
			{
				AchievementHelper.Unlock("GetAllPlagueSprout");
			}
		}
	}

	private bool HasAllSprout()
	{
		bool result = true;
		for (RegionName regionName = RegionName.Terradin; regionName < RegionName.Max; regionName++)
		{
			if (Game.Instance.SaveData.PlayerData.GetShadowSproutCount(regionName) != Util.GetSproutRewardDataList(regionName).Count)
			{
				result = false;
				break;
			}
		}
		return result;
	}

	protected override void PlaySfx(bool isBreak)
	{
		string path = (isBreak ? "event:/SFX/Object/SFX_Object_Plague_Destroy" : "event:/SFX/Object/SFX_Object_Plague_Hit");
		BreakableObject.SoundManager.PlayOneShotAttached(path, base.transform);
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = base.transform.position.ToVector2() - center;
		return new MapObjectInfo(MapObjectType.Sprout, position, BrokenState, ID);
	}
}
