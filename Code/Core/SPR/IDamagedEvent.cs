namespace SPR;

public interface IDamagedEvent
{
	void OnDamaged(IAttackSource source, int damage);
}
