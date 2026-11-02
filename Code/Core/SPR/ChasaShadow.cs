using System;
using System.Collections.Generic;
using Library;
using SPR.FSM;
using SPR.Field;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class ChasaShadow : Enemy, IPoolableObject
{
	[Header("패턴")]
	[SerializeField]
	private GenericAttackSequence<ChasaShadow> poseA;

	[SerializeField]
	private GenericAttackSequence<ChasaShadow> poseY;

	private Area area;

	public override string Name => "ChasaShadow";

	public override EnemyState DefaultState => null;

	public override HitType HitType => HitType.Platform;

	public string PrefabKey => "Chasa_Shadow.prefab";

	public GameObject GameObject => base.gameObject;

	public override Area MyArea => area;

	protected override float Width => base.Width * 2f;

	protected override void Awake()
	{
		base.Awake();
		AddAction(ActionTiming.Begin, "TeleportAroundPlayer", (Action<int, Parameter[]>)delegate
		{
			SetVisible(visible: false);
		});
		AddAction(ActionTiming.End, "TeleportAroundPlayer", delegate(Parameter[] p)
		{
			TeleportAroundPlayer(p[0].Float);
		});
	}

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { poseA, poseY };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		return false;
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		Vector2 vector = (moveForward ? FacingDirection.ToVector2() : FacingDirection.Opposite().ToVector2());
		base.transform.position += (Vector3)vector * (velocityX * Time.fixedDeltaTime);
	}

	private void TeleportAroundPlayer(float distance)
	{
		int num = -(Enemy.Player.VisualBottom - VisualBottom).normalized.x.GetSign();
		num = ((num != 0) ? num : (MyArea.AreaBox.Center - VisualCenter).normalized.x.GetSign());
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x + (float)num * distance, base.SpawnPosition.y);
		SetVisible(visible: true);
	}

	public void PoseYAttack()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { poseY, poseA }));
	}

	public void SetArea(Area area)
	{
		this.area = area;
	}
}
