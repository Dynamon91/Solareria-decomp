using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Sharg : Creature
{
	[Header("공격 패턴")]
	[SerializeField]
	private GenericAttackSequence<Sharg> glideAttack;

	[SerializeField]
	private GenericAttackSequence<Sharg> tailAttack;

	[Header("점프")]
	[SerializeField]
	private float jumpHeight;

	[SerializeField]
	private int jumpDuration;

	private Vector2 lastDestination;

	public override string Name => "Sharg";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(glideAttack, () => new List<AttackSequence> { glideAttack, tailAttack });
		AddAction(ActionTiming.Begin, "Jump", Jump);
		AddAction(ActionTiming.Begin, "GlideToPlayer", delegate(int duration, Parameter[] p)
		{
			GlideToPlayer(duration);
		});
		AddAction(ActionTiming.OnFrame, "UpdateGlideDestination", delegate(Parameter[] p)
		{
			UpdateGlideDestination(p[0].Float);
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { glideAttack, tailAttack };
	}

	protected override void OnDie()
	{
		DOTween.Kill(base.transform);
		base.OnDie();
		ForceDrop(Physics2D.Raycast(VisualCenter, Vector2.down, float.MaxValue, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer).point.y, 60f);
	}

	private void Jump()
	{
		lastDestination = VisualBottom;
		DOTween.Kill(base.transform);
		base.transform.DOJump(new Vector3(VisualBottom.x, VisualBottom.y + jumpHeight), jumpHeight, 1, Util.FrameToSeconds(jumpDuration)).SetEase(Ease.Linear);
	}

	private void GlideToPlayer(int duration)
	{
		base.transform.DOMove(new Vector3(lastDestination.x, lastDestination.y), Util.FrameToSeconds(duration)).SetEase(Ease.OutQuart).OnComplete(base.SetConnectedPath);
	}

	private void UpdateGlideDestination(float distance)
	{
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		float distance2 = Vector2.Distance(VisualCenter, Enemy.Player.VisualCenter);
		if (!Physics2D.Raycast(VisualCenter, normalized, distance2, LayerUtility.PlatformLayer))
		{
			PathFinder.Node node = PathFinder.FindNode(Enemy.Player.VisualBottom, 0f);
			lastDestination = node.Shift(FacingDirection.GetSignX(), Mathf.RoundToInt(distance)).Position;
		}
	}
}
