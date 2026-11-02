using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using LapinLibrary;
using Library;
using SPR.Cutscene;
using SPR.Save;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class DojoDummy : MonoBehaviour, IDamageable, IStateSavable
{
	[SerializeField]
	private ParticleSystem hitVfx;

	[SerializeField]
	private EventReference hitSfx;

	[SerializeField]
	private ParticleSystem activeVfx;

	private int currentState;

	public const int InactiveState = 0;

	public const int ActiveState = 1;

	private ChasaCutsceneProducer producer;

	private MeshRenderer meshRenderer;

	private SkeletonAnimation skeletonAnimation;

	private Sequence visualKnockBackSequence;

	private CancellationTokenSource effectTokenSource;

	protected static readonly string IdleAnimationName = "idle";

	protected static readonly string HitAnimationName = "hit";

	private static readonly int FillPhase = Shader.PropertyToID("_FillPhase");

	public string ID => $"DojoDummy_{base.transform.position.ToVector2Int()}";

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position + activeVfx.transform.localPosition;

	private ChasaCutsceneProducer Producer => producer ?? (producer = GetComponentInParent<Area>().GetComponentInChildren<ChasaCutsceneProducer>());

	private MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = GetComponentInChildren<MeshRenderer>());

	private SkeletonAnimation SkeletonAnimation => skeletonAnimation ?? (skeletonAnimation = GetComponentInChildren<SkeletonAnimation>());

	public void LoadState(int state)
	{
		SetState(state);
	}

	public void ChangeState(int state)
	{
		SetState(state);
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void SetState(int state)
	{
		currentState = state;
		switch (currentState)
		{
		case 0:
			activeVfx.Stop();
			break;
		case 1:
			activeVfx.Play();
			break;
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		TaskUtil.RefreshToken(ref effectTokenSource);
		hitVfx.Play();
		Game.Instance.SoundManager.PlayOneShotAttached(hitSfx, base.transform);
		PlayerMover mover = Game.Instance.Player.mover;
		Direction direction = ((!((VisualCenter - mover.VisualCenter).x < 0f)) ? Direction.Right : Direction.Left);
		PlayBreakAnimationAsync().Forget();
		VisualKnockBack(direction);
		DamageEffectAsync().Forget();
		if (currentState == 0)
		{
			return true;
		}
		ChangeState(1);
		OnActivate();
		return true;
	}

	protected virtual void OnActivate()
	{
	}

	private void OnValidate()
	{
		SetState(currentState);
	}

	protected virtual async UniTask PlayBreakAnimationAsync()
	{
		SkeletonAnimation.AnimationState.SetAnimation(0, HitAnimationName, loop: false);
		await UniTask.WaitUntil(() => SkeletonAnimation.AnimationState.GetCurrent(0).IsComplete, PlayerLoopTiming.Update, effectTokenSource.Token);
		if (!effectTokenSource.IsCancellationRequested)
		{
			SkeletonAnimation.AnimationState.SetAnimation(0, IdleAnimationName, loop: true);
		}
	}

	private async UniTaskVoid DamageEffectAsync()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FillPhase, 1f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
		Sequence sequence = DOTween.Sequence();
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
}
