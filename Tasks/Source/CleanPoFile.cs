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
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// PoFile:
	///		Header, BlankLine, Messages
	///		
	/// Header:
	///		Comments?, Info
	///		
	///	Comments:
	///		{ CommentSection }
	///		
	///	CommentSection:
	///		{ CommentTitle }, CommentBody
	///		
	///	CommentTitle:
	///		"# #-#-#-#-#  " + PoName + " ('" + PackageName + "')  #-#-#-#-#""
	///		
	///	CommentBody:
	///		"# Language fr-CH translations for '" + PackageName + "' package."
	///		{ "# ..." }
	///		"#"
	///		
	///	Info:
	///		"Project-Id-Version: '" + PackageName + "'\n"
	///		"POT-Creation-Date: " + PotCreationDate + "\n"
	///		"PO-Revision-Date: " + PoRevisionDate + "\n"
	///		...
	///		
	/// Messages:
	///		{ Message }
	/// </remarks>
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
				this.Clean (poFile);
			}
			return !this.Log.HasLoggedErrors;
		}

		private void					Clean(ITaskItem poFileItem)
		{
			try
			{
				var path = poFileItem.GetMetadata ("FullPath");
				if (File.Exists (path))
				{
					this.Log.LogMessageFromText ($"CleanPoFile -> \"{path}\"", MessageImportance.Normal);

					var content     = File.ReadAllLines (path);
					var contentEnum = content.AsEnumerable ().GetEnumerator ();

					var poFile      = PoFile.Parse (contentEnum);
					var newContent  = poFile.Content.ToArray ();

					File.WriteAllLines (path, newContent);
				}
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
		}
		//private string[]				ReadSafe(string path)
		//{
		//	Exception ex = null;
		//	for (int i = 0; i < 10; ++i)
		//	{
		//		try
		//		{
		//			return File.ReadAllLines (path);
		//		}
		//		catch (IOException e)
		//		{
		//			ex = e;
		//			this.Log.LogWarningFromException (e);
		//			Thread.Sleep (1000);
		//		}
		//	}
		//	ExceptionDispatchInfo.Capture (ex).Throw ();
		//	return null;
		//}
		//private void					WriteSafe(string path, string[] lines)
		//{
		//	Exception ex = null;
		//	for (int i = 0; i < 10; ++i)
		//	{
		//		try
		//		{
		//			File.WriteAllLines (path, lines);
		//			return;
		//		}
		//		catch (IOException e)
		//		{
		//			ex = e;
		//			this.Log.LogWarningFromException (e);
		//			Thread.Sleep (1000);
		//		}
		//	}
		//	ExceptionDispatchInfo.Capture (ex).Throw ();
		//}
	}

	internal class PoFile
	{
		public static PoFile				Parse(IEnumerator<string> e)
		{
			var header   = Header.Parse (e);
			var messages = Messages.Parse (e);

			return new PoFile (header, messages);
		}

		public Header						Header
		{
			get;
		}
		public Messages						Messages
		{
			get;
		}
		public IEnumerable<string>			Content
		{
			get
			{
				return Header.Content.Concat (Messages.Content);
			}
		}

		private								PoFile(Header header, Messages messages)
		{
			this.Header   = header;
			this.Messages = messages;
		}
	}
	internal class Header
	{
		public static Header				Parse(IEnumerator<string> e)
		{
			var content     = Header.ParseContent (e).ToArray ();
			var contentEnum = content.AsEnumerable ().GetEnumerator ();

			var comments = HeaderComments.Parse (contentEnum);
			var info     = HeaderInfo.Parse (contentEnum);

			return new Header (comments, info);
		}

		public HeaderComments				Comments
		{
			get;
		}
		public HeaderInfo					Info
		{
			get;
		}
		public IEnumerable<string>			Content
		{
			get
			{
				return Comments.Content.Concat (Info.Content);
			}
		}

		private								Header(HeaderComments comments, HeaderInfo info)
		{
			this.Comments = comments;
			this.Info = info;
		}

		private static bool					IsEmptyMessageMarker(IEnumerator<string> e) => Header.FirstMessageIdMarkerRegex.Match (e.Current).Success;
		private static IEnumerable<string>	ParseContent(IEnumerator<string> e)
		{
			var ok = true;
			while ((ok = e.MoveNext ()) && !Header.IsEmptyMessageMarker (e))
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

		private static readonly Regex		FirstMessageIdMarkerRegex = new Regex("^msgid\\s+\"\"", RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}
	internal class Messages
	{
		public static Messages				Parse(IEnumerator<string> e)
		{
			var content = e.ParseRemaining ().ToArray ();
			return new Messages (content);
		}

		public string[]						Content
		{
			get;
		}

		private								Messages(string[] content)
		{
			this.Content = content;
		}
	}
	internal class HeaderComments
	{
		public static HeaderComments		Parse(IEnumerator<string> e)
		{
			var content = HeaderComments.ParseContent (e).ToArray ();
			content     = HeaderComments.CleanContent (content);
			return new HeaderComments (content);
		}

		public string[]						Content
		{
			get;
		}

		private								HeaderComments(string[] content)
		{
			this.Content = content;
		}

		private static bool					IsComment(IEnumerator<string> e)		=> HeaderComments.CommentRegex.Match (e.Current).Success;
		private static IEnumerable<string>	ParseContent(IEnumerator<string> e)
		{
			while (e.MoveNext () && HeaderComments.IsComment (e))
			{
				yield return e.Current;
			}
		}
		private static string[]				CleanContent(string[] content)
		{
			var contentEnum = content.AsEnumerable ().GetEnumerator ();
			var sections    = CommentSection.Parse (contentEnum);
			//sections.Where (s => s.IsOrphan).ForEach (orphan => orphan.Body = sections.First (s => s.Package == orphan.Package).Body);
			return sections
				.GroupBy (s => s.Package)
				.SelectMany (g => g.Select (s => s.Title.Value).Concat (g.First (s => !s.IsOrphan).Body.Content))
				.ToArray ();
		}
		// # Language...
		// #
		private static readonly Regex		CommentRegex = new Regex("^(?:#$|# \\S{1})", RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}
	internal class HeaderInfo
	{
		public static HeaderInfo			Parse(IEnumerator<string> e)
		{
			var content = e.ParseRemaining ().ToArray ();
			var lastPotCreationDateTime = HeaderInfo.GetLastDateTime (content, PotCreationDateRegex);
			var lastPoRevisionDateTime  = HeaderInfo.GetLastDateTime (content, PoRevisionDateRegex);
			content = HeaderInfo.Clean (content, lastPotCreationDateTime, lastPoRevisionDateTime);
			return new HeaderInfo (content);
		}

		public string[]						Content
		{
			get;
		}

		private								HeaderInfo(string[] content)
		{
			this.Content = content;
		}

		private static bool					IsPoMarker(IEnumerator<string> e)				=> e.Current.StartsWith ("\"#-");
		private static string[]				Clean(string[] content, DateTimeOffset? lastPotCreationDateTime, DateTimeOffset? lastPoRevisionDateTime)
		{
			content = HeaderInfo.RemoveMarkers (content).ToArray ();

			if (lastPotCreationDateTime != null)
			{
				content = content
					.Select (line => HeaderInfo.PotCreationDateRegex.Replace (line, m => $"{m.Groups[1].Value}{lastPotCreationDateTime.Value.Format ()}{m.Groups[3].Value}"))
					.ToArray ();
			}
			if (lastPoRevisionDateTime != null)
			{
				content = content
					.Select (line => HeaderInfo.PoRevisionDateRegex.Replace (line, m => $"{m.Groups[1].Value}{lastPoRevisionDateTime.Value.Format ()}{m.Groups[3].Value}"))
					.ToArray ();
			}
			return content;
		}
		private static IEnumerable<string>	RemoveMarkers(string[] content)
		{
			var e = content.AsEnumerable ().GetEnumerator ();

			var ok = true;
			// emit until first marker
			while ((ok = e.MoveNext ()) && !HeaderInfo.IsPoMarker (e))
			{
				yield return e.Current;
			}
			if (ok)
			{
				// skip first markers group
				while ((ok = e.MoveNext ()) && HeaderInfo.IsPoMarker (e))
				{
				}
				if (ok)
				{
					// emit until second marker
					yield return e.Current;
					while (e.MoveNext () && !HeaderInfo.IsPoMarker (e))
					{
						yield return e.Current;
					}
					// skip remaining
				}
			}
		}
		private static DateTimeOffset?		GetLastDateTime(string[] lines, Regex regex)
		{
			var values = lines
				.Select (line => regex.Match (line))
				.Where (match => match.Success)
				.Select (match => DateTimeOffset.Parse (match.Groups[2].Value))
				.ToArray ();

			return values.Length == 0 ? default (DateTimeOffset?) : values.Max ();
		}

		// "POT-Creation-Date: 2016-05-12 18:17+0200\n"
		private static readonly Regex		PotCreationDateRegex = new Regex("^(\"POT-Creation-Date:\\s*)(\\d+.*)(\\s*\\\\n\")\\s*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
		// "PO-Revision-Date: 2016-05-12 18:17+0200\n"
		private static readonly Regex		PoRevisionDateRegex = new Regex("^(\"PO-Revision-Date:\\s*)(\\d+.*)(\\s*\\\\n\")\\s*",  RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}

	internal class CommentSection : IEquatable <CommentSection>
	{
		public static CommentSection[]				Parse(IEnumerator<string> e)
		{
			var allSections = CommentSection
				.ParseCore (e)
				.Distinct (s => s.Title)
				.ToArray ();

			var fullSections   = allSections.Where (s => !s.IsOrphan);
			var orphanSections = allSections.Where (s => s.IsOrphan)
				// convert the orphan section to a full section by retrieving a matching body
				.Select (orphan => new CommentSection (orphan.Title, fullSections.First (s => s.Package == orphan.Package).Body));

			return fullSections.Concat (orphanSections).ToArray ();
		}

		public CommentTitle							Title
		{
			get;
		}
		public CommentBody							Body
		{
			get;
		}
		public string								PoName				=> this.Title.PoName;
		public string								Package				=> this.Title.Package;
		public bool									IsOrphan			=> this.Body.IsOrphan;
		public override string						ToString()			=> $"{this.Package}[{this.PoName}] IsOrphan = {this.IsOrphan}";
		public override int							GetHashCode()		=> this.IsOrphan.GetHashCode () ^ this.Package.GetHashCode () ^ this.PoName.GetHashCode ();
		public override bool						Equals(object obj)  => this.Equals (obj as CommentSection);
		public bool									Equals(CommentSection other)
		{
			return other != null && this.IsOrphan == other.IsOrphan && this.Package == other.Package && this.PoName == other.PoName;
		}

		private										CommentSection(CommentTitle title, CommentBody body)
		{
			Debug.Assert (title.Package == body.Package);
			this.Title = title;
			this.Body  = body;
		}

		private static bool							IsEmptyComment(IEnumerator<string> e) => e.Current == "#";
		private static IEnumerable<CommentSection>	ParseCore(IEnumerator<string> e)
		{
			while (true)
			{
				var content = CommentSection.ParseContent (e).ToArray ();
				if (content.Length == 0)
				{
					yield break;
				}
				else
				{
					foreach (var commentSection in CommentSection.ParseDemux (content))
					{
						yield return commentSection;
					}
				}
			}
		}
		private static IEnumerable<string>			ParseContent(IEnumerator<string> e)
		{
			var ok = true;
			// emit until empty comment
			while ((ok = e.MoveNext ()) && !CommentSection.IsEmptyComment (e))
			{
				yield return e.Current;
			}
			if (ok)
			{
				yield return e.Current;
			}
		}
		private static IEnumerable<CommentSection>	ParseDemux(string[] content)
		{
			var e      = content.AsEnumerable ().GetEnumerator ();
			var titles = CommentTitle.Parse (e);
			var body   = CommentBody.Parse(e);

			var fullSections = titles
				.Where (t => t.Package == body.Package)
				.Select (t => new CommentSection (t, body));
			var orphanSections = titles
				.Where (t => t.Package != body.Package)
				.Select (t => new CommentSection (t, CommentBody.GetEmpty (t.Package)));

			return fullSections.Concat(orphanSections);
		}

	}
	internal class CommentTitle
	{
		public static CommentTitle[]		Parse(IEnumerator<string> e)
		{
			return CommentTitle.ParseCore (e)
				.Distinct ()
				.ToArray ();
		}

		public string						PoName
		{
			get;
		}
		public string						Package
		{
			get;
		}
		public string						Value
		{
			get;
		}
		public override string				ToString() => this.Value;
		public override int					GetHashCode() => this.Value.GetHashCode ();
		public override bool				Equals(object obj) => object.Equals (this.Value, (obj as CommentTitle)?.Value);

		private								CommentTitle(string poName, string package, string value)
		{
			this.PoName  = poName;
			this.Package = package;
			this.Value   = value;
		}

		private static IEnumerable<CommentTitle>	ParseCore(IEnumerator<string> e)
		{
			bool ok;
			Match m;
			string poName  = null;
			string package = null;
			while ((ok = e.MoveNext ()) && (m = CommentTitle.PoNamePackageRegex.Match (e.Current)).Success)
			{
				poName  = m.Groups[1].Value;
				package = m.Groups[2].Value;

				yield return new CommentTitle (poName, package, e.Current);
			}
		}


		private static readonly Regex		PoNamePackageRegex = new Regex("# #-#-#-#-#\\s+(\\S+.*) \\('(.*)'\\)\\s+#", RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}
	internal class CommentBody
	{
		public static CommentBody			GetEmpty(string package) => CommentBody.EmptyBodies.GetOrAdd (package, pkg => new CommentBody (pkg, Enumerable.Empty<string> ()));
		public static CommentBody			Parse(IEnumerator<string> e)
		{
			Match m;
			string package = null;
			if ((m = CommentBody.PackageRegex.Match (e.Current)).Success)
			{
				package = m.Groups[1].Value;
			}
			var body = new List<string> ();
			body.Add (e.Current);
			while (e.MoveNext ())
			{
				if (package == null && (m = CommentBody.PackageRegex.Match (e.Current)).Success)
				{
					package = m.Groups[1].Value;
				}
				body.Add (e.Current);
			}
			return new CommentBody (package, body);
		}

		public string						Package
		{
			get;
		}
		public IEnumerable<string>			Content
		{
			get;
		}
		public bool							IsOrphan	=> this.Content.IsEmpty ();
		public override string				ToString()	=> this.Package;

		private								CommentBody(string package, IEnumerable<string> content)
		{
			this.Package = package;
			this.Content = content;
		}

		private static readonly Regex       PackageRegex = new Regex( "# \\S+.*'(.*)'\\s+package\\.", RegexOptions.CultureInvariant | RegexOptions.Compiled);
		private static readonly Dictionary<string, CommentBody> EmptyBodies = new Dictionary<string, CommentBody>();
	}


	internal static partial class Mixins
	{
		public static string				Format(this DateTimeOffset self)					=> self.ToString ("yyyy-MM-dd HH:mmzz00");
		public static bool					IsBlankLine(this IEnumerator<string> e)				=> string.IsNullOrWhiteSpace(e.Current);
		public static bool					IsTranslatorComment(this IEnumerator<string> e)		=> e.Current.StartsWith ("#  ");
		public static bool					IsExtractedComment(this IEnumerator<string> e)		=> e.Current.StartsWith ("#. ");
		public static bool					IsReference(this IEnumerator<string> e)				=> e.Current.StartsWith ("#: ");
		public static bool					IsFlag(this IEnumerator<string> e)					=> e.Current.StartsWith ("#, ");
		public static bool					IsCommentSectionTitle(this IEnumerator<string> e)	=> e.Current.StartsWith("# #-#-#-#-#");
		public static IEnumerable<string>	ParseRemaining(this IEnumerator<string> e)
		{
			yield return e.Current;
			while (e.MoveNext ())
			{
				yield return e.Current;
			}
		}

		public static TValue				GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, Func<TKey, TValue> factory)
		{
			TValue value;
			if (!self.TryGetValue (key, out value))
			{
				self.Add (key, value = factory (key));
			}
			return value;
		}
	}
}
