using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

public class DevController : MonoBehaviour, IControllable
{
	public readonly bool IsDevMode;

	public readonly bool IsImmortal;

	public readonly bool IsPowerful;

	public bool IsValid => false;

	public string ControllerName()
	{
		return "Dev Controller";
	}

	public UniTask Activate()
	{
		return UniTask.CompletedTask;
	}

	public void Control()
	{
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		return UniTask.CompletedTask;
	}
}
