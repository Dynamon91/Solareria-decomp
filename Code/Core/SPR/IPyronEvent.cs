using System.Collections.Generic;

namespace SPR;

public interface IPyronEvent
{
	void OnPyronAttack(List<IDamageable> damaged, bool isHeavyPyron);
}
