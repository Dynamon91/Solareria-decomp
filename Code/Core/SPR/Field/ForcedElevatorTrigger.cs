using Cysharp.Threading.Tasks;

namespace SPR.Field;

public class ForcedElevatorTrigger : ElevatorTrigger
{
	private bool active;

	private void OnEnable()
	{
		active = true;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		active = false;
	}

	protected override void OnPushed()
	{
		base.OnPushed();
		Game.Instance.InputManager.BlockControlForTask(WaitForMoving(), "Wait Elevator Moving").Forget();
	}

	private async UniTask WaitForMoving()
	{
		Game.Instance.UIManager.FieldCanvas.HideAllAsync(instant: false).Forget();
		await UniTask.WaitUntil(() => base.Velocity.y > 0f || !active);
	}
}
