// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Bcx.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Zou.Tasks
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
        [Required] public ITaskItem[]   SourceFiles { get; set; }

        public override bool            Execute()
        {
            foreach (var poFile in this.SourceFiles)
            {
                this.Clean(poFile);
            }
            return !this.Log.HasLoggedErrors;
        }

        private void                    Clean(ITaskItem poFileItem)
        {
            try
            {
                var info = new PoFileInfo(poFileItem);
                if (File.Exists(info.FullPath))
                {
                    this.Log.LogMessageFromText($"CleanPoFile -> \"{info.FullPath}\"", MessageImportance.Normal);

                    var content = File.ReadAllLines(info.FullPath);
                    var contentEnum = content.AsEnumerable().GetEnumerator();

                    var poFile = PoFile.Parse(contentEnum, info);
                    var newContent = poFile.Content.ToArray();

                    File.WriteAllLines(info.FullPath, newContent);
                }
                info.LogWarnings(this.Log);
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e, true);
            }
        }
    }

    internal class PoFileInfo
    {
        public PoFileInfo(ITaskItem item)
        {
            this.FullPath = item.GetMetadata("FullPath");
            this.PoName = Path.GetFileName(this.FullPath);

            this.Bundle = item.GetMetadata("Bundle");
            this.Module = item.GetMetadata("Module");
            this.Domain = item.GetMetadata("Domain");
            var noPotCreationDate = item.GetMetadata("NoPotCreationDate");
            var noPoRevisionDate = item.GetMetadata("NoPoRevisionDate");
            if (!string.IsNullOrEmpty(noPotCreationDate))
            {
                this.NoPotCreationDate = bool.Parse(noPotCreationDate);
            }
            if (!string.IsNullOrEmpty(noPoRevisionDate))
            {
                this.NoPoRevisionDate = bool.Parse(noPoRevisionDate);
            }

            this.Package = string.IsNullOrEmpty(this.Module) ? this.Bundle : this.Module;
            this.ProjectId = string.IsNullOrEmpty(this.Domain)
              ? (string.IsNullOrEmpty(this.Module)
                ? this.Bundle
                : this.Module)
              : this.Domain;

            if (!string.IsNullOrEmpty(this.Domain))
            {
                this.domainRegex = new Regex($"^{this.Domain}\\..{{1,5}}\\.po$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
            }
        }
        
        public string           FullPath          { get; }
        public string           PoName            { get; }
        public string           Bundle            { get; }
        public string           Module            { get; }
        public string           Domain            { get; }
        public string           Package           { get; }
        public string           ProjectId         { get; }
        public bool             NoPotCreationDate { get; }
        public bool             NoPoRevisionDate  { get; }
        public IList<string>    Warnings => this.warnings;

        public bool             KeepPoName(string name)
        {
            if (this.domainRegex != null && !this.domainRegex.Match(name).Success)
            {
                return false;
            }
            return true;
        }
        public bool             KeepPackage(string package)
        {
            if (!string.IsNullOrEmpty(this.Module) && package != this.Package)
            {
                return false;
            }
            return true;
        }
        public override string  ToString() => $"{this.Package}[{this.PoName}]";
        public void             LogWarnings(TaskLoggingHelper log) => this.warnings.ForEach(w => log.LogWarning(w));

        private readonly Regex          domainRegex;
        private readonly List<string>   warnings = new List<string>();

    }
    internal class PoFile
    {
        public static PoFile Parse(IEnumerator<string> e, PoFileInfo info)
        {
            var header = Header.Parse(e, info);
            var messages = Messages.Parse(e, info);

            return new PoFile(header, messages);
        }

        private PoFile(Header header, Messages messages)
        {
            this.Header = header;
            this.Messages = messages;
        }

        public Header               Header { get; }
        public Messages             Messages { get; }
        public IEnumerable<string>  Content => this.Header.Content.Concat(this.Messages.Content);
    }
    internal class Header
    {
        public static Header Parse(IEnumerator<string> e, PoFileInfo fileInfo)
        {
            var content        = ParseContent(e).ToArray();
            var contentEnum    = content.AsEnumerable().GetEnumerator();

            var headerComments = HeaderComments.Parse(contentEnum, fileInfo);
            var headerInfo     = HeaderInfo.Parse(contentEnum, fileInfo);

            return new Header(headerComments, headerInfo);
        }

        private Header(HeaderComments comments, HeaderInfo info)
        {
            this.Comments = comments;
            this.Info     = info;
        }

        public HeaderComments       Comments { get; }
        public HeaderInfo           Info     { get; }
        public IEnumerable<string>  Content => this.Comments.Content.Concat(this.Info.Content);

        private static bool                 IsEmptyMessageMarker(IEnumerator<string> e) => Header.FirstMessageIdMarkerRegex.Match(e.Current).Success;
        private static IEnumerable<string>  ParseContent(IEnumerator<string> e)
        {
            bool ok;
            while ((ok = e.MoveNext()) && !IsEmptyMessageMarker(e))
            {
                yield return e.Current;
            }
            if (ok)
            {
                yield return e.Current;
                while (e.MoveNext() && !e.IsBlankLine())
                {
                    yield return e.Current;
                }
            }
        }
        private static readonly Regex       FirstMessageIdMarkerRegex = new Regex("^msgid\\s+\"\"", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
    internal class Messages
    {
        public static Messages Parse(IEnumerator<string> e, PoFileInfo fileInfo)
        {
            var content = e.ParseRemaining().ToArray();
            content     = CleanContent(content, fileInfo);
            return new Messages(content);
        }

        private Messages(string[] content) => this.Content = content;

        public string[] Content { get; }

        private static string[] CleanContent(string[] content, PoFileInfo fileInfo)
        {
            var e = new Parser(content);
            return Message
              .Parse(e, fileInfo)
              .OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
              .SelectMany(m => m.Content)
              .ToArray();
        }
    }
    internal class Message
    {
        public static Message[] Parse(Parser e, PoFileInfo fileInfo) => ParseCore(e, fileInfo).ToArray();

        private Message(string[] header)
        {
            this.Header = OrderLocations(header);
            this.MsgId  = this.MsgStr = new string[0];
            this.Id     = "zzz";
        }
        private Message(string[] header, string[] msgid, string[] msgstr)
        {
            this.Header = OrderLocations(header);
            this.MsgId  = msgid;
            this.MsgStr = msgstr;
            this.Id     = ParseId(msgid);
        }

        public string           Id     { get; }
        public string[]         Header { get; }
        public string[]         MsgId  { get; }
        public string[]         MsgStr { get; }
        public string[]         Content    => this.Header.Concat(this.MsgId).Concat(this.MsgStr).ToArray();
        public override string  ToString() => this.Id;

        private static IEnumerable<Message> ParseCore(Parser e, PoFileInfo fileInfo)
        {
            while (!e.Completed)
            {
                var header = ParseHeader(e).ToArray();
                if (e.Completed)
                {
                    yield return new Message(header);
                }
                else
                {
                    var msgid = ParseItem(e, "msgid").ToArray();
                    if (msgid.Length > 0)
                    {
                        var msgstr = ParseItem(e, "msgstr").ToArray();
                        if (msgstr.Length > 1 && msgstr[1].StartsWith("\"#-#-#-#-#"))
                        {
                            var conflict = msgstr
                                .Where(m => !m.StartsWith("msgstr") && !m.StartsWith("\"#-#-#-#-#"))
                                .Select(m => m.Replace("\\n\"", "\""))
                                .Distinct();

                            fileInfo.Warnings.Add(
                                $"Translation ambiguity between {string.Join(" and ", conflict)} " +
                                $"for {msgid.First()} in \"{Path.GetFileName(fileInfo.FullPath)}\".");
                        }
                        yield return new Message(header, msgid, msgstr);
                    }
                    else
                    {
                        yield return new Message(header);
                    }
                }
            }
        }
        private static IEnumerable<string>  ParseHeader(Parser e)
        {
            // emit empty lines
            while (!e.Completed && string.IsNullOrWhiteSpace(e.Current))
            {
                yield return e.Current;
                e.MoveNext();
            }
            while (!e.Completed && e.Current.StartsWith("#"))
            {
                yield return e.Current;
                e.MoveNext();
            }
        }
        private static IEnumerable<string>  ParseItem(Parser e, string key)
        {
            if (e.Current.StartsWith(key))
            {
                yield return e.Current;

                while (e.MoveNext() && e.Current.StartsWith("\""))
                {
                    yield return e.Current;
                }
            }
        }
        private static string               ParseId(string[] msgid)
        {
            var msgid0 = Regex.Unescape(msgid[0].Substring(msgid[0].IndexOf('"')).Trim('"'));

            if (msgid.Length == 1)
            {
                return msgid0;
            }
            else
            {
                if (!string.IsNullOrEmpty(msgid0))
                {
                    throw new Exception($"Bad format: first line should be the empty string");
                }

                return msgid
                    .Skip(1)
                    .Select(m => Regex.Unescape(m.Trim('"')))
                    .Join("\n");
            }
        }
        private static string[]             OrderLocations(string[] header)
        {
            // order in place
            if (header.Where(h => h.StartsWith("#: ")).Skip(1).Any())
            {
                var indexedHeader = header
                  .Select((value, i) => new
                  {
                      Index = i,
                      Value = value
                  })
                  .ToArray();

                var locations = indexedHeader
                  .Where(h => h.Value.StartsWith("#: "));

                var indexes = locations.Select(l => l.Index);

                var orderedLocations = locations
                  .OrderBy(h => h.Value.ToLowerInvariant())
                  .Zip(indexes, (ol, i) => new
                  {
                      Index = i,
                      Value = ol.Value
                  });

                var result = header.ToArray(); // make a copy
                orderedLocations.ForEach(ol => result[ol.Index] = ol.Value);
                return result;
            }
            else
            {
                return header;
            }
        }
    }
    internal class HeaderComments
    {
        public static HeaderComments Parse(IEnumerator<string> e, PoFileInfo fileInfo)
        {
            var content = ParseContent(e).ToArray();
            content     = CleanContent(content, fileInfo);
            return new HeaderComments(content);
        }

        private HeaderComments(string[] content) => this.Content = content;

        public string[] Content { get; }

        private static bool                 IsComment(IEnumerator<string> e) => HeaderComments.CommentRegex.Match(e.Current).Success;
        private static IEnumerable<string>  ParseContent(IEnumerator<string> e)
        {
            while (e.MoveNext() && IsComment(e))
            {
                yield return e.Current;
            }
        }
        private static string[]             CleanContent(string[] content, PoFileInfo fileInfo)
        {
            var contentEnum = content.AsEnumerable().GetEnumerator();
            var sections    = CommentSection.Parse(contentEnum, fileInfo).ToArray();
            return sections
                .GroupBy(s => s.Package)
                .OrderBy(g => g.Key)
                .SelectMany(g =>
                {
                    var titleComment =
                        g.Take(1).Select(s => s.Title.Value).Concat(
                        g.Skip(1).Select(s => s.Title.Value).OrderBy(_ => _));
                    var translatorComment = g.First().Body.Content;
                    return titleComment.Concat(translatorComment);
                })
              .ToArray();
        }
        // # Language...
        // #
        private static readonly Regex       CommentRegex = new Regex("^(?:#$|# \\S{1})", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
    internal class HeaderInfo
    {
        public static HeaderInfo Parse(IEnumerator<string> e, PoFileInfo fileInfo)
        {
            var content                = e.ParseRemaining().ToArray();
            var minPotCreationDateTime = fileInfo.NoPotCreationDate ? null : GetMinDateTime(content, PotCreationDateRegex);
            var maxPoRevisionDateTime  = fileInfo.NoPoRevisionDate  ? null : GetMaxDateTime(content, PoRevisionDateRegex);
            content                    = Clean(content, fileInfo, minPotCreationDateTime, maxPoRevisionDateTime);
            return new HeaderInfo(content);
        }

        private HeaderInfo(string[] content) => this.Content = content;

        public string[] Content { get; }

        private static bool                 IsPoMarker(IEnumerator<string> e) => e.Current.StartsWith("\"#-");
        private static string[]             Clean(string[] content, PoFileInfo fileInfo, DateTimeOffset? potCreationDateTime, DateTimeOffset? poRevisionDateTime)
        {
            content = RemoveMarkers(content).ToArray();
            content = content
              // update project ID
              .Select(line => ProjectIdRegex.Replace(line, m => $"{m.Groups[1].Value}{fileInfo.ProjectId}{m.Groups[3].Value}"))
              // update language posix format
              .Select(line => PoLanguageRegex.Replace(line, m => $"{m.Groups[1].Value}{(m.Groups[2].Value.Any() ? "_" : "")}{m.Groups[3].Value}{m.Groups[4].Value}"))
              .ToArray();

            if (fileInfo.NoPotCreationDate)
            {
                content = content
                  .Where(line => !PotCreationDateRegex.Match(line).Success)
                  .ToArray();
            }
            else if (potCreationDateTime != null)
            {
                content = content
                  .Select(line => PotCreationDateRegex.Replace(line, m => $"{m.Groups[1].Value}{potCreationDateTime.Value.Format()}{m.Groups[3].Value}"))
                  .ToArray();
            }

            if (fileInfo.NoPoRevisionDate)
            {
                content = content
                  .Where(line => !PoRevisionDateRegex.Match(line).Success)
                  .ToArray();
            }
            else if (poRevisionDateTime != null)
            {
                content = content
                  .Select(line => PoRevisionDateRegex.Replace(line, m => $"{m.Groups[1].Value}{poRevisionDateTime.Value.Format()}{m.Groups[3].Value}"))
                  .ToArray();
            }
            return content;
        }
        private static IEnumerable<string>  RemoveMarkers(string[] content)
        {
            var e = content.AsEnumerable().GetEnumerator();

            bool ok;
            // emit until first marker
            while ((ok = e.MoveNext()) && !IsPoMarker(e))
            {
                yield return e.Current;
            }
            if (ok)
            {
                // skip first markers group
                while ((ok = e.MoveNext()) && IsPoMarker(e))
                {
                }
                if (ok)
                {
                    // emit until second marker
                    yield return e.Current;
                    while (e.MoveNext() && !IsPoMarker(e))
                    {
                        yield return e.Current;
                    }
                    // skip remaining
                }
            }
        }
        private static DateTimeOffset?      GetMinDateTime(string[] lines, Regex regex)
        {
            var values = GetDateTimes(lines, regex);
            return values.Length == 0 ? default(DateTimeOffset?) : values.Min();
        }
        private static DateTimeOffset?      GetMaxDateTime(string[] lines, Regex regex)
        {
            var values = GetDateTimes(lines, regex);
            return values.Length == 0 ? default(DateTimeOffset?) : values.Max();
        }
        private static DateTimeOffset[]     GetDateTimes(string[] lines, Regex regex)
        {
            return lines
              .Select(line => regex.Match(line))
              .Where(match => match.Success)
              .Select(match => DateTimeOffset.Parse(match.Groups[2].Value))
              .ToArray();
        }

        // "Project-Id-Version: 'zou'\n"
        private static readonly Regex ProjectIdRegex = new Regex("^(\"Project-Id-Version:\\s*')(\\S+.*)('\\s*\\\\n\")\\s*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        // "Language: fr-CH\n"
        private static readonly Regex PoLanguageRegex = new Regex("^(\"Language:\\s*\\w{2,3})(?:(-)(\\w{2,3}))?(\\s*\\\\n\")\\s*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        // "POT-Creation-Date: 2016-05-12 18:17+0200\n"
        private static readonly Regex PotCreationDateRegex = new Regex("^(\"POT-Creation-Date:\\s*)(\\w+.*)(\\s*\\\\n\")\\s*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        // "PO-Revision-Date: 2016-05-12 18:17+0200\n"
        private static readonly Regex PoRevisionDateRegex = new Regex("^(\"PO-Revision-Date:\\s*)(\\w+.*)(\\s*\\\\n\")\\s*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    internal class CommentSectionComparer : IComparer<CommentSection>
    {
        public CommentSectionComparer(PoFileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
        }
        
        public int Compare(CommentSection x, CommentSection y)
        {

            if (x.PoName == y.PoName)
            {
                return 0;
            }
            else if (x.PoName == this.fileInfo.PoName)
            {
                return 1;
            }
            else if (y.PoName == this.fileInfo.PoName)
            {
                return -1;
            }
            else
            {
                var regex = new Regex($"^{x.Package}\\..{{1,5}}\\.po$", RegexOptions.CultureInvariant);
                if (regex.Match(x.PoName).Success)
                {
                    return 1;
                }
                else if (regex.Match(y.PoName).Success)
                {
                    return -1;
                }
            }
            return 0;
        }

        private readonly PoFileInfo fileInfo;
    }
    internal class CommentSection : IEquatable<CommentSection>
    {
        public static CommentSection[] Parse(IEnumerator<string> e, PoFileInfo fileInfo)
        {
            var allSections = ParseCore(e, fileInfo)
              .Distinct(s => s.Title)
              .ToArray();

            // add a domain orphan section if necessary
            if (!string.IsNullOrEmpty(fileInfo.Domain) && !allSections.Where(s => s.Title.PoName == fileInfo.PoName).Any())
            {
                allSections = allSections
                  .StartWith(CreateDomainOrphan(fileInfo))
                  .ToArray();
            }

            var fullSections       = allSections.Where(s => !s.IsOrphan);
            var fromOrphanSections = allSections.Where(s => s.IsOrphan)
                // Convert the orphan section to a full section by retrieving a matching body.
                .Select(orphan =>
                {
                    var samePackageSection = fullSections.FirstOrDefault(s => s.Package == orphan.Package);
                    if (samePackageSection == null)
                    {
                        return null;
                    }
                    return new CommentSection(orphan.Title, samePackageSection.Body);
                })
                .NonNull();

            // Update package.
            allSections = fullSections
              .Concat(fromOrphanSections)
              .Select(s => s.Update(fileInfo))
              .ToArray()
              .Distinct(s => s.Title)
              .Where(s => fileInfo.KeepPackage(s.Package))
              .Where(s => fileInfo.KeepPoName(s.PoName))
              .OrderByDescending(_ => _, new CommentSectionComparer(fileInfo))
              .ToArray();

            return allSections;
        }

        private CommentSection(CommentTitle title, CommentBody body)
        {
            Debug.Assert(title.Package == body.Package);

            this.Title = title;
            this.Body  = body;
        }

        public CommentTitle     Title { get; }
        public CommentBody      Body  { get; }
        public string           PoName   => this.Title.PoName;
        public string           Package  => this.Title.Package;
        public bool             IsOrphan => this.Body.IsOrphan;
        public CommentSection   Update(PoFileInfo fileInfo)
        {
            if (fileInfo.PoName == this.PoName && fileInfo.Package != this.Package)
            {
                var title = this.Title.Update(fileInfo.Package);
                var body = this.Body.Update(fileInfo.Package);
                return new CommentSection(title, body);
            }
            return this;
        }
        public override string  ToString()         => $"{this.Package}[{this.PoName}] IsOrphan = {this.IsOrphan}";
        public override int     GetHashCode()      => this.IsOrphan.GetHashCode() ^ this.Package.GetHashCode() ^ this.PoName.GetHashCode();
        public override bool    Equals(object obj) => this.Equals(obj as CommentSection);
        public bool             Equals(CommentSection other)
        {
            return other != null && this.IsOrphan == other.IsOrphan && this.Package == other.Package && this.PoName == other.PoName;
        }

        private static bool                         IsEmptyComment(IEnumerator<string> e) => e.Current == "#";
        private static IEnumerable<CommentSection>  ParseCore(IEnumerator<string> e, PoFileInfo fileInfo)
        {
            while (true)
            {
                var content = ParseContent(e).ToArray();
                if (content.Length == 0)
                {
                    yield break;
                }
                else
                {
                    foreach (var commentSection in ParseDemux(content, fileInfo))
                    {
                        yield return commentSection;
                    }
                }
            }
        }
        private static IEnumerable<string>          ParseContent(IEnumerator<string> e)
        {
            bool ok;
            // emit until empty comment
            while ((ok = e.MoveNext()) && !IsEmptyComment(e))
            {
                yield return e.Current;
            }
            if (ok)
            {
                yield return e.Current;
            }
        }
        private static IEnumerable<CommentSection>  ParseDemux(string[] content, PoFileInfo fileInfo)
        {
            var e = content.AsEnumerable().GetEnumerator(); // save initial position

            var ok = e.MoveNext();
            if (ok)
            {
                var titles = new CommentTitle[0];

                // check if a title exists
                var match = CommentTitle.PoNamePackageRegex.Match(e.Current);
                if (match.Success)
                {
                    // seek to original position
                    e = content.AsEnumerable().GetEnumerator();
                    titles = CommentTitle.Parse(e);
                }
                else
                {
                    match = CommentBody.PackageRegex.Match(e.Current);
                    if (match.Success)
                    {
                        // title is missing
                        var package = match.Groups[1].Value;
                        titles = new[] { CommentTitle.Create(fileInfo.PoName, package) };
                    }
                }

                var body = CommentBody.Parse(e);
                var fullSections = titles
                  .Where(t => t.Package == body.Package)
                  .Select(t => new CommentSection(t, body));
                var orphanSections = titles
                  .Where(t => t.Package != body.Package)
                  .Select(t => new CommentSection(t, CommentBody.GetEmpty(t.Package)));

                return fullSections.Concat(orphanSections);
            }
            else
            {
                return Enumerable.Empty<CommentSection>();
            }
        }
        private static CommentSection               CreateDomainOrphan(PoFileInfo fileInfo)
        {
            return new CommentSection(CommentTitle.Create(fileInfo.PoName, fileInfo.Package), CommentBody.GetEmpty(fileInfo.Package));
        }
    }
    internal class CommentTitle
    {
        public static CommentTitle[]    Parse(IEnumerator<string> e)
        {
            return ParseCore(e).Distinct().ToArray();
        }
        public static CommentTitle      Create(string poName, string package)
        {
            return new CommentTitle(poName, package, $"# #-#-#-#-#  {poName} ('{package}')  #-#-#-#-#");
        }
        
        private CommentTitle(string poName, string package, string value)
        {
            this.PoName  = poName;
            this.Package = package;
            this.Value   = value;
        }

        public string           PoName  { get; }
        public string           Package { get; }
        public string           Value   { get; }
        public CommentTitle     Update(string package)
        {
            return new CommentTitle(this.PoName, package, this.Value.Replace($"'{this.Package}'", $"'{package}'"));
        }
        public override string  ToString()         => this.Value;
        public override int     GetHashCode()      => this.Value.GetHashCode();
        public override bool    Equals(object obj) => Equals(this.Value, (obj as CommentTitle)?.Value);

        private static IEnumerable<CommentTitle>    ParseCore(IEnumerator<string> e)
        {
            Match m;
            while (e.MoveNext() && (m = PoNamePackageRegex.Match(e.Current)).Success)
            {
                var poName  = m.Groups[1].Value;
                var package = m.Groups[2].Value;

                yield return new CommentTitle(poName, package, e.Current);
            }
        }
        public static readonly Regex                PoNamePackageRegex = new Regex("# #-#-#-#-#\\s+(\\S+.*) \\('(.*)'\\)\\s+#", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
    internal class CommentBody
    {
        public static CommentBody GetEmpty(string package) => CommentBody.EmptyBodies.GetOrAdd(package, pkg => new CommentBody(pkg, Enumerable.Empty<string>()));
        public static CommentBody Parse(IEnumerator<string> e)
        {
            Match m;
            string package = null;
            if ((m = PackageRegex.Match(e.Current)).Success)
            {
                package = m.Groups[1].Value;
            }
            var body = new List<string>
            {
                e.Current
            };
            while (e.MoveNext())
            {
                if (package == null && (m = PackageRegex.Match(e.Current)).Success)
                {
                    package = m.Groups[1].Value;
                }
                body.Add(e.Current);
            }
            return new CommentBody(package, body);
        }

        private CommentBody(string package, IEnumerable<string> content)
        {
            this.Package = package;
            this.Content = content;
        }

        public string               Package { get; }
        public IEnumerable<string>  Content { get; }
        public bool                 IsOrphan => this.Content.IsEmpty();
        public CommentBody          Update(string package)
        {
            var content = this.Content
              .Select(v => v.Replace($"'{this.Package}'", $"'{package}'"))
              .ToArray();
            return new CommentBody(package, content);
        }
        public override string      ToString() => this.Package;

        public static readonly Regex                            PackageRegex = new Regex("# \\S+.*'(.*)'\\s+package\\.", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Dictionary<string, CommentBody> EmptyBodies  = new Dictionary<string, CommentBody>();
    }

    internal static partial class Mixins
    {
        public static string                Format(this DateTimeOffset self)                  => self.ToString("yyyy-MM-dd HH:mmzz00");
        public static bool                  IsBlankLine(this IEnumerator<string> e)           => string.IsNullOrWhiteSpace(e.Current);
        public static bool                  IsTranslatorComment(this IEnumerator<string> e)   => e.Current.StartsWith("#  ");
        public static bool                  IsExtractedComment(this IEnumerator<string> e)    => e.Current.StartsWith("#. ");
        public static bool                  IsReference(this IEnumerator<string> e)           => e.Current.StartsWith("#: ");
        public static bool                  IsFlag(this IEnumerator<string> e)                => e.Current.StartsWith("#, ");
        public static bool                  IsObsolete(this IEnumerator<string> e)            => e.Current.StartsWith("#~");
        public static bool                  IsCommentSectionTitle(this IEnumerator<string> e) => e.Current.StartsWith("# #-#-#-#-#");
        public static IEnumerable<string>   ParseRemaining(this IEnumerator<string> e)
        {
            yield return e.Current;
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
        public static TValue                GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, Func<TKey, TValue> factory)
        {
            if (!self.TryGetValue(key, out var value))
            {
                self.Add(key, value = factory(key));
            }
            return value;
        }
    }

    internal class Parser
    {
        public Parser(string[] content)
        {
            this.enumerator = content.AsEnumerable().GetEnumerator();
            this.ok         = this.enumerator.MoveNext();
        }

        public bool             Completed  => !this.ok;
        public string           Current    => this.ok ? this.enumerator.Current : null;
        public bool             MoveNext() => this.ok ? this.ok = this.enumerator.MoveNext() : false;
        public override string  ToString() => this.ok ? this.enumerator.Current : "<completed>";

        private IEnumerator<string> enumerator;
        private bool                ok;
    }
}
