using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.Field;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class WaterSpearman : Creature
{
	[Header("패턴")]
	[SerializeField]
	private GenericAttackSequence<WaterSpearman> attack;

	public override string Name => "WaterSpearman";

	public override Area MyArea => GetComponentInParent<Area>();

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(attack, () => new List<AttackSequence> { attack });
		AddAction(ActionTiming.Begin, "ThrowJavelin", delegate(Parameter[] p)
		{
			ThrowJavelin(p[0].Float);
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	private void ThrowJavelin(float speed)
	{
		Vector2 vector = GetBoneWorldPosition("Spear");
		WaterSpearmanJavelin javelin = Enemy.PoolManager.GetObject<WaterSpearmanJavelin>("WaterSpearmanJavelin.prefab", vector);
		Vector2 vector2 = Enemy.Player.VisualCenter + new Vector2(0f, Enemy.Player.ColliderSize.y / 2f) - vector;
		if (vector2.x * (float)FacingDirection.GetSignX() < 0f)
		{
			vector2.x = FacingDirection.GetSignX() * 20;
		}
		Vector2 normalized = vector2.normalized;
		javelin.Throw(normalized, speed);
		javelin.OnReturn += delegate
		{
			DOTween.Kill(javelin.transform);
		};
		RaycastHit2D hit = Physics2D.Raycast(vector, normalized, float.PositiveInfinity, LayerUtility.PlatformLayer);
		Vector2 vector3 = (hit ? hit.point : (vector + normalized * 999f));
		javelin.transform.DOMove(vector3, speed).SetUpdate(UpdateType.Fixed).SetSpeedBased(isSpeedBased: true)
			.OnUpdate(delegate
			{
				if (!MyArea.AreaBox.IsInArea(javelin.transform.position))
				{
					DOTween.Kill(javelin.transform);
					javelin.ReturnToPool();
				}
				javelin.HandleOverlap(javelin.transform.position, javelin.OverlapSize);
			})
			.OnComplete(delegate
			{
				javelin.OnPlatform(hit);
			});
	}
}
