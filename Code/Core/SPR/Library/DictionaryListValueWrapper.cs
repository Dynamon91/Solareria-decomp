using System;
using System.Collections.Generic;
using System.Linq;

namespace SPR.Library;

[Serializable]
public class DictionaryListValueWrapper<TKey, TParameter> : DictionaryWrapper<TKey, List<TParameter>>
{
	public int KeyCount => dict?.Count ?? 0;

	public int ValueCount => dict?.Sum((KeyValueSet x) => x.Value.Count) ?? 0;

	public override void AddKey(TKey key)
	{
		dict.Add(new KeyValueSet(key, new List<TParameter>()));
	}

	public void AddValue(TKey key, TParameter element)
	{
		base[key].Add(element);
	}

	public void RemoveValue(TKey key, TParameter element)
	{
		if (ContainsKey(key))
		{
			base[key].Remove(element);
		}
	}

	public bool ContainsValue(TKey key, TParameter element)
	{
		if (ContainsKey(key))
		{
			return base[key].Contains(element);
		}
		return false;
	}

	public bool HasValue(TKey key)
	{
		if (ContainsKey(key))
		{
			List<TParameter> list = base[key];
			if (list == null)
			{
				return false;
			}
			return list.Count > 0;
		}
		return false;
	}
}
