namespace SPR.Field;

public class FastDownJumperForSolinLab : FastDownJumper
{
	protected override int JumpSpeed => PlayerControlParameter.Instance.HighJumpSpeedForSolinLab;
}
