using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Achievement;
using SPR.Save;
using SPR.UI.Field;
using SPR.UI.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SPR.UI.Map;

public class MenuMapPanel : MenuPanel
{
	[SerializeField]
	private Vector2 mapOffset;

	[SerializeField]
	private float moveSpeed;

	[SerializeField]
	private float moveSustain;

	[SerializeField]
	private float maxMoveSpeed;

	[SerializeField]
	private float moveAccel;

	[SerializeField]
	private float zoomAccel;

	[SerializeField]
	private float zoomMinScale;

	[SerializeField]
	private float zoomMaxScale;

	[SerializeField]
	private float fullZoomMinScale;

	[SerializeField]
	private float fullZoomMaxScale;

	[SerializeField]
	private float iconScaleMultiplier;

	[SerializeField]
	private Vector2 playerIconOffset;

	[Header("Focus")]
	[SerializeField]
	private float iconFocusDistance;

	[SerializeField]
	private float focusDuration;

	[SerializeField]
	private float refocusAngle;

	[SerializeField]
	[Range(90f, 180f)]
	private float restRefocusAngle;

	[SerializeField]
	[Range(90f, 180f)]
	private float fullMapRefocusAngle;

	[SerializeField]
	private float focusInputInterval;

	[Header("References")]
	[SerializeField]
	private RectTransform mapPartsParent;

	[SerializeField]
	private RectTransform mapIconsParent;

	[SerializeField]
	private RectTransform fullMapPartsParent;

	[SerializeField]
	private RectTransform playerMarker;

	[SerializeField]
	private RectTransform playerSoulMarker;

	[SerializeField]
	private RectTransform fullMapPlayerMarker;

	[SerializeField]
	private MapSectorName sectorName;

	[SerializeField]
	private MapLegend legendGroup;

	[SerializeField]
	private MapGlobalCollectionInfo globalCollection;

	[SerializeField]
	private MapCursor cursor;

	[SerializeField]
	private MapCursor fullCursor;

	[SerializeField]
	private MapIconInfo iconInfo;

	[SerializeField]
	private MapIndicatorController indicatorController;

	[SerializeField]
	private MapMarkerBoard markerBoard;

	[SerializeField]
	private List<FullMapPlayerOffset> fullMapPlayerOffsets;

	[SerializeField]
	private List<FullMapMoveDirection> fullMapMoveDirections;

	private CanvasGroup mapCanvasGroup;

	private CanvasGroup fullMapCanvasGroup;

	private GraphicRaycaster raycaster;

	private PointerEventData pointerEvent;

	private MiniMapGenerator mapGenerator;

	private Dictionary<FieldID, MapPart> mapPartsDict;

	private Dictionary<FieldSubtitle, FullMapPart> fullMapPartsDict;

	private List<MapIcon> mapIcons;

	private List<MapSpecialMarkerIcon> specialMarkerIcons;

	private List<MapIcon> activeMapIcons;

	private IEnumerable<MapIcon> focusableIcons;

	private IEnumerable<FullMapPart> focusableFullMapParts;

	private List<MapIcon> samePosIcons;

	private MapPart currentMapPart;

	private FullMapPart currentFullMapPart;

	private FullMapPart currentFocusFullMap;

	private MapIcon currentFocus;

	private Vector2 realPosition;

	private float currentMoveSpeed;

	private float moveTime;

	private float scaleFactor;

	private float targetScale;

	private float previousScale;

	private float previousFullScale;

	private Sequence focusSeq;

	private Sequence zoomSeq;

	private float focusInputTimer;

	private bool isSwitchingFocus;

	private bool isFullMap;

	private bool isMarkerMode;

	private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

	private List<MapFogPart> fogParts;

	public override MenuCategory Category => MenuCategory.Map;

	protected override bool DeactivateInput => MenuPanel.InputManager.map;

	protected override bool CanChangeCategory
	{
		get
		{
			if (!isMarkerMode)
			{
				return !MenuPanel.IsRest;
			}
			return false;
		}
	}

	private bool CanTeleport
	{
		get
		{
			if (MenuPanel.IsRest && !isFullMap && currentFocus is MapObjectIcon { Type: MapObjectType.Altar } mapObjectIcon)
			{
				return mapObjectIcon.FieldID != CurrentFieldID;
			}
			return false;
		}
	}

	private MapSpecialMarkerIcon CurrentSpecialMarker => specialMarkerIcons.First((MapSpecialMarkerIcon i) => i.MarkerType == markerBoard.CurrentMarkerType);

	private float IconScaleConstant => iconScaleMultiplier * 5f * zoomMinScale;

	private float IconScale => IconScaleConstant / scaleFactor;

	private bool BlockAfterNewFocus
	{
		get
		{
			return focusInputTimer > 0f;
		}
		set
		{
			focusInputTimer = ((!value) ? 0f : (MenuPanel.IsRest ? float.PositiveInfinity : focusInputInterval));
		}
	}

	private Vector2 MapAnchoredPosition
	{
		get
		{
			return mapPartsParent.anchoredPosition - mapOffset;
		}
		set
		{
			mapPartsParent.anchoredPosition = value + mapOffset;
		}
	}

	private static bool IsLibraryDestoryed => Trigger.CheckTrigger("Trigger_LibraryDestroyed");

	private static FieldID CurrentFieldID => Game.Instance.CurrentFieldID;

	private static SaveData SaveData => Game.Instance.SaveData;

	private static WanderingData WanderingData => SaveData.WanderingData;

	private static PlayerData PlayerData => SaveData.PlayerData;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static ControlGuideGroup GuideGroup => Game.Instance.UIManager.MenuCanvas.GuideGroup;

	private CanvasGroup MenuTopBarCanvas => base.MenuCanvas.CategoryList.CanvasGroup;

