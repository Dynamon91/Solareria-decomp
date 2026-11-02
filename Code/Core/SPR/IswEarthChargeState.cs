using Cysharp.Threading.Tasks;
using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Library;
using Spine;
using UnityEngine;

namespace SPR;

public class IswEarthChargeState : CreatureState
{
	private readonly float speed;

	private readonly float delaySeconds;

	public IswEarthChargeState(Creature enemy, float speed, float delaySeconds)
		: base(enemy)
	{
		this.speed = speed;
		this.delaySeconds = delaySeconds;
	}

	public override void Enter(IState prevState)
	{
		base.Creature.DisableVFXAll();
		base.Creature.SetAnimation("attack_3_earth_loop");
		base.Creature.AddAnimationEvent(SpineObject.AnimState.Complete, OnChargeComplete);
		EnemyState.Player.LookAt(base.Creature.Position);
	}

	public override void Exit(IState nextState)
	{
		base.Creature.RemoveAnimationEvent(SpineObject.AnimState.Complete, OnChargeComplete);
	}

	public override void FixedUpdate(float fdt)
	{
		if (base.Creature.GetCurrentAnim(0).Name == "attack_3_earth_end")
		{
			return;
		}
		int signX = base.Creature.FacingDirection.GetSignX();
		Vector3 boneWorldPosition = base.Creature.GetBoneWorldPosition("FingerR_center");
		PathFinder.Node node = PathFinder.FindNode(base.Creature.transform.position).Shift(signX, Mathf.RoundToInt((float)base.Creature.DetectPathWidth * 2f));
		if (node.Position.x <= (float)base.Creature.MyArea.AreaBox.LowerLeft.x + (float)base.Creature.DetectPathWidth / 4f || node.Position.x >= (float)base.Creature.MyArea.AreaBox.UpperRight.x - (float)base.Creature.DetectPathWidth / 4f)
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(boneWorldPosition, new Vector2(signX, 0f), float.PositiveInfinity, LayerUtility.PlatformLayer);
			PoolingParticle poolingParticle = Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>("Infected_Spirit_Earth_attack04_Grab_Hit.prefab", raycastHit2D.point);
			poolingParticle.transform.Flip(base.Creature.FacingDirection.GetSignX());
			poolingParticle.Play();
			base.Creature.SetAnimation("attack_3_earth_end", loop: false);
			if (EnemyState.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
			{
				playerStaggerHitState.TargetPosition = raycastHit2D.point + EnemyState.Player.FacingDirection.ToVector2();
			}
			FallingAfterDelay(delaySeconds).Forget();
		}
		else
		{
			base.Creature.MoveX(speed, fdt, moveForward: true);
			boneWorldPosition = base.Creature.GetBoneWorldPosition("FingerR_center");
			if (EnemyState.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState2)
			{
				playerStaggerHitState2.TargetPosition = boneWorldPosition;
			}
		}
	}

	private void OnChargeComplete(TrackEntry track)
	{
		if (track.Animation.Name == "attack_3_earth_end")
		{
			ChangeState(base.Creature.DefaultState);
		}
	}

	private async UniTaskVoid FallingAfterDelay(float seconds)
	{
		EnemyState.Player.TryTakeDamage(new HitInfo(base.Enemy, "Attack3_Charge"));
		await UniTask.WaitForSeconds(seconds);
		EnemyState.Player.ResetLastHitDirection();
		EnemyState.Player.PSM.DownState.CastObject<PlayerDownState>().DownForwardCurrentPosition = true;
		EnemyState.Player.PSM.ChangeState(EnemyState.Player.PSM.DownState);
	}
}
