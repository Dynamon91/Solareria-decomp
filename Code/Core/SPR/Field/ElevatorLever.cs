using Cysharp.Threading.Tasks;
using Library;

namespace SPR.Field;

public class ElevatorLever : InteractableLever
{
	private Elevator elevator;

	private float targetPosY;

	public override string ID => $"ElevatorLever_{base.transform.position.ToVector2Int()}";

	public void Initialize(Elevator elevator, float targetPosY)
	{
		this.elevator = elevator;
		this.targetPosY = targetPosY;
	}

	public override void LoadState(int state)
	{
		base.LoadState(state);
		base.Animator.SetBool(InteractableLever.Lock, value: true);
		InteractDistance = -1f;
	}

	protected override async UniTask OnActivateAsync()
	{
		elevator.SetLeverPosition(47f, targetPosY);
		elevator.StartMove(targetPosY, ignoreWait: true);
		await UniTask.WaitUntil(() => !elevator.IsMoving);
		await ReturnHandleAsync();
	}

	public override void Interact()
	{
		if (CanInteract())
		{
			base.Interact();
		}
	}

	public override void InteractEnter()
	{
		if (!elevator.IsOnSameFloor(targetPosY))
		{
			base.InteractEnter();
		}
	}

	protected virtual bool CanInteract()
	{
		return !elevator.IsOnSameFloor(targetPosY);
	}
}
