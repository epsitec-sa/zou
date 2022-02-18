// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Bcx;
using Bcx.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class Zip : Task
    {

        // Metadata names
        private const string ArchivePath      = "ArchivePath";
        //private const string ArchiveMode      = "ArchiveMode";
        private const string EntryName        = "EntryName";
        private const string CompressionLevel = "CompressionLevel";

        private sealed record EntryMapping(string EntryName, string Path);
        private sealed record Entry(ITaskItem FileItem, EntryMapping Mapping, CompressionLevel CompressionLevel)
        {
            public static Entry     Create(ITaskItem fileItem)
            {
                var compressionLevel         = CompressionLevel.Optimal;
                var compressionLevelMetadata = fileItem.GetMetadata(Zip.CompressionLevel);
                if (!string.IsNullOrEmpty(compressionLevelMetadata))
                {
                    compressionLevel = (CompressionLevel) Enum.Parse(typeof(CompressionLevel), compressionLevelMetadata, true);
                }

                var path      = fileItem.ItemSpec;
                var entryName = fileItem.GetMetadata(Zip.EntryName);
                if (string.IsNullOrEmpty(entryName))
                {
                    entryName = Path.GetFileName(path);
                }
                var mapping = new EntryMapping(entryName, path);
                return new Entry(fileItem, mapping, compressionLevel);
            }

            public DateTimeOffset   CreatedTime          => DateTimeOffset.Parse(this.FileItem.GetMetadata("CreatedTime")).Truncate(TimeSpan.FromSeconds(2));
            public DateTimeOffset   ModifiedTime         => DateTimeOffset.Parse(this.FileItem.GetMetadata("ModifiedTime")).Truncate(TimeSpan.FromSeconds(2));
            public override int     GetHashCode()        => this.Mapping.GetHashCode();
            public bool             Equals(Entry? other) => this.Mapping.Equals(other?.Mapping);
        }

        public bool                     Overwrite { get; set; } = true;
        [Required] public ITaskItem[]   Files     { get; set; }
        [Output]   public ITaskItem[]   Archives  { get; private set; }

        public override bool            Execute()
        {
            //Debugger.Launch();

            this.Archives = this.CreateArchives().ToArray();
            return !this.Log.HasLoggedErrors;
        }

        private IEnumerable<ITaskItem>  CreateArchives()
        {
            try
            {
                // group file items by archive path
                var archivePaths = this.Files.GroupBy(item => item.GetMetadata(ArchivePath)).ToArray();
                return archivePaths.Select(archivePath => this.CreateArchive(Path.GetFullPath(archivePath.Key), archivePath.AsEnumerable()));
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e);
                return Enumerable.Empty<ITaskItem>();
            }
        }
        private ITaskItem               CreateArchive(string archivePath, IEnumerable<ITaskItem> fileItems)
        {
            var archiveMode = Initialize(archivePath, fileItems);

            // verify that entry names are not duplicated
            var entries    = fileItems.Select(Entry.Create).Distinct().ToArray();
            var dupEntries = entries.GroupBy(entry => entry.Mapping.EntryName).Where(g => g.Count() > 1).Flatten().ToArray();
            if (dupEntries.Any())
            {
                throw new Exception($"Duplicate entries in zip archive '{archivePath}': {{{string.Join(", ", dupEntries.ToStringInline())}}}");
            }

            var modified = false;
            using var archive = ZipFile.Open(archivePath, archiveMode);

            if (archiveMode == ZipArchiveMode.Update)
            {
                var archiveEntryNames = archive.Entries.Select(e => e.FullName).ToArray();
                var entryNames        = entries.Select(e => e.Mapping.EntryName).ToArray();
                var toUpdate          = entryNames.Intersect(archiveEntryNames).ToArray();
                var toAdd             = entryNames.Except(archiveEntryNames).ToArray();
                var toDelete          = archiveEntryNames.Except(entryNames).ToArray();

                toDelete.ForEach(entryName => DeleteEntry(archive, entryName));
                toAdd.ForEach(entryName => CreateEntry(archive, GetEntry(entries, entryName)));
                toUpdate.ForEach (entryName =>
                {
                    var entry = GetEntry(entries, entryName);
                    var archiveEntry = archive.GetEntry(entryName);
                    var modifiedTime = entry.ModifiedTime;
                    if (modifiedTime > archiveEntry.LastWriteTime)
                    {
                        archiveEntry = UpdateEntry(archive, entry, archiveEntry);
                    }
                });
            }
            else
            {
                entries.ForEach(entry => CreateEntry(archive, entry));
            }

            if (modified)
            {
                this.Log.LogMessage(MessageImportance.High, $"[+] {archivePath}");
            }
            return new TaskItem(archivePath);

            static ZipArchiveMode Initialize(string archivePath, IEnumerable<ITaskItem> fileItems)
            {
                if (File.Exists(archivePath))
                {
                    return ZipArchiveMode.Update;
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(archivePath));
                    return ZipArchiveMode.Create;
                }
            }
            static Entry GetEntry(Entry[] entries, string entryName)
            {
                return entries.Single(e => e.Mapping.EntryName == entryName);
            }

            void DeleteEntry(ZipArchive archive, string entryName)
            {
                modified = true;
                this.Log.LogMessage(MessageImportance.Normal, $"[-] {entryName}");
                archive.GetEntry(entryName).Delete();
            }
            ZipArchiveEntry CreateEntry(ZipArchive archive, Entry entry)
            {
                modified = true;
                this.Log.LogMessage(MessageImportance.Normal, $"[+] {entry.Mapping.EntryName}");
                return archive.CreateEntryFromFile(entry.Mapping.Path, entry.Mapping.EntryName, entry.CompressionLevel);
            }
            ZipArchiveEntry UpdateEntry(ZipArchive archive, Entry entry, ZipArchiveEntry archiveEntry)
            {
                using (var archiveMemoryStream = new MemoryStream((int) archiveEntry.Length))
                using (var archiveReadStream = archiveEntry.Open())
                {
                    archiveReadStream.CopyTo(archiveMemoryStream);
                    var archiveEntryBytes = archiveMemoryStream.ToArray ();
                    var updatedEntryBytes = File.ReadAllBytes(entry.Mapping.Path);
                    if ((archiveEntryBytes.Length == updatedEntryBytes.Length) &&
                        (Enumerable.SequenceEqual(archiveEntryBytes, updatedEntryBytes)))
                    {
                        this.Log.LogMessage(MessageImportance.Normal, $"[=] {entry.Mapping.EntryName}");
                        return archiveEntry;
                    }
                }
                
                archiveEntry.Delete();
                archiveEntry = CreateEntry(archive, entry);
                return archiveEntry;
            }
        }
    }
}
