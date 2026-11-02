using UnityEngine;

namespace SPR;

public interface IService
{
	string Name { get; }

	GameObject ServiceGameObject { get; }

	void OnServiceOpening();

	void OnServiceClosing();
}
