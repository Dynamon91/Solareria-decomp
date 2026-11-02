using Cysharp.Threading.Tasks;
using Library;

namespace SPR;

public class ExecutionStatusEffect : StatusEffect
{
	private enum State
	{
		Ready,
		Execution,
		Finished
	}

	private Enemy enemy;

	private State state;

	public override StatusEffectType EffectType { get; protected set; } = StatusEffectType.Execution;

	public override bool IsStackable => false;

	public bool OnExecution => state == State.Execution;

	public bool IsFinished => state == State.Finished;

	public ExecutionStatusEffect(IDamageable target, int durationFrame)
		: base(target, durationFrame)
	{
	}

	protected override void ApplyEffect()
	{
	}

	public override void OnAdd()
	{
		enemy = Target.CastObject<Enemy>();
		enemy.ActivateExecuteLine(value: true);
		state = State.Ready;
	}

	public override void Update(StatusEffect elementEffect)
	{
	}

	protected override void OnRunning()
	{
		if (state == State.Ready && (float)enemy.Health <= (float)enemy.MaxHealth * 0.05f)
		{
			state = State.Execution;
			ExecutionAsync().Forget();
		}
	}

	public override void OnRemove(StatusEffectController statusEffectController, bool forceRemove)
	{
		enemy.ActivateExecuteLine(value: false);
	}

	private async UniTask ExecutionAsync()
	{
		enemy.EnableHitBox(value: false);
		enemy.OnEnterExecution();
		Game.Instance.Player.mover.EnableVFX("burst_execution");
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(110);
		enemy.EnableVFX("burst_execution_attack");
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(10);
		enemy.EnableHitBox(value: true);
		enemy.OnExitExecution();
		state = State.Finished;
		enemy.TryTakeDamage(new HitInfo(enemy, enemy.MaxHealth, 0f, 0f, AttackDirection.Both, AttackAttribute.Default | AttackAttribute.BlockStuck));
	}
}
