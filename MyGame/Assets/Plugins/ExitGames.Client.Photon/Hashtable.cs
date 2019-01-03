using System.Collections;
using System.Collections.Generic;

namespace ExitGames.Client.Photon
{
	/// <summary>
	/// This is a substitute for the Hashtable class, missing in: Win8RT and Windows Phone. It uses a Dictionary&lt;object,object&gt; as base.
	/// </summary>
	/// <remarks>
	/// Please be aware that this class might act differently than the Hashtable equivalent.
	/// As far as Photon is concerned, the substitution is sufficiently precise.
	/// </remarks>
	public class Hashtable : Dictionary<object, object>
	{
		private DictionaryEntryEnumerator enumerator;

		public new object this[object key]
		{
			get
			{
				object ret = null;
				base.TryGetValue(key, out ret);
				return ret;
			}
			set
			{
				base[key] = value;
			}
		}

		public Hashtable()
		{
		}

		public Hashtable(int x)
			: base(x)
		{
		}

		public new IEnumerator<DictionaryEntry> GetEnumerator()
		{
			return new DictionaryEntryEnumerator(((IDictionary)this).GetEnumerator());
		}

		public override string ToString()
		{
			List<string> temp = new List<string>();
			foreach (object key in base.Keys)
			{
				if (key == null || this[key] == null)
				{
					temp.Add(key + "=" + this[key]);
				}
				else
				{
					temp.Add("(" + key.GetType() + ")" + key + "=(" + this[key].GetType() + ")" + this[key]);
				}
			}
			return string.Join(", ", temp.ToArray());
		}

		/// <summary>
		/// Creates a shallow copy of the Hashtable.
		/// </summary>
		/// <remarks>
		/// A shallow copy of a collection copies only the elements of the collection, whether they are
		/// reference types or value types, but it does not copy the objects that the references refer
		/// to. The references in the new collection point to the same objects that the references in
		/// the original collection point to.
		/// </remarks>
		/// <returns>Shallow copy of the Hashtable.</returns>
		public object Clone()
		{
			return new Dictionary<object, object>(this);
		}
	}
}
