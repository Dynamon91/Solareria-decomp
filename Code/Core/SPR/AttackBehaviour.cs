using System;
using System.Collections.Generic;

namespace SPR;

public struct AttackBehaviour
{
	public event Action<float, List<IDamageable>> Action;

	public AttackBehaviour(Enemy enemy, int radius, HitInfo hitInfo)
	{
		this.Action = delegate(float _, List<IDamageable> attackedList)
		{
			enemy.AttackCircle(radius, hitInfo, attackedList);
		};
	}

	public AttackBehaviour(Enemy enemy, Zone localAttackArea, HitInfo hitInfo, Action<List<IDamageable>> action = null)
	{
		this.Action = delegate(float _, List<IDamageable> attackedList)
		{
			List<IDamageable> obj = enemy.AttackSquare(localAttackArea, hitInfo, attackedList);
			action?.Invoke(obj);
		};
	}

	public AttackBehaviour(Action<float, List<IDamageable>> action)
	{
		this.Action = action;
	}

	public void Invoke(float fdt, List<IDamageable> attackedList)
	{
		this.Action?.Invoke(fdt, attackedList);
	}

	public static AttackBehaviour operator +(AttackBehaviour a, AttackBehaviour b)
	{
		return new AttackBehaviour(delegate(float fdt, List<IDamageable> attackedList)
		{
			a.Action?.Invoke(fdt, attackedList);
			b.Action?.Invoke(fdt, attackedList);
		});
	}

	public static AttackBehaviour operator +(AttackBehaviour a, Action b)
	{
		return new AttackBehaviour(delegate(float fdt, List<IDamageable> attackedList)
		{
			a.Action?.Invoke(fdt, attackedList);
			b?.Invoke();
		});
	}
}
