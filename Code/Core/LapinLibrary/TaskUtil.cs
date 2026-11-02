using System.Threading;

namespace LapinLibrary;

public static class TaskUtil
{
	public static CancellationToken RefreshToken(ref CancellationTokenSource tokenSource)
	{
		tokenSource?.Cancel();
		tokenSource?.Dispose();
		tokenSource = new CancellationTokenSource();
		return tokenSource.Token;
	}
}
