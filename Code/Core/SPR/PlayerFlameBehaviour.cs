using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class PlayerFlameBehaviour : PlayableBehaviour
{
	public GameObject Owner;

	public Enemy Enemy;

	public int DurationFrame;

	public HeatFlame.CurveParameter CurveParameter;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		DurationFrame = Util.SecondsToFrame(playable.GetDuration());
		Vector3 position = Owner.transform.position;
		Enemy = Game.Instance.Player.mover.PSM.HyperBurstInputItem.TargetEnemy;
		ExecutionFlame.ShootFlame(new HeatFlame.FlameParameter(Enemy, 0, 0f, DurationFrame, AttackAttribute.None, position, shouldAttack: false), CurveParameter);
	}
}
