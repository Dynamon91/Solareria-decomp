using Spine.Unity;

namespace SPR.Field;

public class FloatingLotus : FloatingObject
{
	private SkeletonAnimation skeletonAnimation;

	private static PlayerController Player => Game.Instance.Player;

	private SkeletonAnimation SkeletonAnimation => skeletonAnimation ?? (skeletonAnimation = GetComponentInChildren<SkeletonAnimation>());

	public override SoundManager.GroundMaterialType GroundMaterial => SoundManager.GroundMaterialType.Grass;

	protected override void Start()
	{
		base.Start();
		SkeletonAnimation.AnimationState.SetAnimation(0, "close_loop", loop: true);
	}

	public override void OnStartRide()
	{
		base.OnStartRide();
		SkeletonAnimation.AnimationState.SetAnimation(0, "active", loop: false);
		SkeletonAnimation.AnimationState.AddAnimation(0, "close_loop", loop: true, 0f);
		Player.mover.TakeHighJump(PlayerControlParameter.Instance.GroggyJumpSpeed);
	}
}
