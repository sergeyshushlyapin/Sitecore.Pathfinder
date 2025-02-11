﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using Sitecore.IO;
using Sitecore.Pathfinder.Emitting;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.Languages.BinFiles;
using Sitecore.Pathfinder.Projects;

namespace Sitecore.Pathfinder.Emitters.Files
{
    public class BinFileEmitter : EmitterBase
    {
        public BinFileEmitter() : base(Constants.Emitters.BinFiles)
        {
        }

        public override bool CanEmit(IEmitContext context, IProjectItem projectItem)
        {
            return projectItem is BinFile;
        }

        public override void Emit(IEmitContext context, IProjectItem projectItem)
        {
            var binFile = (BinFile)projectItem;
            var destinationFileName = FileUtil.MapPath(binFile.FilePath);

            if (!CanCopyBinFile(context, binFile, destinationFileName))
            {
                return;
            }

            context.FileSystem.CreateDirectory(Path.GetDirectoryName(destinationFileName) ?? string.Empty);
            context.FileSystem.Copy(projectItem.Snapshot.SourceFile.AbsoluteFileName, destinationFileName, context.ForceUpdate);
        }

        private bool CanCopyBinFile([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] IProjectItem projectItem, [Diagnostics.NotNull] string destinationFileName)
        {
            if (!context.FileSystem.FileExists(destinationFileName))
            {
                return true;
            }

            if (!context.Configuration.GetBool(Constants.Configuration.InstallPackage.CheckBinFileVersion))
            {
                return true;
            }

            var destinationVersion = GetFileVersion(destinationFileName);
            var sourceVersion = GetFileVersion(projectItem.Snapshot.SourceFile.AbsoluteFileName);

            return sourceVersion > destinationVersion;
        }

        [Diagnostics.NotNull]
        private Version GetFileVersion([Diagnostics.NotNull] string fileName)
        {
            var info = FileVersionInfo.GetVersionInfo(fileName);
            return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
        }
    }
}
