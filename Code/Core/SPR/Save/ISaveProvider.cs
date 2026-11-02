namespace SPR.Save;

public interface ISaveProvider
{
	string UniqueID { get; }

	void Save(string fileName, GameData data);

	GameData Load(string fileName);

	bool Exists(string fileName);

	void Delete(string fileName);
}
