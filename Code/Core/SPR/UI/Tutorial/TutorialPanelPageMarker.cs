using UnityEngine;

namespace SPR.UI.Tutorial;

public class TutorialPanelPageMarker : MonoBehaviour
{
	[SerializeField]
	private Transform[] markers;

	public void SetData(int current, int total)
	{
		if (total <= 1)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		for (int i = 0; i < markers.Length; i++)
		{
			Transform obj = markers[i];
			obj.gameObject.SetActive(i < total);
			obj.GetChild(0).gameObject.SetActive(i == current - 1);
		}
		base.gameObject.SetActive(value: true);
	}
}
