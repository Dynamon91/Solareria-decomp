using SPR.Save;

namespace SPR;

public interface IInteractable
{
	string Name { get; }

	bool IsNull { get; }

	int InteractableOrder { get; }

	void AwakeObject(SaveData saveData);

	void Interact();

	void InteractEnter();

	void InteractExit();
}