	private Vector2 CurrentWorldConstant => -realPosition / scaleFactor;

	public override void Initialize()
	{
		if ((object)mapCanvasGroup == null)
		{
			mapCanvasGroup = mapPartsParent.GetComponent<CanvasGroup>();
		}
		if ((object)fullMapCanvasGroup == null)
		{
			fullMapCanvasGroup = fullMapPartsParent.GetComponent<CanvasGroup>();
		}
		if ((object)raycaster == null)
		{
			raycaster = GetComponent<GraphicRaycaster>();
		}
		if (pointerEvent == null)
		{
			pointerEvent = new PointerEventData(null);
		}
		if (samePosIcons == null)
		{
			samePosIcons = new List<MapIcon>();
		}
		if ((object)mapGenerator == null)
		{
			mapGenerator = GetComponent<MiniMapGenerator>();
		}
		InitializeMapPartReferences();
		iconInfo.Initialize(focusDuration);
		markerBoard.Initialize(focusDuration);
		MapNormalMarkerIcon.PreloadSprites().Forget();
		MapCollection.Initialize(mapIcons.Where((MapIcon icon) => icon is MapObjectIcon).Cast<MapObjectIcon>().ToList());
		scaleFactor = zoomMaxScale;
		targetScale = scaleFactor;
		previousScale = zoomMaxScale;
		previousFullScale = fullZoomMaxScale;
	}

	protected override void OnActivate()
	{
		UpdateMapPartVisibility();
		UpdatePlayerMarkerPosition();
		UpdatePlayerSoulMarkerPosition();
		legendGroup.SetLegend(playerSoulMarker.gameObject.activeInHierarchy);
		InitializeMove();
		ChangeFullModeAsync(activateFull: false, isInstant: true).Forget();
		SetZoomScale();
		SetPlayerOnCenter();
		SynchronizeFullMap();
		globalCollection.Show();
		RaycastMap(instant: true);
		ShowGuide();
		legendGroup.CanvasGroup.alpha = ((!MenuPanel.IsRest) ? 1 : 0);
		WanderingData.UpdateUnwatchedInfoStoneIcon();
	}

