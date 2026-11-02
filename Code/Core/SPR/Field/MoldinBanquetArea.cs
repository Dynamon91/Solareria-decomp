namespace SPR.Field;

public class MoldinBanquetArea : SubArea
{
	protected override bool PlayBGMCondition => !Game.Instance.SaveData.EnemyFightData.IsBossCleared(BossName.Moldin);
}
