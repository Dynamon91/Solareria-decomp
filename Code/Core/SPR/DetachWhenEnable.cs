using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

public class DetachWhenEnable : MonoBehaviour
{
	[SerializeField]
	private Transform followSource;

	[SerializeField]
	private int detachFrame;

	[SerializeField]
	private bool resetRotation;

	private readonly CancellationTokenSource cts = new CancellationTokenSource();

	private void OnDestroy()
	{
		cts.Cancel();
	}

	private void OnEnable()
	{
		Detach().Forget();
	}

	private async UniTaskVoid Detach()
	{
		base.transform.SetParent(followSource);
		base.transform.position = followSource.position;
		if (resetRotation)
		{
			base.transform.localRotation = Quaternion.Euler(Vector3.zero);
		}
		base.transform.Flip(1f);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(detachFrame, cts.Token);
		if (!cts.IsCancellationRequested)
		{
			base.transform.SetParent(null);
		}
	}
}