	protected override void OnControl()
	{
		if (MenuPanel.InputManager.reset && (!isMarkerMode || !markerBoard.IsSpecialMarker))
		{
			SetPlayerOnCenter(instant: false);
		}
		bool detail = MenuPanel.InputManager.detail;
		bool confirmDown = MenuPanel.InputManager.confirmDown;
		if ((detail || (isFullMap && confirmDown)) && !isSwitchingFocus && !isMarkerMode && !MenuPanel.IsRest)
		{
			SetPreviousScale();
			ChangeFullModeAsync(!isFullMap).GetAwaiter().OnCompleted(ShowGuide);
		}
		float scroll = MenuPanel.InputManager.scroll;
		if (scroll != 0f && !isSwitchingFocus)
		{
			float num = scroll * Mathf.Abs(scaleFactor) * zoomAccel * Time.deltaTime;
			targetScale = Mathf.Clamp(min: isFullMap ? fullZoomMinScale : zoomMinScale, max: isFullMap ? fullZoomMaxScale : zoomMaxScale, value: targetScale + num);
			if (!Mathf.Approximately(scaleFactor, targetScale))
			{
				zoomSeq?.Kill();
				zoomSeq = DOTween.Sequence().Join(DOTween.To(() => scaleFactor, delegate(float f)
				{
					scaleFactor = f;
					SetZoomScale();
				}, targetScale, focusDuration).SetEase(Ease.OutQuad));
			}
		}
		int horizontalUI = MenuPanel.InputManager.horizontalUI;
		int verticalUI = MenuPanel.InputManager.verticalUI;
		if ((horizontalUI != 0 || verticalUI != 0) && (!isMarkerMode || !markerBoard.IsSpecialMarker))
		{
			if (MenuPanel.IsRest)
			{
				UpdatePositionOnRest(horizontalUI, verticalUI);
			}
			else
			{
				UpdatePosition(horizontalUI, verticalUI);
			}
		}
		else
		{
			moveTime = 0f;
			BlockAfterNewFocus = false;
		}
		if (BlockAfterNewFocus)
		{
			focusInputTimer -= Time.deltaTime;
		}
		indicatorController.UpdateIndicators(MapAnchoredPosition, mapPartsParent.localScale.x);
		if (confirmDown)
		{
			if (CanTeleport)
			{
				MapObjectIcon mapObjectIcon = currentFocus as MapObjectIcon;
				if (mapObjectIcon.IsDisabled)
				{
					MenuPanel.FadeCanvas.WarningBoard.ShowWarning("UI_Library_Front_Warning");
				}
				else
				{
					FieldID fieldID = mapObjectIcon.FieldID;
					DeactivateMenuAsync().GetAwaiter().OnCompleted(delegate
					{
						MenuPanel.InputManager.RemoveControlTarget(MenuPanel.RestCanvas).Forget();
					});
					Game.Instance.LoadFieldAsync(SceneDataModel.RespawnModel(fieldID)).GetAwaiter().OnCompleted(CallbackAfterTeleport);
				}
			}
			else if (isMarkerMode && !isSwitchingFocus)
			{
				AddOrRemoveMarker();
			}
		}
		int categoryUI = MenuPanel.InputManager.categoryUI;
		if (categoryUI != 0 && isMarkerMode && !isSwitchingFocus && markerBoard.TryMoveCursor(categoryUI, out var _))
		{
			if (markerBoard.IsSpecialMarker)
			{
				FocusSpecialMarkerAsync().Forget();
			}
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
		bool mode = MenuPanel.InputManager.mode;
		if (mode && !isMarkerMode && !isSwitchingFocus && !isFullMap && !MenuPanel.IsRest)
		{
			SetCurrentFocus(null);
			ChangeMarkerModeAsync(active: true, 1f / 6f).GetAwaiter().OnCompleted(delegate
			{
				SetFocusableIcons();
				UpdatePosition(0, 0);
			});
			ShowGuide();
		}
		if (mode && isMarkerMode && !isSwitchingFocus)
		{
			SetCurrentFocus(null);
			ChangeMarkerModeAsync(active: false, 1f / 6f).GetAwaiter().OnCompleted(delegate
			{
				SetFocusableIcons();
				UpdatePosition(0, 0);
			});
			ShowGuide();
		}
		if (detail && isMarkerMode && !isSwitchingFocus)
		{
			if (markerBoard.TryChangeMode(out var isSpecial))
			{
				if (isSpecial)
				{
					FocusSpecialMarkerAsync().Forget();
				}
				else
				{
					SetCurrentFocus(null);
					SetFocusableIcons();
					UpdatePosition(0, 0);
				}
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
				ShowGuide();
			}
			else if (!markerBoard.HasAnySpecialMarker)
			{
				MenuPanel.FadeCanvas.WarningBoard.ShowWarning("UI_Map_Warning_HasNoSpecialMarker");
			}
		}
		SynchronizeFullMap();
		RaycastMap();
	}

	protected override void OnDeactivate()
	{
		indicatorController.Clear();
		SetPreviousScale();
		iconInfo.DeactivateNow();
		if (isMarkerMode)
		{
			ChangeMarkerModeAsync(active: false, 0f).Forget();
		}
		if (mapIcons.IsNullOrEmpty())
		{
			return;
		}
		foreach (MapNormalMarkerIcon item in mapIcons.Where((MapIcon i) => i is MapNormalMarkerIcon).Cast<MapNormalMarkerIcon>().ToList())
		{
			mapIcons.Remove(item);
			activeMapIcons.Remove(item);
			item.Deactivate(0f);
		}
	}

	private void InitializeMapPartReferences()
	{
		if (mapPartsDict != null)
		{
			return;
		}
		mapPartsDict = new Dictionary<FieldID, MapPart>();
		fullMapPartsDict = new Dictionary<FieldSubtitle, FullMapPart>();
		mapIcons = new List<MapIcon>();
		specialMarkerIcons = new List<MapSpecialMarkerIcon>();
		activeMapIcons = new List<MapIcon>();
		fogParts = new List<MapFogPart>();
		MapPart[] componentsInChildren = mapPartsParent.GetComponentsInChildren<MapPart>();
		foreach (MapPart mapPart in componentsInChildren)
		{
			mapPartsDict.Add(mapPart.FieldID, mapPart);
			mapPart.Initialize();
			MapIcon[] componentsInChildren2 = mapPart.GetComponentsInChildren<MapIcon>(includeInactive: true);
			foreach (MapIcon mapIcon in componentsInChildren2)
			{
				mapIcon.transform.SetParent(mapIconsParent);
				mapIcons.Add(mapIcon);
				if (mapIcon is MapSpecialMarkerIcon item)
				{
					specialMarkerIcons.Add(item);
				}
			}
			MapFogPart[] componentsInChildren3 = mapPart.GetComponentsInChildren<MapFogPart>(includeInactive: true);
			foreach (MapFogPart mapFogPart in componentsInChildren3)
			{
				fogParts.Add(mapFogPart);
				mapFogPart.Initialize();
			}
		}
		SortMapIcons();
		specialMarkerIcons = specialMarkerIcons.OrderBy((MapSpecialMarkerIcon icon) => icon.MarkerType).ToList();
		FullMapPart[] componentsInChildren4 = fullMapPartsParent.GetComponentsInChildren<FullMapPart>();
		foreach (FullMapPart fullMapPart in componentsInChildren4)
		{
			fullMapPartsDict.TryAdd(fullMapPart.Type, fullMapPart);
		}
	}

	private void UpdatePlayerMarkerPosition()
	{
		Vector2 normalizedPlayerPosition = GetNormalizedPlayerPosition();
		FieldID source = (CurrentFieldID.Equals(SectorName.Library, 99) ? new FieldID(SectorName.Library, 3) : CurrentFieldID);
		Vector2 pos;
		if (mapPartsDict.TryGetValue(source, out var value))
		{
			Vector2 vector = value.CalculateIconPosition(normalizedPlayerPosition);
			playerMarker.anchoredPosition = vector + playerIconOffset;
		}
		else if (mapGenerator.TryGetUnconnectedPlayerPosition(source, out source, out pos) && mapPartsDict.TryGetValue(source, out value))
		{
			DebugLog.GeneralLog($"지도에 표시되지 않은 씬({CurrentFieldID})이어서, 입구({source}, {pos})에 플레이어 아이콘을 표시합니다.");
			playerMarker.anchoredPosition = value.RectTransform.anchoredPosition + pos * 5f + playerIconOffset;
		}
		else
		{
			DebugLog.GeneralLogError($"현재 위치({source})에 해당하는 지도 오브젝트가 존재하지 않습니다!");
		}
		FieldSubtitle subtitle = source.GetSubtitle();
		if (fullMapPartsDict.TryGetValue(subtitle, out var value2))
		{
			FullMapPlayerOffset fullMapPlayerOffset = fullMapPlayerOffsets.FirstOrDefault((FullMapPlayerOffset o) => o.subtitle == subtitle);
			Vector2 vector2 = ((fullMapPlayerOffset.subtitle == FieldSubtitle.None) ? Vector2.zero : fullMapPlayerOffset.offset);
			fullMapPlayerMarker.anchoredPosition = value2.RectTransform.anchoredPosition + vector2;
		}
		else
		{
			DebugLog.GeneralLogError($"현재 분철({subtitle})에 해당하는 지도 오브젝트가 존재하지 않습니다!");
		}
	}

	public void UpdatePlayerSoulMarkerPosition()
	{
		PlayerData.DropData currentDropData = Game.Instance.SaveData.PlayerData.CurrentDropData;
		bool isValid = currentDropData.IsValid;
		playerSoulMarker.gameObject.SetActive(isValid);
		MapPart value;
		if (!isValid)
		{
			playerSoulMarker.gameObject.SetActive(value: false);
		}
		else if (mapPartsDict.TryGetValue(currentDropData.FieldID, out value))
		{
			Vector2 anchoredPosition = value.CalculateIconPosition(currentDropData.NormalizedPos);
			playerSoulMarker.anchoredPosition = anchoredPosition;
		}
		else
		{
			DebugLog.GeneralLogError($"영혼 위치({currentDropData.FieldID})에 해당하는 지도 오브젝트가 존재하지 않습니다!");
		}
	}

	private static Vector2 GetNormalizedPlayerPosition()
	{
		AreaBox areaBox = Game.Instance.FieldController.MainArea.AreaBox;
		Vector2 visualBottom = Game.Instance.Player.mover.VisualBottom;
		float x = Mathf.InverseLerp(areaBox.LowerLeft.x, areaBox.UpperRight.x, visualBottom.x);
		float y = Mathf.InverseLerp(areaBox.LowerLeft.y, areaBox.UpperRight.y, visualBottom.y);
		return new Vector2(x, y);
	}

	private void UpdateMapPartVisibility()
	{
		foreach (KeyValuePair<FieldID, MapPart> item in mapPartsDict)
		{
			item.Value.Deactivate();
		}
		foreach (KeyValuePair<FieldSubtitle, FullMapPart> item2 in fullMapPartsDict)
		{
			item2.Value.SetActive(value: false);
		}
		List<FieldSubtitle> visitedSubtitles = new List<FieldSubtitle>();
		SectorName[] enumValues = Util.GetEnumValues<SectorName>();
		int i;
		for (i = 0; i < enumValues.Length; i++)
		{
			SectorName sector = enumValues[i];
			ApplyVisitedData(sector);
		}
		if (mapPartsDict.TryGetValue(new FieldID(SectorName.SolinLab, 4), out var value) && SaveData.PlayerData.HasEverObtainedItem("SolinLabEntranceKey", out i))
		{
			value.Activate();
		}
		foreach (MapFogPart fogPart in fogParts)
		{
			fogPart.SetActive();
		}
		foreach (MapIconData activeNormalIcon in SaveData.WorldData.MapData.ActiveNormalIcons)
		{
			foreach (Vector2 worldPositionConstant in activeNormalIcon.worldPositionConstants)
			{
				MapNormalMarkerIcon mapNormalMarkerIcon = Game.Instance.ObjectPoolManager.GetObject<MapNormalMarkerIcon>("Map Normal Marker Icon.prefab", mapIconsParent);
				mapNormalMarkerIcon.SetDataAsync(activeNormalIcon.type, worldPositionConstant).Forget();
				mapNormalMarkerIcon.SetPositionScale(IconScaleConstant);
				mapIcons.Add(mapNormalMarkerIcon);
			}
		}
		SortMapIcons();
		SetFocusableIcons();
		foreach (MapIcon item3 in mapIcons.Where((MapIcon mapIcon) => mapIcon.CheckActive()))
		{
			if (IsInFog(item3.RectTransform.position) && item3 is MapObjectIcon icon && !IsKnownIcon(icon))
			{
				item3.SetActive(value: false);
				continue;
			}
			item3.SetActive(value: true);
			if (!(item3 is MapObjectIcon mapObjectIcon))
			{
				continue;
			}
			mapObjectIcon.SetDisabled(IsLibraryDestoryed, out var isActive);
			if (!isActive)
			{
				item3.SetActive(value: false);
				continue;
			}
			bool flag = IsUnwatchedIcon(mapObjectIcon);
			mapObjectIcon.SetHighlight(flag);
			if (flag)
			{
				indicatorController.Add(mapObjectIcon);
			}
			mapObjectIcon.SetRecent(mapObjectIcon.Type == MapObjectType.Altar && mapObjectIcon.FieldID.Equals(SaveData.LastFieldID));
			SaveData.WorldData.MapData.AddActiveLegend(mapObjectIcon.Type);
		}
		foreach (FieldSubtitle item4 in visitedSubtitles)
		{
			if (fullMapPartsDict.TryGetValue(item4, out var value2))
			{
				value2.SetActive(value: true);
			}
			else if (item4 != FieldSubtitle.None)
			{
				DebugLog.GeneralLogError($"{item4}에 해당하는 세계 지도 오브젝트가 존재하지 않습니다! 일단 넘어갑니다.\n" + "Map Panel 프리팹의 Mini Map Generator의 [Generate] 버튼을 누르고, Ctrl + S를 눌러 저장해주세요!");
			}
		}
		focusableFullMapParts = from kvp in fullMapPartsDict
			where kvp.Value.gameObject.activeSelf
			select kvp.Value;
		activeMapIcons.Clear();
		activeMapIcons.AddRange(mapIcons.Where((MapIcon mapIcon) => mapIcon.gameObject.activeInHierarchy));
		void ApplyVisitedData(SectorName sectorName)
		{
			if (sectorName == SectorName.None || !SaveData.WorldData.TryGetSectorData(sectorName, out var data))
			{
				return;
			}
			foreach (int visitedFieldID in data.VisitedFieldIDList)
			{
				FieldID source = new FieldID(sectorName, visitedFieldID);
				Vector2 pos;
				if (mapPartsDict.TryGetValue(source, out var value3))
				{
					value3.Activate();
				}
				else if (mapGenerator.TryGetUnconnectedPlayerPosition(source, out source, out pos))
				{
					DebugLog.GeneralLog($"{source}는 지도에 표시되지 않는 씬이므로 넘어갑니다!");
				}
				else if (!source.Equals(SectorName.None, 0))
				{
					DebugLog.GeneralLogError($"{source}에 해당하는 지도 오브젝트가 존재하지 않습니다! 일단 넘어갑니다.\n" + "Map Panel 프리팹의 Mini Map Generator의 [Generate] 버튼을 누르고, Ctrl + S를 눌러 저장해주세요!");
				}
				FieldSubtitle subtitle = source.GetSubtitle();
				if (!visitedSubtitles.Contains(subtitle))
				{
					visitedSubtitles.Add(subtitle);
				}
			}
		}
	}

	private void SortMapIcons()
	{
		foreach (MapIcon item in mapIcons.OrderBy(GetIconOrder))
		{
			item.transform.SetAsFirstSibling();
		}
	}

	private static int GetIconOrder(MapIcon icon)
	{
		if (!(icon is MapObjectIcon { Type: var type }))
		{
			if (icon is MapMarkerIcon mapMarkerIcon)
			{
				return mapMarkerIcon.MarkerType;
			}
			return 0;
		}
		int num = (int)type;
		if (num < 2)
		{
			return num + 100;
		}
		return type switch
		{
			MapObjectType.Relics => 102, 
			MapObjectType.FiveWarriors => 103, 
			_ => num + 2 + 100, 
		};
	}

	private void InitializeMove()
	{
		realPosition = MapAnchoredPosition;
		currentFocus = null;
		BlockAfterNewFocus = false;
		isSwitchingFocus = false;
	}

	private void SetZoomScale()
	{
		float x = mapPartsParent.localScale.x;
		Vector2 vector = realPosition / x;
		Vector3 localScale = new Vector3(scaleFactor, scaleFactor, 1f);
		realPosition = vector * scaleFactor;
		MapAnchoredPosition = realPosition;
		mapPartsParent.localScale = localScale;
		float iconScale = IconScale;
		Vector3 localScale2 = new Vector3(iconScale, iconScale, 1f);
		playerMarker.localScale = localScale2;
		playerSoulMarker.localScale = localScale2;
		fullMapPlayerMarker.localScale = localScale2;
		foreach (MapIcon activeMapIcon in activeMapIcons)
		{
			activeMapIcon.RectTransform.localScale = localScale2;
		}
	}

	private void SetPlayerOnCenter(bool instant = true)
	{
		if (isSwitchingFocus && !instant)
		{
			return;
		}
		Vector2 anchoredPosition = (isFullMap ? fullMapPlayerMarker : playerMarker).anchoredPosition;
		realPosition = -anchoredPosition * mapPartsParent.localScale.x;
		if (instant)
		{
			MapAnchoredPosition = realPosition;
			UpdatePosition(0, 0);
			return;
		}
		FocusAsync(realPosition, null).GetAwaiter().OnCompleted(delegate
		{
			currentFocusFullMap = null;
			UpdatePosition(0, 0);
		});
		SetCurrentFocus(null);
		MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
	}

	private void SetFocusableIcons()
	{
		focusableIcons = activeMapIcons.Where(Condition);
		bool Condition(MapIcon icon)
		{
			if (isMarkerMode)
			{
				if (!markerBoard.IsSpecialMarker)
				{
					return icon is MapNormalMarkerIcon;
				}
				return icon is MapSpecialMarkerIcon;
			}
			return true;
		}
	}

	private void UpdatePosition(int moveX, int moveY, bool playSfx = true)
	{
		_ = scaleFactor;
		if (isSwitchingFocus || BlockAfterNewFocus)
		{
			return;
		}
		moveTime += Time.deltaTime;
		currentMoveSpeed = ((moveTime <= moveSustain) ? moveSpeed : Mathf.Min(currentMoveSpeed + moveAccel * Time.deltaTime, maxMoveSpeed));
		if (isFullMap)
		{
			MoveFullMap(moveX, moveY);
			return;
		}
		Vector2 inputVector = new Vector2(moveX, moveY);
		if ((object)currentFocus != null)
		{
			if (inputVector != Vector2.zero)
			{
				MapIcon mapIcon = (from icon in focusableIcons
					select (icon: icon, v: Displacement(icon)) into tuple
					where tuple.v.magnitude < iconFocusDistance && Vector2.Angle(inputVector, tuple.v) <= refocusAngle / 2f
					orderby tuple.v.magnitude, GetIconOrder(tuple.icon)
					select tuple.icon into icon
					where (object)currentFocus != icon && !samePosIcons.Contains(icon)
					select icon).FirstOrDefault();
				if ((object)mapIcon != null)
				{
					if (Mathf.Approximately((mapIcon.WorldPositionConstant - currentFocus.WorldPositionConstant).magnitude, 0f))
					{
						samePosIcons.Add(currentFocus);
					}
					else
					{
						samePosIcons.Clear();
					}
					FocusNew(mapIcon);
					SetCurrentFocus(mapIcon, playSfx);
					return;
				}
				samePosIcons.Clear();
			}
			realPosition += -inputVector.normalized * iconFocusDistance;
		}
		else
		{
			realPosition += -inputVector * (currentMoveSpeed * Time.deltaTime);
		}
		MapIcon mapIcon2 = (from icon in focusableIcons
			where (object)icon != currentFocus && Displacement(icon).magnitude < iconFocusDistance
			orderby Displacement(icon).magnitude
			select icon).ThenBy(GetIconOrder).FirstOrDefault();
		if ((object)mapIcon2 != null)
		{
			if (!BlockAfterNewFocus)
			{
				FocusNew(mapIcon2);
			}
			SetCurrentFocus(mapIcon2, playSfx);
		}
		else
		{
			MoveWithoutFocus();
		}
		bool MoveWithoutFocus()
		{
			if ((object)currentFocus == null)
			{
				MapAnchoredPosition = realPosition;
				return true;
			}
			FocusAsync(realPosition, false).Forget();
			SetCurrentFocus(null, playSfx);
			return false;
		}
	}

	private void UpdatePositionOnRest(int moveX, int moveY)
	{
		if (isSwitchingFocus || BlockAfterNewFocus)
		{
			return;
		}
		if ((object)currentFocus != null)
		{
			Vector2 inputVector = new Vector2(moveX, moveY);
			if (!(inputVector == Vector2.zero))
			{
				MapIcon mapIcon = (from icon in focusableIcons
					where icon is MapObjectIcon { Type: MapObjectType.Altar } mapObjectIcon && !mapObjectIcon.IsDisabled
					select (icon: icon, v: Displacement(icon)) into tuple
					where Vector2.Angle(inputVector, tuple.v) <= restRefocusAngle / 2f
					orderby tuple.v.magnitude
					select tuple.icon into icon
					where (object)currentFocus != icon
					select icon).FirstOrDefault();
				if ((object)mapIcon != null)
				{
					FocusNew(mapIcon);
					SetCurrentFocus(mapIcon);
				}
			}
			return;
		}
		MapIcon mapIcon2 = (from icon in focusableIcons
			where icon is MapObjectIcon mapObjectIcon && mapObjectIcon.Type == MapObjectType.Altar
			orderby Displacement(icon).magnitude
			select icon).FirstOrDefault();
		if ((object)mapIcon2 != null)
		{
			if (!BlockAfterNewFocus)
			{
				FocusNew(mapIcon2);
			}
			SetCurrentFocus(mapIcon2);
		}
	}

	private void MoveFullMap(float moveX, float moveY)
	{
		List<(FullMapPart, Vector2)> source = focusableFullMapParts.Select((FullMapPart fullMapPart2) => (fullMap: fullMapPart2, v: realPosition + fullMapPart2.WorldConstant * scaleFactor)).ToList();
		if (currentFocusFullMap.IsNull())
		{
			FullMapPart item = source.OrderBy<(FullMapPart, Vector2), float>(((FullMapPart fullMap, Vector2 v) tuple) => tuple.v.magnitude).First().Item1;
			FocusFullMap(item);
			return;
		}
		Vector2 inputVector = new Vector2(moveX, moveY);
		if (inputVector == Vector2.zero)
		{
			return;
		}
		List<FieldSubtitle> targets = fullMapMoveDirections.FirstOrDefault((FullMapMoveDirection d) => d.subtitle == currentFocusFullMap.Type && d.direction == inputVector.ToDirection(includeDiagonal: false)).targets;
		FullMapPart fullMapPart = null;
		if (!targets.IsNullOrEmpty())
		{
			foreach (FieldSubtitle target in targets)
			{
				fullMapPart = source.FirstOrDefault<(FullMapPart, Vector2)>(((FullMapPart fullMap, Vector2 v) t) => t.fullMap.Type == target).Item1;
				if ((object)fullMapPart != null)
				{
					break;
				}
			}
		}
		if ((object)fullMapPart != null)
		{
			FocusFullMap(fullMapPart);
			BlockAfterNewFocus = true;
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
		}
		void FocusFullMap(FullMapPart fullMapPart2)
		{
			realPosition = fullMapPart2.WorldConstant * (0f - scaleFactor);
			FocusAsync(realPosition, true).Forget();
			currentFocusFullMap = fullMapPart2;
			SelectCurrentFullMapPart(focusDuration);
		}
	}

	private Vector2 Displacement(MapIcon icon)
	{
		return realPosition + icon.WorldPositionConstant * scaleFactor;
	}

	private void FocusNew(MapIcon icon)
	{
		FocusAsync(realPosition = MapPosition(icon), true).Forget();
		if (icon is MapObjectIcon icon2)
		{
			CheckUnwatched(icon2);
		}
		BlockAfterNewFocus = true;
	}

	private async UniTask FocusAsync(Vector2 targetPos, bool? isEnter)
	{
		isSwitchingFocus = true;
		Vector2 normalizedOrigin = MapAnchoredPosition / scaleFactor;
		Vector2 normalizedDistance = targetPos / scaleFactor - normalizedOrigin;
		Vector2 offset = Vector2.zero;
		Ease ease = ((!isEnter.HasValue) ? Ease.InOutQuad : (isEnter.Value ? Ease.OutQuad : Ease.InQuad));
		focusSeq?.Kill();
		focusSeq = DOTween.Sequence().Join(DOTween.To(() => offset, delegate(Vector2 v2)
		{
			offset = v2;
			SetRealPosition(offset);
		}, normalizedDistance, focusDuration).SetEase(ease)).OnComplete(delegate
		{
			SetRealPosition(normalizedDistance);
		});
		await focusSeq.WaitAsync();
		isSwitchingFocus = false;
		void SetRealPosition(Vector2 currentOffset)
		{
			MapAnchoredPosition = (normalizedOrigin + currentOffset) * scaleFactor;
		}
	}

	private void SetCurrentFocus(MapIcon icon, bool playSfx = true)
	{
		bool flag = (object)icon != null;
		currentFocus = icon;
		cursor.SetFocus(flag);
		iconInfo.SetActive(currentFocus);
		SortMapIcons();
		icon?.transform.SetAsLastSibling();
		if (flag && playSfx)
		{
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
		}
	}

	private void SetPreviousScale()
	{
		if (isFullMap)
		{
			previousFullScale = targetScale;
		}
		else
		{
			previousScale = targetScale;
		}
	}

	private async UniTask ChangeFullModeAsync(bool activateFull, bool isInstant = false)
	{
		isSwitchingFocus = true;
		cursor.SetFocus(value: false);
		fullCursor.SetFocus(value: false);
		int num = ((!activateFull) ? 1 : 0);
		int num2 = (activateFull ? 1 : 0);
		float scale = (activateFull ? previousFullScale : previousScale);
		SetCurrentFocus(null);
		Vector2 vector = CurrentWorldConstant;
		if (!isFullMap && (object)currentMapPart != null && fullMapPartsDict.TryGetValue(currentMapPart.FieldID.GetSubtitle(), out var value))
		{
			vector = value.RectTransform.anchoredPosition;
		}
		if (isInstant)
		{
			mapCanvasGroup.alpha = num;
			cursor.CanvasGroup.alpha = num;
			fullMapCanvasGroup.alpha = num2;
			fullCursor.CanvasGroup.alpha = 0f;
			legendGroup.CanvasGroup.alpha = num;
			indicatorController.CanvasGroup.alpha = num;
			scaleFactor = scale;
			realPosition = -vector * scaleFactor;
			SetZoomScale();
		}
		else
		{
			Ease ease = (activateFull ? Ease.OutQuad : Ease.InQuad);
			Vector2 tweenConstant = CurrentWorldConstant;
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_Message_Appear");
			await DOTween.Sequence().Join(mapCanvasGroup.DOAlpha(num, focusDuration).SetEase(ease)).Join(cursor.CanvasGroup.DOAlpha(num, focusDuration).SetEase(ease))
				.Join(fullMapCanvasGroup.DOAlpha(num2, focusDuration).SetEase(ease))
				.Join(fullCursor.CanvasGroup.DOAlpha(0f, focusDuration).SetEase(ease))
				.Join(legendGroup.CanvasGroup.DOAlpha(num, focusDuration).SetEase(ease))
				.Join(indicatorController.CanvasGroup.DOAlpha(num, focusDuration).SetEase(ease))
				.Join(DOTween.To(() => tweenConstant, delegate(Vector2 v)
				{
					tweenConstant = v;
					realPosition = -tweenConstant * scaleFactor;
				}, vector, focusDuration).SetEase(ease))
				.Join(DOTween.To(() => scaleFactor, delegate(float f)
				{
					scaleFactor = f;
					SetZoomScale();
				}, scale, focusDuration).SetEase(ease))
				.WaitAsync();
		}
		targetScale = scale;
		isFullMap = activateFull;
		isSwitchingFocus = false;
		if (!isInstant)
		{
			UpdatePosition(0, 0);
		}
		if (!isFullMap && (object)currentFullMapPart != null)
		{
			currentFullMapPart.SetHighlight(value: false);
			currentFullMapPart = null;
			currentFocusFullMap = null;
		}
	}

	private async UniTask ChangeMarkerModeAsync(bool active, float duration)
	{
		isSwitchingFocus = true;
		_ = active;
		int num = ((!active) ? 1 : 0);
		Ease ease = (active ? Ease.OutQuad : Ease.InQuad);
		if (duration > 0f)
		{
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
		}
		isMarkerMode = active;
		markerBoard.SetActive(active, duration, ease);
		await DOTween.Sequence().Join(legendGroup.CanvasGroup.DOAlpha(num, duration).SetEase(ease)).Join(MenuTopBarCanvas.DOAlpha(num, duration).SetEase(ease))
			.WaitAsync();
		isSwitchingFocus = false;
	}

	private async UniTask FocusSpecialMarkerAsync()
	{
		MapSpecialMarkerIcon icon = CurrentSpecialMarker;
		if (icon.IsNull())
		{
			DebugLog.GeneralLogError($"현재 마커({markerBoard.CurrentMarkerType})에 해당하는 특수 마커가 지도에 생성되어 있지 않습니다!");
			throw new Exception();
		}
		SetCurrentFocus(null);
		realPosition = MapPosition(icon);
		await FocusAsync(realPosition, true);
		SetCurrentFocus(icon, playSfx: false);
	}

	private void AddOrRemoveMarker()
	{
		if (markerBoard.IsSpecialMarker)
		{
			if (markerBoard.TrySetSpecialMarker(CurrentSpecialMarker, out var result))
			{
				CurrentSpecialMarker.SetScale(IconScale);
				if (result)
				{
					activeMapIcons.Add(CurrentSpecialMarker);
					MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Map/SFX_UI_Map_Marker_Place");
				}
				else
				{
					activeMapIcons.Remove(CurrentSpecialMarker);
					MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Map/SFX_UI_Map_Marker_Remove");
				}
			}
			return;
		}
		MapIcon mapIcon = currentFocus;
		if ((object)mapIcon != null && !(mapIcon is MapNormalMarkerIcon))
		{
			return;
		}
		MapNormalMarkerIcon mapNormalMarkerIcon = currentFocus as MapNormalMarkerIcon;
		if (!markerBoard.TrySetNormalMarker(mapNormalMarkerIcon, CurrentWorldConstant, out var newIcon, out var resultIcon))
		{
			return;
		}
		if (newIcon)
		{
			resultIcon.SetDataAsync(markerBoard.CurrentMarkerType, CurrentWorldConstant).GetAwaiter().OnCompleted(delegate
			{
				resultIcon.transform.SetParent(mapIconsParent);
				resultIcon.SetPositionScale(IconScale);
				resultIcon.Activate(focusDuration);
			});
			mapIcons.Add(resultIcon);
			activeMapIcons.Add(resultIcon);
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Map/SFX_UI_Map_Marker_Place");
		}
		else
		{
			if (!mapNormalMarkerIcon)
			{
				DebugLog.GeneralLogError("지도 일반 마커에 뭔가 엄청난 로직 에러가 발생했습니다! 왜지?부가 설명 : 현재 포커스 된 일반 마커 제거 허가가 떨어졌는데, 제거하려고 봤더니 현재 포커스 된 일반 마커가 없어졌습니다!");
				return;
			}
			mapIcons.Remove(mapNormalMarkerIcon);
			activeMapIcons.Remove(mapNormalMarkerIcon);
			mapNormalMarkerIcon.Deactivate(focusDuration);
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Map/SFX_UI_Map_Marker_Remove");
		}
		UpdatePosition(0, 0, playSfx: false);
	}

	private void SynchronizeFullMap()
	{
		fullMapPartsParent.anchoredPosition = mapPartsParent.anchoredPosition;
		fullMapPartsParent.localScale = mapPartsParent.localScale;
	}

	private void RaycastMap(bool instant = false)
	{
		if (!instant && isSwitchingFocus)
		{
			return;
		}
		raycastResults.Clear();
		Vector3 position = (isFullMap ? fullCursor : cursor).transform.position;
		pointerEvent.position = position;
		float duration = (instant ? 0f : focusDuration);
		raycaster.Raycast(pointerEvent, raycastResults);
		if (!isFullMap)
		{
			if (!raycastResults.Any((RaycastResult r) => r.gameObject.transform.parent.TryGetComponent<MapPart>(out currentMapPart)) || IsInFog(position))
			{
				currentMapPart = null;
			}
			sectorName.SetActive(currentMapPart, duration);
		}
	}

	private void SelectCurrentFullMapPart(float duration)
	{
		currentMapPart = null;
		FullMapPart fullMapPart = currentFullMapPart;
		currentFullMapPart = currentFocusFullMap;
		if ((object)fullMapPart != currentFullMapPart)
		{
			if (!fullMapPart)
			{
				fullCursor.SetFocus(value: true);
			}
			if (!currentFullMapPart)
			{
				fullCursor.SetFocus(value: false);
			}
			fullMapPart?.SetHighlight(value: false);
			currentFullMapPart?.SetHighlight(value: true);
		}
		sectorName.SetActive(currentFullMapPart, duration);
	}

	private static bool IsUnwatchedIcon(MapObjectIcon icon)
	{
		switch (icon.Type)
		{
		case MapObjectType.FieldItem:
		case MapObjectType.Information:
		case MapObjectType.Npc:
		case MapObjectType.ItemBox:
		case MapObjectType.Sprout:
			return WanderingData.IsUnwatchedInfoStoneIcon(icon.StringValue);
		default:
			return false;
		}
	}

	private static bool IsKnownIcon(MapObjectIcon icon)
	{
		switch (icon.Type)
		{
		case MapObjectType.FieldItem:
		case MapObjectType.Information:
		case MapObjectType.Npc:
		case MapObjectType.ItemBox:
		case MapObjectType.Sprout:
			return WanderingData.HasKnownInfoStone(icon.StringValue);
		case MapObjectType.Relics:
		case MapObjectType.FiveWarriors:
		{
			int current;
			return PlayerData.HasEverObtainedItem("AshHillsFiveRelics", out current);
		}
		default:
			return false;
		}
	}

	private void CheckUnwatched(MapObjectIcon icon)
	{
		if (IsUnwatchedIcon(icon))
		{
			switch (icon.Type)
			{
			case MapObjectType.FieldItem:
			case MapObjectType.Information:
			case MapObjectType.Npc:
			case MapObjectType.ItemBox:
			case MapObjectType.Sprout:
				WanderingData.RemoveUnwatchedInfoStoneIcon(icon.StringValue);
				break;
			}
			indicatorController.TryRemove(icon);
			icon.SetHighlight(isHighlight: false);
		}
	}

	public static void AddInfoStoneInformation(string id)
	{
		if (!(id == "AshHillsFiveRelics") && WanderingData.AddKnownInfoStone(id) && !Trigger.CheckTrigger("Trigger_" + id))
		{
			FieldCanvas.MessageBoard.Activate(MessageType.Map);
			WanderingData.AddUnwatchedInfoStoneIcon(id);
		}
	}

	private Vector2 MapPosition(MapIcon icon)
	{
		return -icon.WorldPositionConstant * scaleFactor;
	}

	private bool IsInFog(Vector2 worldPos)
	{
		return fogParts.Any((MapFogPart fog) => fog.CheckRaycast(worldPos));
	}

	private void ShowGuide()
	{
		if (MenuPanel.IsRest)
		{
			GuideGroup.ActivateMap(Mode.FastTravel);
		}
		else if (isFullMap)
		{
			GuideGroup.ActivateMap(Mode.FullMap);
		}
		else if (isMarkerMode)
		{
			Mode mode = (markerBoard.IsSpecialMarker ? Mode.SpecialMarker : Mode.NormalMarker);
			GuideGroup.ActivateMap(mode);
		}
		else
		{
			GuideGroup.ActivateMap(Mode.Default);
		}
	}

	private static void CallbackAfterTeleport()
	{
		if (Trigger.CheckTrigger("Trigger_ShouldTeleportToLibraryFront") && CurrentFieldID.SectorName != SectorName.Library && !CurrentFieldID.Equals(SectorName.AshHills, 26) && SaveData.TalkData.GetTalkCount("MoldinClearVeritaCalled_Special") <= 0)
		{
			Game.Instance.UIManager.TalkCanvas.ShowSpecialTalk("MoldinClearVeritaCalled_Special").Forget();
			Game.Instance.SaveData.TalkData.AddTalk("MoldinClearVeritaCalled_Special");
		}
	}

	public void CheckAchievementCleared()
	{
		if (!AchievementHelper.IsUnlocked("UnlockAllMaps") && mapPartsDict.All((KeyValuePair<FieldID, MapPart> kvp) => kvp.Value.IsVisited()) && fogParts.All((MapFogPart fog) => fog.IsRevealed()))
		{
			AchievementHelper.Unlock("UnlockAllMaps");
		}
	}
}
