using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class CleanPoFile : Task
	{
		[Required]
		public ITaskItem[]				SourceFiles
		{
			get;
			set;
		}
		public override bool			Execute()
		{
			foreach (var poFile in this.SourceFiles)
			{
				this.CleanHeader (poFile);
			}
			return !this.Log.HasLoggedErrors;
		}

		private void CleanHeader(ITaskItem poFileItem)
		{
			var path = poFileItem.GetMetadata ("FullPath");
			if (File.Exists (path))
			{
				var lines = File.ReadAllLines (path);
				var maxPotCreationDateTime = CleanPoFile.GetMaxDateTime (lines, PotCreationDateRegex);
				var maxPoRevisionDateTime  = CleanPoFile.GetMaxDateTime (lines, PoRevisionDateRegex);
				var outLines = this.Clean (lines).ToArray ();
				if (maxPotCreationDateTime != null)
				{
					outLines = outLines
						.Select (line => CleanPoFile.PotCreationDateRegex.Replace (line, m => $"{m.Groups[1].Value}{maxPotCreationDateTime}{m.Groups[3].Value}"))
						.ToArray ();
				}
				if (maxPoRevisionDateTime != null)
				{
					outLines = outLines
						.Select (line => CleanPoFile.PoRevisionDateRegex.Replace (line, m => $"{m.Groups[1].Value}{maxPoRevisionDateTime}{m.Groups[3].Value}"))
						.ToArray ();
				}
				File.WriteAllLines (path, outLines);
			}
		}

		private IEnumerable<string> Clean(IEnumerable<string> lines)
		{
			var e = lines.GetEnumerator ();
			var ok = true;
			// emit until marker
			while ((ok = e.MoveNext ()) && !e.IsPoMarker ())
			{
				yield return e.Current;
			}
			if (ok)
			{
				// skip markers
				while ((ok = e.MoveNext ()) && e.IsPoMarker ())
				{
				}

				if (ok)
				{
					// emit first after markers
					yield return e.Current;

					// emit until marker
					while ((ok = e.MoveNext ()) && !e.IsPoMarker ())
					{
						yield return e.Current;
					}
					// skip until blank line
					while ((ok = e.MoveNext ()) && !e.IsBlankLine ())
					{
					}
					if (ok)
					{
						// emit blank line
						yield return e.Current;
						// emit remaining
						while (e.MoveNext ())
						{
							yield return e.Current;
						}
					}
				}
			}
		}

		// "POT-Creation-Date: 2016-05-12 18:17+0200\n"
		// "PO-Revision-Date: 2016-05-12 18:17+0200\n"
		private static string GetMaxDateTime(string[] lines, Regex regex)
		{
			var values = lines
				.Select (line => regex.Match (line))
				.Where (match => match.Success)
				.Select (match => DateTimeOffset.Parse (match.Groups[2].Value))
				.ToArray ();

			return values.Length == 0 ? null : values.Max ().ToString ("yyyy-MM-dd HH:mmzz00");
		}
		private static readonly Regex PotCreationDateRegex = new Regex("(\"POT-Creation-Date:\\s*)(\\d+.*)(\\s*\\\\n\")", RegexOptions.CultureInvariant | RegexOptions.Compiled);
		private static readonly Regex PoRevisionDateRegex  = new Regex("(\"PO-Revision-Date:\\s*)(\\d+.*)(\\s*\\\\n\")",  RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}

	internal static partial class Mixins
	{
		public static bool IsPoMarker(this IEnumerator<string> e) => e.Current.StartsWith ("\"#-");
		public static bool IsBlankLine(this IEnumerator<string> e)    => string.IsNullOrWhiteSpace(e.Current);
	}
}
