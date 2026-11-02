using Cysharp.Threading.Tasks;
using SPR.UI.Field;

namespace SPR.UI;

public class LevelUpEffectProducer : UIEffectProducer
{
	private int pointCount;

	private static ItemBoard ItemBoard => Game.Instance.UIManager.FieldCanvas.ItemBoard;

	public void SetData(int levelDiff)
	{
		pointCount = levelDiff;
	}

	protected override async UniTask OnStartTimeline()
	{
		ItemBoard.Skip();
		await UniTask.CompletedTask;
	}

	protected override async UniTask OnEndTimeline()
	{
		await UniTask.CompletedTask;
	}

	protected override async UniTask OnSkipTimeline()
	{
		await UniTask.CompletedTask;
	}

	public void ShowItemBoard()
	{
		ItemBoard.ShowLevelUpInflareImmediately(pointCount);
	}

	public void ActivateItemEffect()
	{
		ItemBoard.ActivateInflareEffect();
	}
}
