// � 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Emitting;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.IO;
using Sitecore.Pathfinder.Projects.Templates;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Emitters.Writers
{
    public class TemplateWriter
    {
        [Diagnostics.CanBeNull, ItemNotNull]
        private IEnumerable<TemplateSectionWriter> _sectionBuilders;

        public TemplateWriter([Diagnostics.NotNull] Template template)
        {
            Template = template;
        }

        [Diagnostics.CanBeNull]
        public Item Item { get; set; }

        [Diagnostics.NotNull, ItemNotNull]
        public IEnumerable<TemplateSectionWriter> Sections
        {
            get { return _sectionBuilders ?? (_sectionBuilders = Template.Sections.Select(s => new TemplateSectionWriter(s)).ToList()); }
        }

        [Diagnostics.NotNull]
        public Template Template { get; }

        [Diagnostics.CanBeNull]
        public Item Write([Diagnostics.NotNull] IEmitContext context)
        {
            List<Data.Templates.TemplateField> inheritedFields;
            string baseTemplates;

            GetInheritedFields(Template, out inheritedFields, out baseTemplates);

            if (Item == null)
            {
                ResolveItem(context);
            }

            if (Item == null)
            {
                WriteNewTemplate(context, inheritedFields, baseTemplates);
                if (Item == null)
                {
                    return null;
                }
            }
            else
            {
                WriteTemplate(context, inheritedFields, baseTemplates);
                DeleteSections(context);
            }

            SortSections(context, this);

            return Item;
        }

        protected virtual void DeleteFields([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] TemplateSectionWriter templateSectionWriter)
        {
            if (templateSectionWriter.Item == null)
            {
                return;
            }

            foreach (Item child in templateSectionWriter.Item.Children)
            {
                if (child.TemplateID != TemplateIDs.TemplateField)
                {
                    continue;
                }

                var found = false;

                foreach (var field in templateSectionWriter.Fields)
                {
                    if (field.Item == null)
                    {
                        continue;
                    }

                    if (field.Item.ID == child.ID)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    child.Recycle();
                }
            }
        }

        protected virtual void DeleteSections([Diagnostics.NotNull] IEmitContext context)
        {
            if (Item == null)
            {
                return;
            }

            foreach (Item child in Item.Children)
            {
                if (child.TemplateID != TemplateIDs.TemplateSection)
                {
                    continue;
                }

                var found = false;

                foreach (var section in Sections)
                {
                    if (section.Item == null)
                    {
                        continue;
                    }

                    if (section.Item.ID == child.ID)
                    {
                        DeleteFields(context, section);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    child.Recycle();
                }
            }
        }

        
        protected void GetInheritedFields([Diagnostics.NotNull] Template template, [Diagnostics.NotNull, ItemNotNull] out List<Data.Templates.TemplateField> fields, [NotNull] out string baseTemplates)
        {
            fields = new List<Data.Templates.TemplateField>();

            var database = Factory.GetDatabase(template.DatabaseName);
            var baseTemplateList = new List<Item>();

            var templates = template.BaseTemplates.Split(Constants.Pipe, StringSplitOptions.RemoveEmptyEntries);
            foreach (var templateId in templates)
            {
                // resolve possible item paths
                var baseTemplateItem = database.GetItem(templateId);
                if (baseTemplateItem == null)
                {
                    throw new RetryableEmitException(Msg.E1030, Texts.Base_Template_missing, template.Snapshot, templateId);
                }

                baseTemplateList.Add(baseTemplateItem);

                var t = TemplateManager.GetTemplate(baseTemplateItem.ID, database);
                if (t == null)
                {
                    throw new RetryableEmitException(Msg.E1031, Texts.Template_missing, template.Snapshot, templateId);
                }

                var templateFields = t.GetFields(true);

                foreach (var templateField in templateFields)
                {
                    if (fields.All(f => f.Name != templateField.Name))
                    {
                        fields.Add(templateField);
                    }
                }
            }

            baseTemplates = string.Join("|", baseTemplateList.Select(t => t.ID.ToString()));
        }

        [Diagnostics.CanBeNull]
        protected virtual Item GetParentItem([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] Database database)
        {
            var parentPath = PathHelper.GetItemParentPath(Template.ItemIdOrPath);
            if (string.IsNullOrEmpty(parentPath))
            {
                return null;
            }

            var parentItem = database.GetItem(parentPath);
            if (parentItem == null)
            {
                var innerItem = database.GetItem(TemplateIDs.TemplateFolder);
                if (innerItem != null)
                {
                    var templateFolder = new TemplateItem(innerItem);
                    parentItem = database.CreateItemPath(parentPath, templateFolder, templateFolder);
                }
            }

            return parentItem;
        }

        protected virtual void ResolveItem([Diagnostics.NotNull] IEmitContext context)
        {
            var database = Factory.GetDatabase(Template.DatabaseName);
            if (database == null)
            {
                return;
            }

            Item = database.GetItem(new ID(Template.Uri.Guid));
            if (Item == null)
            {
                return;
            }

            foreach (var section in Sections)
            {
                section.ResolveItem(context, Item);
            }
        }

        protected virtual void SortFields([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] TemplateSectionWriter templateSectionWriter)
        {
            var lastSortorder = 0;

            var fields = templateSectionWriter.Fields.ToList();
            for (var index = 0; index < fields.Count; index++)
            {
                var fieldWriter = fields.ElementAt(index);
                if (fieldWriter.Item == null)
                {
                    continue;
                }

                var sortorder = fieldWriter.Item.Appearance.Sortorder;

                if (sortorder <= lastSortorder)
                {
                    var nextSortorder = lastSortorder + 200;

                    if (index < fields.Count - 1)
                    {
                        var nextField = fields.ElementAt(index + 1);
                        if (nextField.Item != null)
                        {
                            nextSortorder = nextField.Item.Appearance.Sortorder;
                            if (nextSortorder < lastSortorder + 2)
                            {
                                nextSortorder = lastSortorder + 200;
                            }
                        }
                    }

                    sortorder = lastSortorder + (nextSortorder - lastSortorder) / 2;

                    using (new EditContext(fieldWriter.Item))
                    {
                        fieldWriter.Item.Appearance.Sortorder = sortorder;
                    }
                }

                lastSortorder = sortorder;
            }
        }

        protected virtual void SortSections([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] TemplateWriter templateWriter)
        {
            var lastSortorder = 0;

            var sections = templateWriter.Sections.ToList();
            for (var index = 0; index < sections.Count; index++)
            {
                var section = sections.ElementAt(index);
                if (section.Item == null)
                {
                    continue;
                }

                var sortorder = section.Item.Appearance.Sortorder;

                if (sortorder <= lastSortorder)
                {
                    var nextSortorder = lastSortorder + 200;

                    if (index < sections.Count - 1)
                    {
                        var nextSection = sections.ElementAt(index + 1);
                        if (nextSection.Item != null)
                        {
                            nextSortorder = nextSection.Item.Appearance.Sortorder;
                            if (nextSortorder < lastSortorder + 2)
                            {
                                nextSortorder = lastSortorder + 200;
                            }
                        }
                    }

                    sortorder = lastSortorder + (nextSortorder - lastSortorder) / 2;

                    using (new EditContext(section.Item))
                    {
                        section.Item.Appearance.Sortorder = sortorder;
                    }
                }

                SortFields(context, section);

                lastSortorder = sortorder;
            }
        }

        protected virtual void WriteField([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] TemplateSectionWriter templateSectionWriter, [Diagnostics.NotNull] TemplateFieldWriter templateFieldWriter, [Diagnostics.NotNull, ItemNotNull] IEnumerable<Data.Templates.TemplateField> inheritedFields)
        {
            if (inheritedFields.Any(f => string.Equals(f.Name, templateFieldWriter.TemplateField.FieldName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var item = templateFieldWriter.Item;

            var isNew = item == null;
            if (isNew)
            {
                var id = new ID(templateFieldWriter.TemplateField.Uri.Guid);
                item = ItemManager.AddFromTemplate(templateFieldWriter.TemplateField.FieldName, new TemplateID(TemplateIDs.TemplateField), templateSectionWriter.Item, id);
                if (item == null)
                {
                    throw new EmitException(Msg.E1032, Texts.Could_not_create_template_field, TraceHelper.GetTextNode(templateFieldWriter.TemplateField.FieldNameProperty), templateFieldWriter.TemplateField.FieldName);
                }

                templateFieldWriter.Item = item;
            }

            if (templateSectionWriter.Item != null && item.ParentID != templateSectionWriter.Item.ID)
            {
                item.MoveTo(templateSectionWriter.Item);
            }

            using (new EditContext(item))
            {
                if (!string.IsNullOrEmpty(templateFieldWriter.TemplateField.FieldName))
                {
                    item.Name = templateFieldWriter.TemplateField.FieldName;
                }

                if (!string.IsNullOrEmpty(templateFieldWriter.TemplateField.Type))
                {
                    item["Type"] = templateFieldWriter.TemplateField.Type;
                }

                item["Shared"] = templateFieldWriter.TemplateField.Shared ? "1" : string.Empty;
                item["Unversioned"] = templateFieldWriter.TemplateField.Unversioned ? "1" : string.Empty;

                if (!string.IsNullOrEmpty(templateFieldWriter.TemplateField.Source))
                {
                    item["Source"] = templateFieldWriter.TemplateField.Source;
                }

                if (!string.IsNullOrEmpty(templateFieldWriter.TemplateField.ShortHelp))
                {
                    item.Help.ToolTip = templateFieldWriter.TemplateField.ShortHelp;
                }

                if (!string.IsNullOrEmpty(templateFieldWriter.TemplateField.LongHelp))
                {
                    item.Help.Text = templateFieldWriter.TemplateField.LongHelp;
                }

                item.Appearance.Sortorder = templateFieldWriter.TemplateField.Sortorder;

                item.UpdateProjectUniqueIds(context);
            }
        }

        protected virtual void WriteNewTemplate([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull, ItemNotNull] IEnumerable<Data.Templates.TemplateField> inheritedFields, [NotNull] string baseTemplates)
        {
            var database = Factory.GetDatabase(Template.DatabaseName);
            if (database == null)
            {
                return;
            }

            var parentItem = GetParentItem(context, database);
            if (parentItem == null)
            {
                throw new RetryableEmitException(Msg.E1033, Texts.Failed_to_create_template, Template.Snapshot);
            }

            var item = ItemManager.AddFromTemplate(Template.ItemName, new TemplateID(TemplateIDs.Template), parentItem, new ID(Template.Uri.Guid));
            if (item == null)
            {
                throw new EmitException(Msg.E1034, Texts.Failed_to_add_new_template, Template.Snapshot);
            }

            Item = item;
            using (new EditContext(item))
            {
                if (!string.IsNullOrEmpty(baseTemplates))
                {
                    item[FieldIDs.BaseTemplate] = baseTemplates;
                }

                if (!string.IsNullOrEmpty(Template.Icon))
                {
                    item.Appearance.Icon = Template.Icon;
                }

                if (!string.IsNullOrEmpty(Template.ShortHelp))
                {
                    item.Help.ToolTip = Template.ShortHelp;
                }

                if (!string.IsNullOrEmpty(Template.LongHelp))
                {
                    item.Help.Text = Template.LongHelp;
                }

                item[FieldIDs.StandardValues] = Template.StandardValuesItem?.Uri.Guid.Format() ?? string.Empty;

                item.UpdateProjectUniqueIds(context);
            }

            foreach (var section in Sections)
            {
                WriteSection(context, section, inheritedFields);
            }
        }

        protected virtual void WriteSection([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] TemplateSectionWriter templateSectionWriter, [Diagnostics.NotNull, ItemNotNull] IEnumerable<Data.Templates.TemplateField> inheritedFields)
        {
            if (Item == null)
            {
                return;
            }

            var isNew = templateSectionWriter.Item == null;
            if (isNew)
            {
                var id = new ID(templateSectionWriter.TemplateSection.Uri.Guid);
                templateSectionWriter.Item = ItemManager.AddFromTemplate(templateSectionWriter.TemplateSection.SectionName, new TemplateID(TemplateIDs.TemplateSection), Item, id);
                if (templateSectionWriter.Item == null)
                {
                    throw new EmitException(Msg.E1035, Texts.Could_not_create_section_item, TraceHelper.GetTextNode(Template.ItemNameProperty));
                }
            }

            if (Item != null && templateSectionWriter.Item.ParentID != Item.ID)
            {
                templateSectionWriter.Item.MoveTo(Item);
            }

            using (new EditContext(templateSectionWriter.Item))
            {
                if (templateSectionWriter.Item.Name != templateSectionWriter.TemplateSection.SectionName)
                {
                    templateSectionWriter.Item.Name = templateSectionWriter.TemplateSection.SectionName;
                }

                if (!string.IsNullOrEmpty(templateSectionWriter.TemplateSection.Icon))
                {
                    templateSectionWriter.Item.Appearance.Icon = templateSectionWriter.TemplateSection.Icon;
                }

                templateSectionWriter.Item.UpdateProjectUniqueIds(context);
            }

            foreach (var fieldWriter in templateSectionWriter.Fields)
            {
                WriteField(context, templateSectionWriter, fieldWriter, inheritedFields);
            }
        }

        protected virtual void WriteTemplate([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull, ItemNotNull] IEnumerable<Data.Templates.TemplateField> inheritedFields, [NotNull] string baseTemplates)
        {
            var item = Item;
            if (item == null)
            {
                return;
            }

            // move
            if (!string.Equals(item.Paths.Path, Template.ItemIdOrPath, StringComparison.OrdinalIgnoreCase) && !string.Equals(item.ID.ToString(), Template.ItemIdOrPath, StringComparison.OrdinalIgnoreCase))
            {
                var parentItemPath = PathHelper.GetItemParentPath(Template.ItemIdOrPath);

                var parentItem = item.Database.GetItem(parentItemPath);
                if (parentItem == null)
                {
                    parentItem = item.Database.CreateItemPath(parentItemPath);
                    if (parentItem == null)
                    {
                        throw new RetryableEmitException(Msg.E1036, Texts.Could_not_create_item, Template.Snapshot, parentItemPath);
                    }
                }

                item.MoveTo(parentItem);
            }

            // rename item and update fields
            using (new EditContext(item))
            {
                item.Name = Template.ItemName;
                item[FieldIDs.BaseTemplate] = baseTemplates;
                item.Appearance.Icon = Template.Icon;
                item.Help.ToolTip = Template.ShortHelp;
                item.Help.Text = Template.LongHelp;
                item[FieldIDs.StandardValues] = Template.StandardValuesItem?.Uri.Guid.Format() ?? string.Empty;

                item.UpdateProjectUniqueIds(context);
            }

            foreach (var templateSectionWriter in Sections)
            {
                WriteSection(context, templateSectionWriter, inheritedFields);
            }
        }
    }
}
