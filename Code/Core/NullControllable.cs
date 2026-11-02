using Cysharp.Threading.Tasks;

public class NullControllable : IControllable
{
	public enum Purpose
	{
		Stack,
		Switch,
		Remove,
		JustBlock
	}

	private readonly IControllable _owner;

	private readonly Purpose _purpose;

	private readonly string _originOwnerName;

	public bool IsValid => true;

	public string OwnerName => _owner?.ControllerName() ?? _originOwnerName;

	public Purpose PurposeOfNull => _purpose;

	public NullControllable(IControllable owner, Purpose purpose)
	{
		_owner = owner;
		_purpose = purpose;
		_originOwnerName = owner.ControllerName();
	}

	public NullControllable(string ownerName)
	{
		_owner = null;
		_purpose = Purpose.JustBlock;
		_originOwnerName = ownerName;
	}

	public string ControllerName()
	{
		return "NullControllable(To " + _purpose.ToString() + " " + OwnerName + ")";
	}

	public async UniTask Activate()
	{
		await UniTask.CompletedTask;
	}

	public void Control()
	{
	}

	public async UniTask Pause()
	{
		await UniTask.CompletedTask;
	}

	public async UniTask Restart()
	{
		await UniTask.CompletedTask;
	}

	public async UniTask Deactivate()
	{
		await UniTask.CompletedTask;
	}
}
