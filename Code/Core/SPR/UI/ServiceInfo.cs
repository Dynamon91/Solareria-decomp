using System;
using SPR.Field;
using UnityEngine;

namespace SPR.UI;

[Serializable]
public struct ServiceInfo
{
	public ServiceName serviceName;

	public Vector2 canvasPivot;

	public GameObject vcam;
}
