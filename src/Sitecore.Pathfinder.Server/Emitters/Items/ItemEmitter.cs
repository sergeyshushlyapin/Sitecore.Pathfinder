﻿// © 2015-2016 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Data.Validators;
using Sitecore.Data.Validators.FieldValidators;
using Sitecore.Data.Validators.ItemValidators;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Emitters.Writers;
using Sitecore.Pathfinder.Emitting;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Projects.Items;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Emitters.Items
{
    public class ItemEmitter : EmitterBase
    {
        public ItemEmitter() : base(Constants.Emitters.Items)
        {
        }

        public override bool CanEmit(IEmitContext context, IProjectItem projectItem)
        {
            return projectItem is Item;
        }

        public override void Emit(IEmitContext context, IProjectItem projectItem)
        {
            var item = projectItem as Item;
            Assert.Cast(item, nameof(item));

            if (!item.IsEmittable || item.IsImport)
            {
                return;
            }

            var templateIdOrPath = ResolveTemplateIdOrPath(item);
            if (string.IsNullOrEmpty(templateIdOrPath))
            {
                throw new RetryableEmitException(Msg.E1013, Texts.Template_missing, TraceHelper.GetTextNode(item.TemplateIdOrPathProperty, item.ItemNameProperty), item.TemplateIdOrPath);
            }

            var database = Factory.GetDatabase(item.DatabaseName);
            var templateItem = database.GetItem(templateIdOrPath);
            if (string.IsNullOrEmpty(templateIdOrPath))
            {
                throw new RetryableEmitException(Msg.E1014, Texts.Template_missing, TraceHelper.GetTextNode(item.TemplateIdOrPathProperty), item.TemplateIdOrPath);
            }

            var template = TemplateManager.GetTemplate(templateItem.ID, templateItem.Database);
            if (template == null)
            {
                throw new RetryableEmitException(Msg.E1015, Texts.Template_missing, TraceHelper.GetTextNode(item.TemplateIdOrPathProperty), item.TemplateIdOrPath);
            }

            // todo: convert to pipeline
            ValidateFields(database, template, item);

            var itemWriter = new ItemWriter
            {
                Snapshot = item.Snapshot,
                DatabaseName = item.DatabaseName,
                Guid = projectItem.Uri.Guid,
                ItemName = item.ItemName,
                ItemIdOrPath = item.ItemIdOrPath,
                TemplateIdOrPath = templateIdOrPath
            };

            foreach (var field in item.Fields)
            {
                var templateField = template.GetField(field.FieldName);
                if (templateField == null)
                {
                    throw new RetryableEmitException(Msg.E1016, Texts.Template_field_missing, TraceHelper.GetTextNode(field.FieldNameProperty, item.ItemNameProperty), field.FieldName);
                }

                var fieldWriter = new FieldWriter(itemWriter, field.FieldIdProperty, field.FieldNameProperty, field.Language, field.Version, field.CompiledValue);
                itemWriter.Fields.Add(fieldWriter);
            }

            var dataItem = itemWriter.Write(context);

            if (context.Configuration.GetBool(Constants.Configuration.BuildProject.RunValidators))
            {
                Validate(context, item, dataItem);
            }
        }

        [Diagnostics.CanBeNull]
        protected string ResolveTemplateIdOrPath([Diagnostics.NotNull] Item item)
        {
            var templateIdOrPath = item.TemplateIdOrPath;
            if (Data.ID.IsID(templateIdOrPath) || templateIdOrPath.StartsWith("/sitecore", StringComparison.OrdinalIgnoreCase))
            {
                return templateIdOrPath;
            }

            var database = Factory.GetDatabase(item.DatabaseName);
            var templates = TemplateManager.GetTemplates(database).Values.ToList();

            // try matching by name only
            var template = templates.FirstOrDefault(t => string.Equals(t.Name, templateIdOrPath, StringComparison.OrdinalIgnoreCase));
            if (template != null)
            {
                return database.GetItem(template.ID)?.Paths.Path;
            }

            // try matching by Xml safe name
            template = templates.FirstOrDefault(t => string.Equals(t.Name.EscapeXmlElementName(), templateIdOrPath, StringComparison.OrdinalIgnoreCase));
            if (template != null)
            {
                return database.GetItem(template.ID)?.Paths.Path;
            }

            // try to see if the item already exists - may have been created elsewhere
            var i = database.GetItem(item.ItemIdOrPath);
            if (i != null)
            {
                return database.GetItem(i.Template.ID)?.Paths.Path;
            }

            return null;
        }

        protected virtual void Validate([Diagnostics.NotNull] IEmitContext context, [Diagnostics.NotNull] Item item, [Diagnostics.NotNull] Data.Items.Item dataItem)
        {
            var validatorCollection = new ValidatorCollection();
            foreach (BaseValidator validator in ValidatorManager.BuildValidators(ValidatorsMode.ValidatorBar, dataItem))
            {
                // remove slow and obsolete validators
                if (validator is FullPageXHtmlValidator)
                {
                    continue;
                }

                if (validator is XhtmlValidator)
                {
                    continue;
                }

                if (validator is W3CXhtmlValidator)
                {
                    continue;
                }

                validatorCollection.Add(validator);
            }

            ValidatorManager.Validate(validatorCollection, new ValidatorOptions(true));

            foreach (BaseValidator validator in validatorCollection)
            {
                var text = validator.Text.TrimEnd('.').Replace('\r', ' ').Replace('\n', ' ');

                var details = validator.GetFieldDisplayName();
                if (details == "[unknown]")
                {
                    details = string.Empty;
                }

                var textNode = item.SourceTextNode;
                if (!Data.ID.IsNullOrEmpty(validator.FieldID))
                {
                    var field = item.Fields.FirstOrDefault(f => f.FieldId == validator.FieldID.Guid);
                    if (field != null)
                    {
                        textNode = TraceHelper.GetTextNode(field.ValueProperty, field.FieldNameProperty, field, item);
                    }
                }

                switch (validator.Result)
                {
                    case ValidatorResult.Suggestion:
                    case ValidatorResult.Warning:
                        context.Trace.TraceWarning(Msg.E1008, text, textNode, details);
                        break;

                    case ValidatorResult.Error:
                    case ValidatorResult.CriticalError:
                    case ValidatorResult.FatalError:
                        context.Trace.TraceError(Msg.E1008, text, textNode, details);
                        break;
                }
            }
        }

        protected void ValidateFields([Diagnostics.NotNull] Data.Database database, [Diagnostics.NotNull] Template template, [Diagnostics.NotNull] Item projectItem)
        {
            var templateFields = template.GetFields(true);

            foreach (var field in projectItem.Fields)
            {
                if (templateFields.All(f => !string.Equals(f.Name, field.FieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new RetryableEmitException(Msg.E1017, Texts.Field_is_not_defined_in_the_template, TraceHelper.GetTextNode(field.ValueProperty, field.FieldNameProperty, field), field.FieldName);
                }

                if (!string.IsNullOrEmpty(field.Language))
                {
                    var language = LanguageManager.GetLanguage(field.Language, database);
                    if (language == null)
                    {
                        throw new RetryableEmitException(Msg.E1018, Texts.Language_not_found, TraceHelper.GetTextNode(field.ValueProperty), field.Language);
                    }
                }

                if (field.FieldName == "__Renderings" || field.FieldName == "__Final Renderings" || field.TemplateField.Type == "Layout")
                {
                    ValidateLayout(database, field, field.CompiledValue);
                }
            }
        }

        protected void ValidateLayout([Diagnostics.NotNull] Data.Database database, [Diagnostics.NotNull] Field field, [Diagnostics.NotNull] string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var root = value.ToXElement();
            if (root == null)
            {
                throw new EmitException(Msg.E1019, Texts.Layout_is_not_valid, TraceHelper.GetTextNode(field.FieldNameProperty, field));
            }

            foreach (var deviceElement in root.Elements())
            {
                var deviceId = deviceElement.GetAttributeValue("id");
                var device = database.GetItem(deviceId);
                if (device == null)
                {
                    throw new RetryableEmitException(Msg.E1020, Texts.Device_not_found, TraceHelper.GetTextNode(field.FieldNameProperty, field), deviceId);
                }

                var layoutId = deviceElement.GetAttributeValue("l");
                if (!string.IsNullOrEmpty(layoutId))
                {
                    var layout = database.GetItem(layoutId);
                    if (layout == null)
                    {
                        throw new RetryableEmitException(Msg.E1021, Texts.Layout_not_found, TraceHelper.GetTextNode(field.FieldNameProperty, field), layoutId);
                    }
                }

                foreach (var renderingElement in deviceElement.Elements())
                {
                    var renderingId = renderingElement.GetAttributeValue("id");
                    var rendering = database.GetItem(renderingId);
                    if (rendering == null)
                    {
                        throw new RetryableEmitException(Msg.E1022, Texts.Rendering_not_found, TraceHelper.GetTextNode(field.FieldNameProperty, field), renderingId);
                    }
                }
            }
        }
    }
}
