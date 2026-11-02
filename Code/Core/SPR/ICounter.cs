namespace SPR;

public interface ICounter
{
	PlayerCounterAttack CounterAttack { get; set; }

	void OnParried(IDamageable damageable);

	int GetStunCount(Enemy enemy, AttackAttribute attackAttribute);

	void ChangeID(string id);
}
