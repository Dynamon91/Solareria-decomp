using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Spine;
using UnityEngine;

namespace SPR.Field;

public class BreakableWall : StateSaveBreakableObject
{
	[Flags]
	private enum Direction
	{
		None = 0,
		Right = 1,
		Left = 2,
		Up = 4,
		Down = 8
	}

	private enum AllowAttack
	{
		All,
		Player
	}

	[SerializeField]
	private AllowAttack allowAttack;

	[Tooltip("수직 방향일 땐 Right, Left를, 수평 방향일 땐 Up, Down을 사용하세요.")]
	[SerializeField]
	private Direction allowDirection;

	[SerializeField]
	private Fog fog;

	[SerializeField]
	private ParticleSystem idleParticle;

	private bool isHorizontal;

	private DG.Tweening.Sequence visualKnockBackSequence;

	private BoxCollider2D box2D;

	private readonly List<Vector2> groundPositions = new List<Vector2>();

	private static PlayerController Player => Game.Instance.Player;

	public override string ID => $"BreakableWall_{base.transform.position.ToVector2Int()}";

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	protected override void Initialize()
	{
		Bounds bounds = base.Collider2D.bounds;
		isHorizontal = bounds.size.x > bounds.size.y;
		SetVisual();
		Vector2 vector = (Vector2)base.transform.position - Box2D.size / 2f;
		Vector2 vector2 = (Vector2)base.transform.position + new Vector2(Box2D.size.x, 0f - Box2D.size.y) / 2f;
		if (Box2D.size.x % 2f == 0f)
		{
			vector.x += 1f;
		}
		for (float num = vector.x; num <= vector2.x; num += 1f)
		{
			groundPositions.Add(new Vector2(num, vector.y));
		}
	}

	private void SetVisual()
	{
		if (IsOneSide())
		{
			Direction direction = allowDirection;
			if (direction == Direction.Right || direction == Direction.Up)
			{
				if ((object)base.SkeletonAnimation == null)
				{
					base.transform.localScale = new Vector3(-1f, 1f, 1f);
				}
				else
				{
					base.SkeletonAnimation.transform.localScale = new Vector3(-1f, 1f, 1f);
				}
			}
		}
		if (isHorizontal && base.SkeletonAnimation != null)
		{
			Vector3 localScale = base.SkeletonAnimation.transform.localScale;
			localScale.y = 1.05f;
			base.SkeletonAnimation.transform.localScale = localScale;
		}
	}

	protected override void OnLoadBrokenState()
	{
		ModifyCreaturePath(setGround: true);
		TrackEntry trackEntry = base.SkeletonAnimation.AnimationState.SetAnimation(0, BreakAnimationName, loop: false);
		trackEntry.TrackTime = trackEntry.AnimationEnd;
		if (!idleParticle.IsNull())
		{
			idleParticle.Stop();
		}
		if (!fog.IsNull())
		{
			fog.FadeInAsync(instant: true).Forget();
		}
	}

	protected override void OnLoadNormalState()
	{
		ModifyCreaturePath(setGround: false);
		base.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
		if (!fog.IsNull())
		{
			fog.FadeOutAsync(instant: true).Forget();
		}
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		switch (hitInfo.AttackerType)
		{
		case HitType.Player:
		{
			if (isHorizontal && !hitInfo.AttackAttribute.HasFlag(AttackAttribute.DestroyWall))
			{
				return false;
			}
			Vector3 vector = Player.transform.position - base.transform.position;
			if (!allowDirection.HasFlag(VectorToDirection(vector)))
			{
				if (!Player.mover.CanBlockStuck(hitInfo))
				{
					Player.mover.PSM.ChangeState(Player.mover.PSM.AttackStuckState);
					BreakableObject.SoundManager.PlayOneShot("event:/SFX/Boss/Charon/SFX_Charon_Rock_Impact");
				}
				return false;
			}
			break;
		}
		case HitType.Creature:
		{
			if (allowDirection.HasFlag(Direction.Up))
			{
				return false;
			}
			Vector3 vector2 = hitInfo.Source.CastObject<MonoBehaviour>().transform.position - base.transform.position;
			if (!allowDirection.HasFlag(VectorToDirection(vector2)))
			{
				return false;
			}
			if (allowAttack == AllowAttack.All)
			{
				Count += 100;
			}
			break;
		}
		case HitType.Object:
			if (allowAttack == AllowAttack.All)
			{
				Count += 100;
			}
			break;
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override void OnDamaged()
	{
		visualKnockBackSequence?.Kill();
		visualKnockBackSequence = DOTween.Sequence();
		Transform tr = base.SkeletonAnimation.transform;
		Vector2 vector = ((Vector2)tr.position - Player.mover.VisualCenter).ToDirection(includeDiagonal: false).ToVector2();
		visualKnockBackSequence.Append(DOLocalMoveByDirection(0.25f * (isHorizontal ? vector.y : vector.x), 1f / 15f).SetEase(Ease.Linear)).Append(DOLocalMoveByDirection(0f, 1f / 15f).SetEase(Ease.Linear)).OnKill(delegate
		{
			tr.localPosition = Vector3.zero;
		});
		Tween DOLocalMoveByDirection(float endValue, float duration)
		{
			if (!isHorizontal)
			{
				return tr.DOLocalMoveX(endValue, duration);
			}
			return tr.DOLocalMoveY(endValue, duration);
		}
	}

	protected override void OnBreak()
	{
		if (!idleParticle.IsNull())
		{
			idleParticle.Stop();
		}
		if (!fog.IsNull())
		{
			fog.FadeInAsync(instant: false).Forget();
		}
		ChangeState(BrokenState);
		ModifyCreaturePath(setGround: true);
	}

	protected override void PlaySfx(bool isBreak)
	{
		string path = (isBreak ? "event:/SFX/Object/Breakable/SFX_Breakable_Wall" : "event:/SFX/Object/Breakable/SFX_Breakable_Debris");
		BreakableObject.SoundManager.PlayOneShotAttached(path, base.transform);
	}

	private Direction VectorToDirection(Vector2 vector)
	{
		if (isHorizontal)
		{
			if (vector.y != 0f)
			{
				if (!(vector.y > 0f))
				{
					return Direction.Down;
				}
				return Direction.Up;
			}
			return Direction.None;
		}
		if (vector.x != 0f)
		{
			if (!(vector.x > 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
		return Direction.None;
	}

	private bool IsOneSide()
	{
		Direction direction = allowDirection;
		return direction == Direction.Right || direction == Direction.Left || direction == Direction.Up || direction == Direction.Down;
	}

	private void ModifyCreaturePath(bool setGround)
	{
		Game.Instance.FieldController.ModifyPathGraphs(groundPositions, setGround, setGround);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		SetVisual();
	}
}
