﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Framework.ConfigurationModel;
using Sitecore.Pathfinder.Checking;
using Sitecore.Pathfinder.Configuration;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.IO;
using Sitecore.Pathfinder.Parsing;
using Sitecore.Pathfinder.Projects.Items;
using Sitecore.Pathfinder.Projects.Items.FieldResolvers;
using Sitecore.Pathfinder.Projects.Templates;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Projects
{
    [Export]
    [Export(typeof(IProject))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Project : IProject
    {
        public static readonly IProject Empty = new Project();

        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        private readonly List<IProjectItem> _items = new List<IProjectItem>();

        private string _projectUniqueId;

        [ImportingConstructor]
        public Project([NotNull] ICompositionService compositionService, [NotNull] IConfiguration configuration, [NotNull] IFactoryService factory, [NotNull] IFileSystemService fileSystem, [NotNull] IParseService parseService, [NotNull] ICheckerService checker)
        {
            CompositionService = compositionService;
            Configuration = configuration;
            Factory = factory;
            FileSystem = fileSystem;
            ParseService = parseService;
            Checker = checker;

            Options = ProjectOptions.Empty;
        }

        private Project()
        {
            Options = ProjectOptions.Empty;
            _projectUniqueId = "{00000000-0000-0000-0000-000000000000}";
        }

        public ICollection<Diagnostic> Diagnostics => _diagnostics;

        [NotNull]
        public IFactoryService Factory { get; }

        public IFileSystemService FileSystem { get; }

        public bool HasErrors => Diagnostics.Any(m => m.Severity == Severity.Error);

        public IEnumerable<IProjectItem> Items => _items;

        public ProjectOptions Options { get; private set; }

        public string ProjectUniqueId => _projectUniqueId ?? (_projectUniqueId = Configuration.Get(Constants.Configuration.ProjectUniqueId));

        public ICollection<ISourceFile> SourceFiles { get; } = new List<ISourceFile>();

        public long Ducats { get; set; }

        [NotNull]
        protected ICheckerService Checker { get; }

        [NotNull]
        protected ICompositionService CompositionService { get; }

        [NotNull]
        protected IConfiguration Configuration { get; }

        [NotNull]
        protected IParseService ParseService { get; }

        public virtual void Add(string sourceFileName)
        {
            if (string.IsNullOrEmpty(Options.ProjectDirectory))
            {
                throw new InvalidOperationException(Texts.Project_has_not_been_loaded__Call_Load___first);
            }

            if (SourceFiles.Any(s => string.Compare(s.FileName, sourceFileName, StringComparison.OrdinalIgnoreCase) == 0))
            {
                Remove(sourceFileName);
            }

            var projectFileName = "~/" + PathHelper.NormalizeItemPath(PathHelper.UnmapPath(Options.ProjectDirectory, PathHelper.GetDirectoryAndFileNameWithoutExtensions(sourceFileName))).TrimStart('/');

            var sourceFile = Factory.SourceFile(FileSystem, sourceFileName, projectFileName);
            SourceFiles.Add(sourceFile);

            ParseService.Parse(this, sourceFile);
        }

        public virtual T AddOrMerge<T>(IParseContext context, T projectItem) where T : IProjectItem
        {
            var newItem = projectItem as Item;
            if (newItem != null)
            {
                return (T)MergeItem(context, newItem);
            }

            var newTemplate = projectItem as Template;
            if (newTemplate != null)
            {
                return (T)MergeTemplate(context, newTemplate);
            }

            _items.Add(projectItem);
            return projectItem;
        }

        public virtual void Compile()
        {
            Checker.CheckProject(this);
        }

        [ImportMany]
        public IEnumerable<IFieldResolver> FieldResolvers { get; private set; }

        public virtual IProject Load(ProjectOptions projectOptions, IEnumerable<string> sourceFileNames)
        {
            Options = projectOptions;     

            var context = CompositionService.Resolve<IParseContext>().With(this, Snapshot.Empty);

            var masterExternalReferences = PathHelper.Combine(context.Configuration.GetString(Constants.Configuration.SolutionDirectory), "sitecore.project\\master.master.content.xml");
            if (FileSystem.FileExists(masterExternalReferences))
            {
                Add(masterExternalReferences);
            }

            var coreExternalReferences = PathHelper.Combine(context.Configuration.GetString(Constants.Configuration.SolutionDirectory), "sitecore.project\\core.core.content.xml");
            if (FileSystem.FileExists(coreExternalReferences))
            {
                Add(coreExternalReferences);
            }

            foreach (var sourceFileName in sourceFileNames)
            {
                Add(sourceFileName);
            }

            Compile();

            return this;
        }

        public virtual void Remove(IProjectItem projectItem)
        {
            _items.Remove(projectItem);
        }

        public virtual void Remove(string sourceFileName)
        {
            if (string.IsNullOrEmpty(Options.ProjectDirectory))
            {
                throw new InvalidOperationException(Texts.Project_has_not_been_loaded__Call_Load___first);
            }

            SourceFiles.Remove(SourceFiles.FirstOrDefault(s => string.Compare(s.FileName, sourceFileName, StringComparison.OrdinalIgnoreCase) == 0));

            _diagnostics.RemoveAll(d => string.Compare(d.FileName, sourceFileName, StringComparison.OrdinalIgnoreCase) == 0);

            foreach (var projectItem in Items.ToList())
            {
                // todo: not working
                if (string.Compare(projectItem.Snapshots.First().SourceFile.FileName, sourceFileName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    continue;
                }

                foreach (var item in Items)
                {
                    foreach (var reference in item.References)
                    {
                        var target = reference.Resolve();
                        if (target == projectItem)
                        {
                            reference.Invalidate();
                        }
                    }
                }

                _items.Remove(projectItem);
            }
        }

        public virtual void SaveChanges()
        {
            foreach (var snapshot in Items.SelectMany(i => i.Snapshots))
            {
                if (snapshot.IsModified)
                {
                    snapshot.SaveChanges();
                }
            }
        }

        public IProjectItem FindQualifiedItem(string qualifiedName)
        {
            Guid guid;
            if (Guid.TryParse(qualifiedName, out guid))
            {
                return Items.FirstOrDefault(i => i.Uri.Guid == guid);
            }

            return Items.FirstOrDefault(i => string.Equals(i.QualifiedName, qualifiedName, StringComparison.OrdinalIgnoreCase));
        }

        [NotNull]
        protected virtual IProjectItem MergeItem<T>([NotNull] IParseContext context, [NotNull] T newItem) where T : Item
        {
            Item item = null;
            if (newItem.MergingMatch == MergingMatch.MatchUsingSourceFile)
            {
                item = Items.OfType<Item>().FirstOrDefault(i => string.Compare(i.Snapshots.First().SourceFile.GetFileNameWithoutExtensions(), newItem.Snapshots.First().SourceFile.GetFileNameWithoutExtensions(), StringComparison.OrdinalIgnoreCase) == 0);
            }

            if (item == null)
            {
                item = Items.OfType<Item>().FirstOrDefault(i => i.MergingMatch == MergingMatch.MatchUsingSourceFile && string.Compare(i.Snapshots.First().SourceFile.GetFileNameWithoutExtensions(), newItem.Snapshots.First().SourceFile.GetFileNameWithoutExtensions(), StringComparison.OrdinalIgnoreCase) == 0);
            }

            if (item == null)
            {
                item = Items.OfType<Item>().FirstOrDefault(i => string.Compare(i.ItemIdOrPath, newItem.ItemIdOrPath, StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(i.DatabaseName, newItem.DatabaseName, StringComparison.OrdinalIgnoreCase) == 0);
            }

            if (item == null)
            {
                _items.Add(newItem);
                return newItem;
            }

            item.Merge(context, newItem);
            return item;
        }

        [NotNull]
        protected virtual IProjectItem MergeTemplate<T>([NotNull] IParseContext context, [NotNull] T newTemplate) where T : Template
        {
            var template = Items.OfType<Template>().FirstOrDefault(i => string.Compare(i.ItemIdOrPath, newTemplate.ItemIdOrPath, StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(i.DatabaseName, newTemplate.DatabaseName, StringComparison.OrdinalIgnoreCase) == 0);
            if (template == null)
            {
                _items.Add(newTemplate);
                return newTemplate;
            }

            template.Merge(context, newTemplate);
            return template;
        }
    }
}
