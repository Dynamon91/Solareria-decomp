using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using Library;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

[RequireComponent(typeof(Collider2D))]
public class BreakableObject : MonoBehaviour, IDamageable, IUnclimbable
{
	[SerializeField]
	protected SoundManager.MaterialType materialType;

	[SerializeField]
	private int hitCount;

	private Collider2D collider2D;

	private SkeletonAnimation skeletonAnimation;

	protected const string EventParameterName = "Breakable_Material";

	protected int Count;

	private ParticleSystem breakParticle;

	protected CancellationToken EnableToken;

	private CancellationTokenSource enableTokenSource;

	public virtual HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position;

	protected Collider2D Collider2D => collider2D ?? (collider2D = GetComponent<Collider2D>());

	protected SkeletonAnimation SkeletonAnimation => skeletonAnimation ?? (skeletonAnimation = GetComponentInChildren<SkeletonAnimation>());

	private SkeletonData SkeletonData => SkeletonAnimation.Skeleton.Data;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected virtual string BreakAnimationName => "break";

	protected int OriginalHitCount => hitCount;

	public virtual int HitCount => OriginalHitCount;

	private void Awake()
	{
		EnableToken = TaskUtil.RefreshToken(ref enableTokenSource);
		Initialize();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			if (base.transform.GetChild(i).TryGetComponent<ParticleSystem>(out var component))
			{
				breakParticle = component;
				break;
			}
		}
	}

	protected virtual void Initialize()
	{
		Count = 0;
		Collider2D.enabled = true;
	}

	public virtual bool TryTakeDamage(HitInfo hitInfo)
	{
		Count++;
		OnDamaged();
		if (Count < HitCount)
		{
			PlayFXsAsync(isBreak: false).Forget();
		}
		else
		{
			Break();
		}
		return true;
	}

	protected void Break()
	{
		OnBreak();
		Collider2D.enabled = false;
		new List<UniTask>
		{
			PlayBreakAnimationAsync(),
			PlayFXsAsync(isBreak: true)
		};
	}

	protected virtual void OnDamaged()
	{
	}

	protected virtual void OnBreak()
	{
	}

	protected virtual async UniTask PlayBreakAnimationAsync()
	{
		if (SkeletonData.FindAnimation(BreakAnimationName) == null)
		{
			SkeletonAnimation.gameObject.SetActive(value: false);
			return;
		}
		base.transform.rotation = Quaternion.identity;
		SkeletonAnimation.AnimationState.SetAnimation(0, BreakAnimationName, loop: false);
		await UniTask.WaitUntil(() => SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete, PlayerLoopTiming.Update, EnableToken);
	}

	private async UniTask PlayFXsAsync(bool isBreak)
	{
		PlaySfx(isBreak);
		await breakParticle.PlayAndWaitAsync(enableTokenSource.Token);
	}

	protected virtual void PlaySfx(bool isBreak)
	{
		string path = (isBreak ? "event:/SFX/Object/Breakable/SFX_Breakable_Impact" : "event:/SFX/Object/Breakable/SFX_Breakable_Debris");
		SoundManager.PlayOneShotAttached(path, base.transform).setParameterByName("Breakable_Material", (float)materialType);
	}

	protected virtual void OnDrawGizmosSelected()
	{
	}

	protected virtual void OnDisable()
	{
		enableTokenSource?.Cancel();
	}
}
