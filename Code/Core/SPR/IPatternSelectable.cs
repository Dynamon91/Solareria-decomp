namespace SPR;

public interface IPatternSelectable
{
	void AddFatigue(float value);

	void ResetOtherAttackSequences(BossAttackSequence currentSequence);
}
