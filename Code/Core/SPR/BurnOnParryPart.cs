namespace SPR;

public class BurnOnParryPart : SimpleStatEffectPart, IParryEvent
{
	private const string SkillDataID = "part_parry_burn";

	public void OnParry(Enemy enemy)
	{
		Game.Instance.Player.PlayerStat.MakeEnemyBurn(enemy, "part_parry_burn");
	}
}
