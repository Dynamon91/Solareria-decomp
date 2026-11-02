using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;

namespace SPR;

public abstract class ParryActionTask : AttackTask
{
	protected List<Enemy> TargetEnemies;

	private readonly List<IDamageable> damaged = new List<IDamageable>();

	protected string SkillID;

	protected string FlameID;

	protected ParryActionTask(string skillID, string flameID)
	{
		SkillID = skillID;
		FlameID = flameID;
	}

	public void SetEnemies(List<Enemy> enemies)
	{
		TargetEnemies = enemies;
	}

	public void SetSkillID(string skillID)
	{
		SkillID = skillID;
	}

	public void SetFlameID(string flameID)
	{
		FlameID = flameID;
	}

	protected override async UniTask AttackAsync(CancellationToken token)
	{
		damaged.Clear();
		AttackTask.PlayerMover.CancelKnockBack();
		LazySingleton<MessageSystem>.Instance.Publish(new MyosoSupportEvent(MyosoEventState.None, isPyronAction: true));
		await AttackProgressAsync(damaged, token);
	}

	protected abstract UniTask AttackProgressAsync(List<IDamageable> damaged, CancellationToken token);
}
