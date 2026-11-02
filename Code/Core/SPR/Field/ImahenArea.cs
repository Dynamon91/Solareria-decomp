using System.Collections.Generic;
using UnityEngine;

namespace SPR.Field;

public class ImahenArea : SubArea
{
	private readonly List<ImahenSection> imahenSections = new List<ImahenSection>();

	public ImahenCoreMachine ImahenCoreMachine { get; private set; }

	protected override void OnInitialize()
	{
		FieldController.Platform.PathFinder.BakeGraph(areaBox.LowerLeft.x, areaBox.UpperRight.x, areaBox.LowerLeft.y, areaBox.UpperRight.y);
		foreach (Transform item in base.transform)
		{
			if (item.TryGetComponent<ImahenSection>(out var component))
			{
				imahenSections.Add(component);
				component.Initialize();
				component.gameObject.SetActive(value: false);
			}
			if (item.TryGetComponent<ImahenCoreMachine>(out var component2))
			{
				ImahenCoreMachine = component2;
				ImahenCoreMachine.InitializeOnInstantiate(-1, ImahenCoreMachine.Position);
			}
		}
	}

	public void Activate()
	{
		foreach (ImahenSection imahenSection in imahenSections)
		{
			imahenSection.gameObject.SetActive(value: true);
		}
	}

	public void Clear()
	{
		foreach (ImahenSection imahenSection in imahenSections)
		{
			imahenSection.OnClear();
		}
	}
}
