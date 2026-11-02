using System.Threading;
using Cysharp.Threading.Tasks;

namespace SPR;

public abstract class AttackTask
{
	public struct AttackParameter
	{
		public readonly string AnimName;

		public readonly string DataID;

		public readonly int Index;

		public readonly (int min, int max) AttackFrameRange;

		public readonly AttackAttribute AttackAttribute;

		public AttackParameter(string animName, string dataID, int index, (int, int) attackFrameRange, AttackAttribute attackAttribute)
		{
			AnimName = animName;
			DataID = dataID;
			Index = index;
			AttackFrameRange = attackFrameRange;
			AttackAttribute = attackAttribute;
		}
	}

	private bool isPending;

	public abstract StateOption StateOption { get; }

	protected static PlayerController PlayerController => Game.Instance.Player;

	protected static PlayerMover PlayerMover => Game.Instance.Player.mover;

	protected static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public void FixedUpdate()
	{
		if (IsValid())
		{
			OnFixedUpdate();
		}
	}

	public virtual bool IsValid()
	{
		return isPending;
	}

	protected abstract void OnFixedUpdate();

	public async UniTask Attack(CancellationToken token)
	{
		isPending = true;
		await AttackAsync(token);
		isPending = false;
	}

	protected abstract UniTask AttackAsync(CancellationToken token);
}
