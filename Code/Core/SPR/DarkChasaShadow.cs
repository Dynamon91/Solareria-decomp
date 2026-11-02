using System;
using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.FSM;
using SPR.Field;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class DarkChasaShadow : Enemy, IPoolableObject
{
	private const string TeleportDisappear = "DarkChasa_Shadow_Teleport_Disappear.prefab";

	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<DarkChasaShadow> poseA;

	[SerializeField]
	private GenericAttackSequence<DarkChasaShadow> poseB;

	[SerializeField]
	private GenericAttackSequence<DarkChasaShadow> slashShadowAbove;

	[SerializeField]
	private GenericAttackSequence<DarkChasaShadow> slashShadowCenter;

	[SerializeField]
	private GenericAttackSequence<DarkChasaShadow> pincer;

	[SerializeField]
	private GenericAttackSequence<DarkChasaShadow> pierce;

	[SerializeField]
	private GenericAttackSequence<DarkChasaShadow> comboCSideSlash;

	private DarkChasa darkChasa;

	public override string Name => "DarkChasa_Shadow";

	public string PrefabKey => "Dark Chasa_Shadow.prefab";

	public GameObject GameObject => base.gameObject;

	public override EnemyState DefaultState => null;

	public override HitType HitType => HitType.None;

	public override Area MyArea => darkChasa.MyArea;

	public override Vector2 GroundPosition => darkChasa.SpawnPosition;

	protected override float Width => base.Width * 2f;

	protected override void Awake()
	{
		base.Awake();
		AddAction(ActionTiming.Begin, "SlashAboveMove", delegate(int duration, Parameter[] p)
		{
			SlashAboveMove(duration, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "TeleportToPlayer", (Action<int, Parameter[]>)delegate
		{
			SetVisible(visible: false);
		});
		AddAction(ActionTiming.End, "TeleportToPlayer", delegate(Parameter[] p)
		{
			TeleportAroundPlayer(p[0].Float);
		});
		TimelineBeginActions.Add("JumpToPlayer", delegate(int duration)
		{
			base.transform.DOMove(Enemy.Player.VisualBottom - new Vector2(0f, HitBox.size.y / 2f), Util.FrameToSeconds(duration)).SetEase(Ease.OutQuad);
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
		PlayTeleportEffect("DarkChasa_Shadow_Teleport_Disappear.prefab");
		RemoveAnimation();
		DisableVFXAll();
		base.StateMachine.ChangeState(DefaultState);
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		return false;
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		Vector2 vector = (moveForward ? FacingDirection.ToVector2() : FacingDirection.Opposite().ToVector2());
		base.transform.position += (Vector3)vector * (velocityX * deltaTime);
	}

	public void SetOwner(DarkChasa owner)
	{
		if ((object)darkChasa == null)
		{
			darkChasa = owner;
		}
		LookPlayer();
	}

	public void SlashAbove()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { slashShadowAbove }));
	}

	public void SlashCenter()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { slashShadowCenter }));
	}

	public void Pincer()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { pincer }));
	}

	public void Pierce()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { pierce }));
	}

	public void ComboCSideSlash()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { comboCSideSlash }));
	}

	public void PoseA()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { poseA }));
	}

	public void PoseB()
	{
		base.StateMachine.ChangeState(new ShadowAttackState(this, () => new List<AttackSequence> { poseB }));
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}

	private void SlashAboveMove(int duration, float distance)
	{
		base.transform.DOMove(new Vector3(Enemy.Player.VisualBottom.x + (float)FacingDirection.GetSignX() * distance, darkChasa.SpawnPosition.y), Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void TeleportAroundPlayer(float distance)
	{
		int num = -FacingDirection.GetSignX();
		num = ((num != 0) ? num : (MyArea.AreaBox.Center - VisualCenter).normalized.x.GetSign());
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x + (float)num * distance, darkChasa.SpawnPosition.y);
		SetVisible(visible: true);
	}

	private void PlayTeleportEffect(string address)
	{
		Enemy.PoolManager.GetObject<PoolingParticle>(address, GetBoneWorldPosition("Body_0")).Play();
	}
}
