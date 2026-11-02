using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.Field;

public class OhnPile : StateSaveBreakableObject
{
	private enum Type
	{
		Small,
		Middle,
		Large
	}

	[SerializeField]
	private int totalOhn;

	[SerializeField]
	private Type type;

	[SerializeField]
	private ParticleSystem glowParticle;

	private Sequence visualKnockBackSequence;

	private Vector2 originLocalPosition;

	private const string IdleAnimation = "idle";

	private const string DamagedFirstAnimation = "hit_1";

	private const string DamagedSecondAnimation = "hit_2";

	private const string DestroyingAnimation = "break";

	private static readonly int FillPhase = Shader.PropertyToID("_FillPhase");

	private MeshRenderer meshRenderer;

	protected override int BrokenState => MaxState;

	private int MaxState
	{
		get
		{
			if (base.OriginalHitCount > 0)
			{
				return base.OriginalHitCount;
			}
			return type switch
			{
				Type.Small => 2, 
				Type.Middle => 3, 
				Type.Large => 5, 
				_ => 1, 
			};
		}
	}

	private int MaxOhn
	{
		get
		{
			int num = ((totalOhn <= 0) ? (type switch
			{
				Type.Small => 15, 
				Type.Middle => 50, 
				Type.Large => 100, 
				_ => 1, 
			}) : totalOhn);
			return Mathf.CeilToInt((float)num * Game.Instance.Player.PlayerStat.GetStat<CollectingPower>().Rate);
		}
	}

	public override string ID => Util.GetID(Game.FieldID, "OhnPile", base.transform.position.ToVector2Int());

	protected override string BreakAnimationName => "break";

	public override int HitCount => MaxState;

	private MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = GetComponentInChildren<MeshRenderer>());

	private void Start()
	{
		GetComponentInChildren<ParticleSystem>(includeInactive: true).Stop();
		originLocalPosition = base.SkeletonAnimation.transform.localPosition;
	}

	protected override void OnDamaged()
	{
		ChangeState(Count);
		PlayerMover mover = Game.Instance.Player.mover;
		Direction direction = ((!((base.VisualCenter - mover.VisualCenter).x < 0f)) ? Direction.Right : Direction.Left);
		VisualKnockBack(direction);
		DamageEffectAsync().Forget();
		foreach (int randomAmount in Money.GetRandomAmounts(CalculateOhn(Count)))
		{
			Money money = Game.Instance.ObjectPoolManager.GetObject<Money>("Money.prefab", base.VisualCenter);
			money.Initialize(randomAmount);
			money.gameObject.SetActive(value: true);
		}
	}

	protected override void OnBreak()
	{
		glowParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	protected override void OnLoadBrokenState()
	{
		base.gameObject.SetActive(value: false);
	}

	protected override void OnLoadNormalState()
	{
		OnChangeState();
	}

	protected override void OnChangeState()
	{
		string animationName = ((Count == 0) ? "idle" : ((Count <= MaxState / 2) ? "hit_1" : "hit_2"));
		base.SkeletonAnimation.SetAnimation(animationName, 0, Count == 0);
	}

	private int CalculateOhn(int stateAfterHit)
	{
		int num = MaxOhn / 3;
		int num2 = 0;
		if (MaxState > 1)
		{
			int num3 = ((num % (MaxState - 1) != 0) ? 1 : 0);
			num2 = num / (MaxState - 1) + num3;
		}
		int result = MaxOhn - num2 * (MaxState - 1);
		if (stateAfterHit != BrokenState)
		{
			return num2;
		}
		return result;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.transform.position = base.transform.position.RoundToInt();
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
		Transform tr = base.SkeletonAnimation.transform;
		visualKnockBackSequence.Append(tr.DOLocalMoveX(0.3f * direction.ToVector2().x, 1f / 30f).SetEase(Ease.Linear)).Append(tr.DOLocalMoveX(0f, 1f / 30f).SetEase(Ease.Linear)).OnKill(delegate
		{
			tr.localPosition = originLocalPosition;
		});
	}
}
