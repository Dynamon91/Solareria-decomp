using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using SPR.FSM;
using UnityEngine;

namespace SPR;

public class PlayerFastDownState : PlayerUpgradeableState
{
	private float landingHardTime;

	private float elapsedTime;

	private readonly int startFrame;

	private const string SkillDataID = "fast_down";

	private const string InAirID = "fast_down_air";

	private const string StartAnimName = "fast_descent_start";

	private const string LoopAnimName = "fast_descent_loop";

	private const string EndAnimName = "fast_descent_end";

	private const string EndHardVFXName = "fast_descent_end_hard";

	private const int LandingHardDistance = 39;

	public override bool CanExitOnSceneLoading => false;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerFastDownState(PlayerStateMachine machine)
		: base(machine)
	{
		startFrame = Util.SecondsToFrame(PlayerMover.GetAnimDuration("fast_descent_start"));
	}

	protected override void OnEnter(IState prevState)
	{
		PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(-PlayerAsyncState.pcp.fastDownStamina);
		int fastDownSpeed = PlayerControlParameter.Instance.fastDownSpeed;
		elapsedTime = 0f;
		landingHardTime = 39f / (float)fastDownSpeed;
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		PlayerMover.ApplyRelativeVelocity(Direction.Down, fastDownSpeed, faceJump: false);
		if (base.IsUpgraded)
		{
			PlayerMover.SetSkin("spear");
		}
		PlayerMover.SetAnimation("fast_descent_start", loop: false);
		SoundManager.PlayOneShot(base.IsUpgraded ? "event:/SFX/Player/Skill/SFX_Player_PowerDescend" : "event:/SFX/Player/Skill/SFX_Player_RapidDescent");
	}

	protected override void OnFixedUpdate(float fdt)
	{
		elapsedTime += fdt;
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.ClearDamagedList();
		for (int i = 0; i < startFrame; i++)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			if (base.IsUpgraded)
			{
				PlayerMover.HandleAttackHitBox("fast_down_air", 0, AttackAttribute.Default | AttackAttribute.DestroyWall);
			}
			await PlayerUniTaskUtil.WaitOneFrame();
		}
		if (token.IsCancellationRequested)
		{
			return;
		}
		if (base.IsUpgraded)
		{
			PlayerMover.HandleAttackHitBox("fast_down_air", 0, AttackAttribute.Default | AttackAttribute.DestroyWall);
		}
		if (!PlayerMover.IsOnGround)
		{
			PlayerMover.SetAnimation("fast_descent_loop");
		}
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			if (PlayerState.PlayerController.IsInSceneLoadSequence)
			{
				PlayerMover.ApplyRelativeVelocity(Direction.Down, PlayerControlParameter.Instance.TerminalSpeed, faceJump: false);
			}
			if (!PlayerState.PlayerController.IsInSceneLoadSequence && !psm.StateOption.HasFlag(StateOption.GravityFree))
			{
				psm.SetStateOptionActive(StateOption.GravityFree, active: true);
				PlayerMover.ApplyRelativeVelocity(Direction.Down, PlayerControlParameter.Instance.fastDownSpeed, faceJump: false);
			}
			if (base.IsUpgraded)
			{
				PlayerMover.HandleAttackHitBox("fast_down_air", 0, AttackAttribute.Default | AttackAttribute.DestroyWall);
			}
			if (PlayerMover.IsOnGround)
			{
				break;
			}
			await PlayerUniTaskUtil.WaitOneFrame();
		}
		PlayerMover.SetAnimation("fast_descent_end", loop: false, 1f, 0, 0f, vfxEnable: false);
		SoundManager.PlayOneShot(base.IsUpgraded ? "event:/SFX/Player/Skill/SFX_Player_PowerDescend_Landing" : "event:/SFX/Player/Skill/SFX_Player_RapidDescent_Landing");
		bool flag = elapsedTime >= landingHardTime;
		if (base.IsUpgraded)
		{
			PlayerMover.EnableVFX(flag ? "fast_descent_end_hard" : "fast_descent_end");
			PlayerMover.ClearDamagedList();
			PlayerMover.HandleAttackHitBox("fast_down", flag ? 1 : 0, AttackAttribute.Default | AttackAttribute.DestroyWall);
		}
		if (!flag)
		{
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.fastDownPostdelayFrame, token);
		}
		else
		{
			foreach (IDamageable item in PlayerMover.Damaged)
			{
				if (item is Creature creature)
				{
					creature.KnockBackAsync(direction: new Vector2((creature.VisualCenter.x - PlayerMover.VisualCenter.x).GetSign(), 0f), frame: PlayerAsyncState.pcp.fastDownHardKnockbackPower).Forget();
				}
			}
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(5, token);
			PlayerMover.SetAnimation("landing", loop: false);
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.fastDownHardPostdelayFrame - 5, token);
		}
		if (!token.IsCancellationRequested)
		{
			psm.ChangeState(psm.IdleState);
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
		if (base.IsUpgraded)
		{
			PlayerMover.SetSkin("player");
		}
	}
}
