using UnityEngine;

namespace SPR.UI.Map;

public class MapGlobalCollectionInfo : MonoBehaviour
{
	private LocaleText localeText;

	private LocaleText LocaleText => localeText ?? (localeText = GetComponent<LocaleText>());

	public void Show()
	{
		int total;
		int current;
		int globalCollection = MapCollection.GetGlobalCollection(Game.Instance.SaveData, out total, out current);
		LocaleText.SetDataFormat("UI_Map_GlobalCollection_Format", globalCollection.ToString());
		DebugLog.GeneralLog($"전체 완료율 : {current} / {total} ({globalCollection}%)");
	}
}
