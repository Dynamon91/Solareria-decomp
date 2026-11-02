using System.Collections;
using UnityEngine;

namespace SPR;

public sealed class DisplayModeBootSetter : MonoBehaviour
{
	private IEnumerator Start()
	{
		yield return null;
		DisplayModeHelper.ApplyDisplayModeFromPrefs();
	}
}
