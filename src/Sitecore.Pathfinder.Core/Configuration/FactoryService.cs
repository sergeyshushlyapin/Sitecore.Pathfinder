﻿// © 2015-2016 Sitecore Corporation A/S. All rights reserved.

using System;
using System.ComponentModel.Composition;
using Microsoft.Framework.ConfigurationModel;
using Sitecore.Pathfinder.Compiling.Builders;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.IO;
using Sitecore.Pathfinder.Languages.BinFiles;
using Sitecore.Pathfinder.Languages.ConfigFiles;
using Sitecore.Pathfinder.Languages.Content;
using Sitecore.Pathfinder.Languages.Media;
using Sitecore.Pathfinder.Languages.Renderings;
using Sitecore.Pathfinder.Languages.Serialization;
using Sitecore.Pathfinder.Parsing;
using Sitecore.Pathfinder.Parsing.Items;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Projects.Items;
using Sitecore.Pathfinder.Projects.References;
using Sitecore.Pathfinder.Projects.Templates;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Configuration
{
    [Export(typeof(IFactoryService))]
    public class FactoryService : IFactoryService
    {
        [ImportingConstructor]
        public FactoryService([NotNull] IConfiguration configuration, [NotNull] ICompositionService compositionService)
        {
            Configuration = configuration;
            CompositionService = compositionService;
        }

        [NotNull]
        protected IConfiguration Configuration { get; }

        [NotNull]
        protected ICompositionService CompositionService { get; }

        public virtual BinFile BinFile(IProject project, ISnapshot snapshot, string filePath) => new BinFile(project, snapshot, filePath);

        public ConfigFile ConfigFile(IProject project, ISnapshot snapshot, string filePath) => new ConfigFile(project, snapshot, filePath);

        public virtual IProjectItem ContentFile(IProject project, ISnapshot snapshot, string filePath) => new ContentFile(project, snapshot, filePath);

        public virtual DeviceReference DeviceReference(IProjectItem projectItem, SourceProperty<string> deviceNameSourceProperty) => new DeviceReference(projectItem, deviceNameSourceProperty, deviceNameSourceProperty.GetValue());

        public virtual Diagnostic Diagnostic(int msg, string fileName, TextSpan span, Severity severity, string text) => new Diagnostic(msg, fileName, span, severity, text);

        public virtual Field Field(Item item, ITextNode textNode) => new Field(item, textNode);

        public Field Field(Item item) => new Field(item, Snapshots.TextNode.Empty);

        public Field Field(Item item, ITextNode textNode, string fieldName, string fieldValue)
        {
            var field = new Field(item, textNode);
            field.FieldNameProperty.SetValue(fieldName);
            field.ValueProperty.SetValue(fieldValue);
            return field;
        }

        public FieldBuilder FieldBuilder() => new FieldBuilder(this);

        public virtual FileReference FileReference(IProjectItem owner, SourceProperty<string> sourceSourceProperty, string referenceText) => new FileReference(owner, sourceSourceProperty, referenceText);

        public virtual Item Item(IProject project, ITextNode textNode, Guid guid, string databaseName, string itemName, string itemIdOrPath, string templateIdOrPath) => new Item(project, textNode, guid, databaseName, itemName, itemIdOrPath, templateIdOrPath);

        public Item Item(IProject project, ISnapshot snapshot, Guid guid, string databaseName, string itemName, string itemIdOrPath, string templateIdOrPath) => new Item(project, new SnapshotTextNode(snapshot), guid, databaseName, itemName, itemIdOrPath, templateIdOrPath);

        public ItemBuilder ItemBuilder() => new ItemBuilder(this);

        public virtual ItemParseContext ItemParseContext(IParseContext context, ItemParser itemParser, string databaseName, string parentItemPath, bool isImport) => new ItemParseContext(context, itemParser, databaseName, parentItemPath, isImport);

        public virtual LayoutReference LayoutReference(IProjectItem projectItem, SourceProperty<string> layoutSourceProperty) => new LayoutReference(projectItem, layoutSourceProperty, layoutSourceProperty.GetValue());

        public virtual LayoutRenderingReference LayoutRenderingReference(IProjectItem projectItem, SourceProperty<string> renderingTextNode) => new LayoutRenderingReference(projectItem, renderingTextNode, renderingTextNode.GetValue());

        public virtual MediaFile MediaFile(IProject project, ISnapshot snapshot, string databaseName, string itemName, string itemPath, string filePath) => new MediaFile(project, snapshot, databaseName, itemName, itemPath, filePath);

        public virtual ProjectOptions ProjectOptions(string projectDirectory, string databaseName) => new ProjectOptions(projectDirectory, databaseName);

        public virtual IReference Reference(IProjectItem projectItem, SourceProperty<string> sourceSourceProperty, string referenceText) => new Reference(projectItem, sourceSourceProperty, referenceText);

        public virtual Rendering Rendering(IProject project, ISnapshot snapshot, string databaseName, string itemPath, string itemName, string filePath, string templateIdOrPath) => new Rendering(project, snapshot, databaseName, itemPath, itemName, filePath, templateIdOrPath);

        public virtual SerializationFile SerializationFile(IProject project, ISnapshot snapshot, string filePath) => new SerializationFile(project, snapshot, filePath);

        public virtual ISnapshot Snapshot(ISourceFile sourceFile) => new Snapshot().With(sourceFile);

        public virtual ISourceFile SourceFile(IFileSystemService fileSystem, string absoluteFileName)
        {
            var projectDirectory = Configuration.GetProjectDirectory();

            var relativeFileName = PathHelper.NormalizeFilePath(PathHelper.UnmapPath(projectDirectory, absoluteFileName)).TrimStart('\\');
            var projectFileName = "~/" + PathHelper.NormalizeItemPath(PathHelper.UnmapPath(projectDirectory, PathHelper.GetDirectoryAndFileNameWithoutExtensions(absoluteFileName))).TrimStart('/');

            return new SourceFile(fileSystem, absoluteFileName, relativeFileName, projectFileName);
        }

        public virtual Template Template(IProject project, Guid guid, ITextNode textNode, string databaseName, string itemName, string itemIdOrPath) => new Template(project, textNode, guid, databaseName, itemName, itemIdOrPath);

        public virtual TemplateField TemplateField(Template template, Guid guid, ITextNode templateFieldTextNode) => new TemplateField(template, guid, templateFieldTextNode);

        public virtual TemplateSection TemplateSection(Template template, Guid guid, ITextNode templateSectionTextNode) => new TemplateSection(template, guid, templateSectionTextNode);

        public virtual TextNode TextNode(ISnapshot snapshot, TextSpan span, string name, string value) => new TextNode(snapshot, name, value, span);
    }
}
