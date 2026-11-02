using System.Threading;
using Cysharp.Threading.Tasks;
using Library;

namespace SPR;

public static class PlayerUniTaskUtil
{
	public static async UniTask WaitOneFrame()
	{
		await UniTaskExtension.WaitOneFrame();
	}

	public static async UniTask WaitForFixedUpdatesOrCancel(int frameCnt, CancellationToken token = default(CancellationToken))
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(frameCnt, token);
	}
}
