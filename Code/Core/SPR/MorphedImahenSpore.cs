using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Field;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class MorphedImahenSpore : StateSaveBreakableObject
{
	[SerializeField]
	private char postfix;

	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference breakEvent;

	private MeshRenderer meshRenderer;

	private SkeletonAnimation spine;

	private DG.Tweening.Sequence visualKnockBackSequence;

	private static readonly int FillPhase = Shader.PropertyToID("_FillPhase");

	private MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = Spine.GetComponent<MeshRenderer>());

	private SkeletonAnimation Spine => spine ?? (spine = GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	public override string ID => $"{Game.Instance.CurrentFieldID}_{base.gameObject.name}_{base.transform.position.ToVector2Int()}";

	private string IdleAnim => $"{postfix}_idle";

	private string HitAnim => $"{postfix}_hit";

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	protected override UniTask PlayBreakAnimationAsync()
	{
		Spine.SetAnimation($"explode_{postfix}");
		TrackEntry currentAnimState = Spine.AnimationState.GetCurrent(0);
		UniTask.WaitWhile(() => currentAnimState.TrackTime <= currentAnimState.Animation.Duration - 1f / 60f).GetAwaiter().OnCompleted(delegate
		{
			base.gameObject.SetActive(value: false);
		});
		return default(UniTask);
	}

	protected override void OnLoadBrokenState()
	{
		base.gameObject.SetActive(value: false);
	}

	protected override void OnLoadNormalState()
	{
		Spine.SetAnimation(IdleAnim);
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		PoolableTimeLimitObject poolableTimeLimitObject = ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Enemy_Damaged.prefab", base.VisualCenter);
		poolableTimeLimitObject.Initialize("VFX/Enemy_Damaged.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		return base.TryTakeDamage(hitInfo);
	}

	protected override void OnDamaged()
	{
		Spine.SetAnimation(HitAnim, 0, loop: false);
		Spine.AnimationState.AddAnimation(0, IdleAnim, loop: true, 0f);
		ChangeState(Count);
	}

	protected override void PlaySfx(bool isBreak)
	{
		BreakableObject.SoundManager.PlayOneShotAttached(isBreak ? breakEvent : hitEvent, base.transform);
	}

	private async UniTaskVoid DamageEffectAsync()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FillPhase, 1f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
		DG.Tweening.Sequence sequence = DOTween.Sequence();
		sequence.Append(DOFill(materialPropertyBlock, 1f, 5).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed)).Append(DOFill(materialPropertyBlock, 0f, 2).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed));
		await sequence.WaitAsync();
		Tween DOFill(MaterialPropertyBlock materialPropertyBlock2, float endValue, int frame)
		{
			return DOTween.To(() => materialPropertyBlock2.GetFloat(FillPhase), delegate(float x)
			{
				materialPropertyBlock2.SetFloat(FillPhase, x);
				MeshRenderer.SetPropertyBlock(materialPropertyBlock2);
			}, endValue, (float)frame / 60f);
		}
	}

	private void VisualKnockBack(Direction direction)
	{
		visualKnockBackSequence?.Kill();
		visualKnockBackSequence = DOTween.Sequence();
		Transform tr = MeshRenderer.transform;
		Vector3 initialPos = tr.localPosition;
		visualKnockBackSequence.Append(tr.DOLocalMoveX(0.3f * direction.ToVector2().x, 1f / 30f).SetEase(Ease.Linear)).Append(tr.DOLocalMoveX(0f, 1f / 30f).SetEase(Ease.Linear)).OnKill(delegate
		{
			tr.localPosition = initialPos;
		});
	}

	protected override void OnDrawGizmosSelected()
	{
	}
}
