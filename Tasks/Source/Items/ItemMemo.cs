using Bcx;
using Bcx.IO;
using Bcx.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Epsitec.Zou
{
    public class ItemMemo
	{
		public static IEnumerable<ItemMemo>			Parse(IEnumerable<string> content)
		{
			var cursor = content.GetEnumeraptor ();
			while (!cursor.AtEnd)
			{
				yield return ItemMemo.ParseItem (cursor);
			}
		}
		public static ItemMemo						FromTaskItem(ITaskItem item, string relativeTo, string metadataNames)
		{
			var itemSpec = string.IsNullOrEmpty (relativeTo) ? item.ItemSpec : PathEx.GetRelativePath(relativeTo, item.GetMetadata ("FullPath"));

			var names    = metadataNames == null ? Enumerable.Empty<string> () : metadataNames.Split (';');
			var metadata = names
				.Select (name => new { Key = name, Value = item.GetMetadata (name) })
				.NonNull ()
				.ToDictionary (a => a.Key, a => a.Value);

			return new ItemMemo(itemSpec, metadata);
		}

		public string								Id => this.id;
		public IReadOnlyDictionary<string, string>	Metadata => this.metadata;
		public ITaskItem							ToTaskItem(string rootDir)
		{
			var itemSpec = string.IsNullOrEmpty (rootDir) ? this.id : Path.Combine (rootDir, this.id);
			return new TaskItem (itemSpec, this.metadata);
		}
		public ItemMemo								MergeMetadata(ItemMemo other)
		{
			var newMetadataNames = other.Metadata.Keys.Except (this.Metadata.Keys).ToArray ();
			if (newMetadataNames.Length == 0)
			{
				return this;
			}

			var newMetadata = this.metadata
				.Concat (newMetadataNames.Select (name => new KeyValuePair<string, string> (name, other.metadata[name])))
				.ToDictionary (a => a.Key, a => a.Value);

			return new ItemMemo (this.Id, newMetadata);
		}
		public IEnumerable<string>					Serialize()
		{
			return this.metadata
				.Select (pair => $"  {pair.Key}={pair.Value}")
				.StartWith (this.id);
		}
		public override string ToString() => this.Id;

		private static ItemMemo						ParseItem(IEnumeraptor<string> cursor)
		{
			var itemSpec = cursor.Current;
			var metadata = ItemMemo
				.ParseMetadata (cursor)
				.ToDictionary (pair => pair[0], pair => pair[1]);

			return new ItemMemo (itemSpec, metadata);
		}
		private static IEnumerable<string[]>		ParseMetadata(IEnumeraptor<string> cursor)
		{
			while (cursor.MoveNext () && cursor.Current[0] == ' ')
			{
				yield return ItemMemo.ParseKeyValue (cursor.Current.TrimStart ());
			}
		}
		private static string[]						ParseKeyValue(string keyValue)
		{
			var equalIndex = keyValue.IndexOf ('=');
			return new[] { keyValue.Substring (0, equalIndex), keyValue.Substring (equalIndex + 1) };
		}

		private										ItemMemo(string itemSpec, Dictionary<string, string> metadata)
		{
			this.id = itemSpec;
			this.metadata = metadata;
		}

		private readonly string						id;
		private readonly Dictionary<string, string> metadata;
	}

	public static class ItemMemoExtensions
	{
		public static IEnumerable<string> Serialize(this IEnumerable<ItemMemo> self) => self.SelectMany (memo => memo.Serialize ());
	}
}
