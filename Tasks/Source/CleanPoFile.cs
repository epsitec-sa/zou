using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
		public ITaskItem[]				ChildPoFiles
		{
			get;
			set;
		}
		public override bool			Execute()
		{
			var childPoNames = this.ChildPoFiles?.Select (poName => Path.GetFileName (poName.GetMetadata ("Identity"))).ToArray ();
			foreach (var poFile in this.SourceFiles)
			{
				this.CleanHeader (poFile, childPoNames);
			}
			return !this.Log.HasLoggedErrors;
		}

		private void CleanHeader(ITaskItem poFileItem, string[] childPoNames)
		{
			try
			{
				var path = poFileItem.GetMetadata ("FullPath");
				if (File.Exists (path))
				{
					this.Log.LogMessageFromText ($"CleanPoFile -> \"{path}\"", MessageImportance.Low);
					var content = this.ReadSafe (path);
					var contentEnum = content.AsEnumerable ().GetEnumerator ();
					var header = contentEnum.GetHeaderEntry ().ToArray ();
					var headerEnum = header.AsEnumerable ().GetEnumerator ();
					var headerComments = headerEnum.GetComments ().ToArray ().CleanComments (childPoNames);
					var headerBody = headerEnum.GetRemaining ().ToArray ().CleanHeader ();

					var newContent = headerComments
						.Concat (headerBody)
						.Concat (contentEnum.GetRemaining ())
						.ToArray ();

					this.WriteSafe (path, newContent);
				}
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
		}
		private string[] ReadSafe(string path)
		{
			Exception ex = null;
			for (int i = 0; i < 10; ++i)
			{
				try
				{
					return File.ReadAllLines (path);
				}
				catch (IOException e)
				{
					ex = e;
					this.Log.LogWarningFromException (e);
					Thread.Sleep (1000);
				}
			}
			ExceptionDispatchInfo.Capture (ex).Throw ();
			return null;
		}
		private void WriteSafe(string path, string[] lines)
		{
			Exception ex = null;
			for (int i = 0; i < 10; ++i)
			{
				try
				{
					File.WriteAllLines (path, lines);
					return;
				}
				catch (IOException e)
				{
					ex = e;
					this.Log.LogWarningFromException (e);
					Thread.Sleep (1000);
				}
			}
			ExceptionDispatchInfo.Capture (ex).Throw ();
		}
	}

	internal static partial class Mixins
	{
		public static IEnumerable<string>	GetHeaderEntry(this IEnumerator<string> e)
		{
			var ok = true;
			while ((ok = e.MoveNext ()) && !e.IsEmptyMessageMarker ())
			{
				yield return e.Current;
			}
			if (ok)
			{
				yield return e.Current;
				while ((ok = e.MoveNext ()) && !e.IsBlankLine ())
				{
					yield return e.Current;
				}
			}
		}
		public static IEnumerable<string>	GetRemaining(this IEnumerator<string> e)
		{
			yield return e.Current;
			while (e.MoveNext ())
			{
				yield return e.Current;
			}
		}
		public static IEnumerable<string>	GetComments(this IEnumerator<string> e)
		{
			while (e.MoveNext () && e.IsComment ())
			{
				yield return e.Current;
			}
		}
		public static IEnumerable<string>   CleanHeader(this string[] header)
		{
			var maxPotCreationDateTime = header.GetMaxDateTime (PotCreationDateRegex);
			var maxPoRevisionDateTime  = header.GetMaxDateTime (PoRevisionDateRegex);
			var headerEnum             = header.AsEnumerable ().GetEnumerator ();

			header = headerEnum.CleanHeaderMessage ().ToArray ();

			if (maxPotCreationDateTime != null)
			{
				header = header
					.Select (line => Mixins.PotCreationDateRegex.Replace (line, m => $"{m.Groups[1].Value}{maxPotCreationDateTime}{m.Groups[3].Value}"))
					.ToArray ();
			}
			if (maxPoRevisionDateTime != null)
			{
				header = header
					.Select (line => Mixins.PoRevisionDateRegex.Replace (line, m => $"{m.Groups[1].Value}{maxPoRevisionDateTime}{m.Groups[3].Value}"))
					.ToArray ();
			}
			return header;
		}
		public static IEnumerable<string>   CleanComments(this string[] comments, string[] childPoNames)
		{
			var commentEnum = comments.AsEnumerable ().GetEnumerator ();
			var sections = commentEnum.GetCommentSections (childPoNames).ToArray ();
			return sections.Concat().ToArray ();
		}

		private static bool					IsEmptyMessageMarker(this IEnumerator<string> e)	=> Mixins.FirstMessageMarkerRegex.Match(e.Current).Success;
		private static bool					IsComment(this IEnumerator<string> e)				=> Mixins.CommentRegex.Match (e.Current).Success;
		private static bool					IsTranslatorComment(this IEnumerator<string> e)		=> e.Current.StartsWith ("#  ");
		private static bool					IsExtractedComment(this IEnumerator<string> e)		=> e.Current.StartsWith ("#. ");
		private static bool					IsReference(this IEnumerator<string> e)				=> e.Current.StartsWith ("#: ");
		private static bool					IsFlag(this IEnumerator<string> e)					=> e.Current.StartsWith ("#, ");
		private static bool					IsPoMarker(this IEnumerator<string> e)				=> e.Current.StartsWith ("\"#-");
		private static bool					IsEmptyComment(this IEnumerator<string> e)			=> e.Current == "#";
		private static bool					IsCommentSectionTitle(this IEnumerator<string> e)	=> e.Current.StartsWith("# #-#-#-#-#");
		private static bool					IsBlankLine(this IEnumerator<string> e)				=> string.IsNullOrWhiteSpace(e.Current);
		private static IEnumerable<List<string>> GetCommentSections(this IEnumerator<string> e, string[] childPoNames)
		{
			return e.CleanCommentSections (childPoNames)
				.Distinct (s => s.Item1)
				.Select (s => s.Item2)
				.ToArray ();
		}
		private static IEnumerable<Tuple<string, List<string>>> CleanCommentSections(this IEnumerator<string> e, string[] childPoNames)
		{
			while (true)
			{
				var section = e.GetCommentSection ().ToArray ();
				if (section.Length == 0)
				{
					yield break;
				}
				else
				{
					var cleanedSection = section.CleanCommentSection ();
					if (childPoNames == null || childPoNames.Contains (cleanedSection.Item1, StringComparer.OrdinalIgnoreCase))
					{
						yield return cleanedSection;
					}
				}
			}
		}
		private static IEnumerable<string>	GetCommentSection(this IEnumerator<string> e)
		{
			var ok = true;
			// emit until empty comment
			while ((ok = e.MoveNext ()) && !e.IsEmptyComment ())
			{
				yield return e.Current;
			}
			if (ok)
			{
				yield return e.Current;
			}
		}
		private static Tuple<string, List<string>>	CleanCommentSection(this string[] section)
		{
			var e = section.AsEnumerable ().GetEnumerator ();
			var ok = true;
			Match m;
			string poName = null;
			string package = null;
			var content = new List<string> ();
			if ((ok = e.MoveNext ()) && (m = Mixins.CommentSectionTitleRegex.Match(e.Current)).Success)
			{
				poName  = m.Groups[1].Value;
				package = m.Groups[2].Value;

				// emit title
				content.Add (e.Current);
				if (ok)
				{
					// skip duplicate titles
					while ((ok = e.MoveNext ()) && e.IsCommentSectionTitle ())
					{
					}
					if (ok)
					{
						content.Add (e.Current);
						while (e.MoveNext ())
						{
							content.Add (e.Current);
						}
					}
				}
			}
			return Tuple.Create (poName, content);
		}
		private static IEnumerable<string>	CleanHeaderMessage(this IEnumerator<string> e)
		{
			var ok = true;
			// emit until marker
			while ((ok = e.MoveNext ()) && !e.IsPoMarker ())
			{
				yield return e.Current;
			}
			if (ok)
			{
				// skip consecutive markers
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
					// skip remaining
				}
			}
		}
		private static string				GetMaxDateTime(this string[] lines, Regex regex)
		{
			var values = lines
				.Select (line => regex.Match (line))
				.Where (match => match.Success)
				.Select (match => DateTimeOffset.Parse (match.Groups[2].Value))
				.ToArray ();

			return values.Length == 0 ? null : values.Max ().ToString ("yyyy-MM-dd HH:mmzz00");
		}

		// "POT-Creation-Date: 2016-05-12 18:17+0200\n"
		private static readonly Regex		PotCreationDateRegex		= new Regex("^(\"POT-Creation-Date:\\s*)(\\d+.*)(\\s*\\\\n\")\\s*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
		// "PO-Revision-Date: 2016-05-12 18:17+0200\n"
		private static readonly Regex		PoRevisionDateRegex			= new Regex("^(\"PO-Revision-Date:\\s*)(\\d+.*)(\\s*\\\\n\")\\s*",  RegexOptions.CultureInvariant | RegexOptions.Compiled);
		// msgstr ""
		private static readonly Regex		FirstMessageMarkerRegex		= new Regex("^msgstr\\s+\"\"", RegexOptions.CultureInvariant | RegexOptions.Compiled);
		// # Language...
		// #
		private static readonly Regex		CommentRegex				= new Regex("^(?:#$|# \\S{1})", RegexOptions.CultureInvariant | RegexOptions.Compiled);
		private static readonly Regex		CommentSectionTitleRegex	= new Regex("# #-#-#-#-#\\s+(\\S+.*) \\('(.*)'\\)\\s+#", RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}
}
