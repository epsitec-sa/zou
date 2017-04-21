using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Epsitec.Zou
{
	public class CreateHelpMap: Task
	{
		// Format example:
		//	HIDR_MAINFRAME	0x20002
		[Required]
		public string[] TopicIdValues
		{
			get;
			set;
		}
		// Format example:
		//	html\intro_cresus.htm
		public string DefaultTopicUri
		{
			get;
			set;
		}
		// Format example:
		//	html\afx_hidd_font.htm
		public string[] TopicUris
		{
			get;
			set;
		}
		// Format example:
		//	hidd_newwizard
		public string[] IgnoreTopicIds
		{
			get;
			set;
		}
		// Format example:
		//	hid_edit_navprev		navigation
		public string[] SynonymTopicIds
		{
			get;
			set;
		}
		// Format example:
		//	{ 0x00000, _T("html/intro_cresus.htm") },
		[Output]
		public string[] IndexMappings
		{
			get;
			private set;
		}
		// Format example:
		//	{ 0x2014D, _T("html/hidd_openpreced.htm") },
		[Output]
		public string[] ContextMappings
		{
			get;
			private set;
		}
		public override bool			Execute()
		{
			try
			{
				this.IndexMappings   = this.GetElements (null, new[] { this.DefaultTopicUri }, null).ToArray ();
				this.ContextMappings = this.GetElements (
					this.TopicIdValues,
					this.TopicUris,
					this.SynonymTopicIds)
					.ToArray ();
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}

		private IEnumerable<string> GetElements(string[] topicIdValues, string[] topicUris, string[] synonymTopicIds)
		{
			// Parse topic relative pathes
			// symbol -> relative-path*
			var symPathLookup = topicUris
				.ToLookup (topic => Path.GetFileNameWithoutExtension (topic).ToLowerInvariant ());

			// check that topic file names are unique
			symPathLookup.ForEach (item =>
			{
				if (item.Count () > 1)
				{
					var pathes = string.Join (", ", item);
					this.Log.LogError ($"duplicate topic file names are forbidden: {{ {pathes} }}");
				}
			});

			// Parse synonyms
			// symbol -> symbol*
			var synonymLookup = synonymTopicIds?
				.Select (line => KeyValueRegex.Match (line))
				.Where (match => match.Success)
				.Select (match => new
				{
					Symbol = match.Groups[1].Value,
					Synonym = match.Groups[2].Value.ToLowerInvariant ()
				})
				.ToLookup (a => a.Symbol, a => a.Synonym);

			// Merge synonyms with C/C++ help ID symbols
			// symbol -> values
			var symVals = topicIdValues?
				.Select (line => KeyValueRegex.Match (line))
				.Where (match => match.Success)
				.SelectMany (match =>
				{
					var symbol = match.Groups[1].Value.ToLowerInvariant ();
					var value = int.Parse (match.Groups[2].Value.Substring (2), NumberStyles.HexNumber);
					var synonyms = synonymLookup?[symbol] ?? Enumerable.Empty<string> ();
					return EnumerableEx
						.Return (symbol)
						.Concat (synonyms)
						.Distinct ()
						.Select (sym => new
						{
							Symbol = sym,
							Value = value
						});
				})
				.ToArray ();

			var ignore = this.IgnoreTopicIds ?? Enumerable.Empty<string> ();

			// Keep items matching existing topic files or ignore list
			var symValLookup = symVals?
				.Where (a =>
					symPathLookup.Select(item => item.Key).Contains (a.Symbol, StringComparer.OrdinalIgnoreCase) ||
					ignore.Contains (a.Symbol, StringComparer.OrdinalIgnoreCase))
				.ToLookup (a => a.Symbol, a => a.Value);


			// Compute help context mappings
			var valSymLookup = symValLookup?
				.SelectMany (helpId => helpId.Select (value => new
				{
					Value = value,
					Symbol = helpId.Key,
				}))
				.OrderBy (a => a.Symbol)
				.ToLookup (a => a.Value, a => a.Symbol);

			// Compute help index mappings
			valSymLookup = valSymLookup ?? symPathLookup
				.Select (symPath => new
				{
					Value = 0,
					Symbol = symPath.Key,
				})
				.ToLookup (a => a.Value, a => a.Symbol);

			if (topicIdValues != null && valSymLookup.Count == 0)
			{
				this.Log.LogWarning ("could not find any matching help topic");
			}
			else
			{
				foreach (var valSym in valSymLookup)
				{
					string sym;
					var syms = valSym.Except (ignore, StringComparer.OrdinalIgnoreCase).ToArray ();
					if (syms.Length > 1)
					{
						// favour second column of synonym table (used as a redirection table)
						sym = syms.Where (x => synonymLookup.Contains (x)).Select (x => synonymLookup[x].Last ()).FirstOrDefault () ?? syms.Last ();

						var values = string.Join (" and ", syms.Select (x => $"\"{x}\""));
						var warning = $"topics {values} have the same ID (0x{valSym.Key:X4}), using \"{sym}\"...";
						this.Log.LogWarning (warning);
						yield return $"// WARNING: {warning}";
					}
					else
					{
						sym = syms.FirstOrDefault ();
					}

					string mapElement;
					var url = sym == null ? null : symPathLookup[sym].FirstOrDefault ()?.Replace ('\\', '/');
					if (url == null)
					{
						mapElement = $"{{ 0x{valSym.Key:X5}, _T(\"\") }},	// ignore {valSym.First ()}";
					}
					else
					{
						mapElement = $"{{ 0x{valSym.Key:X5}, _T(\"{url}\") }},";
					}
					yield return mapElement;
				}
			}
		}

		// Match this:
		//   KEY	VALUE		// comment
		// But not C++ line comment:
		//   // C++ line comment

		private static readonly Regex KeyValueRegex = new Regex("^(?<!\\s*//\\s*)(\\w+)\\s*(\\w+)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}
}
