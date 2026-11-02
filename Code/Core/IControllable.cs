using Cysharp.Threading.Tasks;

public interface IControllable
{
	bool IsValid { get; }

	string ControllerName();

	UniTask Activate();

	void Control();

	UniTask Pause();

	UniTask Restart();

	UniTask Deactivate();
}
