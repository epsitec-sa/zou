using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
				var outLines = this.Clean (lines).ToArray ();
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
	}

	internal static partial class Mixins
	{
		public static bool IsPoMarker(this IEnumerator<string> e) => e.Current.StartsWith ("\"#-");
		public static bool IsBlankLine(this IEnumerator<string> e)    => string.IsNullOrWhiteSpace(e.Current);
	}
}
