﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.IO;
using Sitecore.Mvc.Configuration;
using Sitecore.Mvc.Extensions;
using Sitecore.Mvc.Names;
using Sitecore.Mvc.Pipelines.Response.GetRenderer;
using Sitecore.Mvc.Presentation;
using Sitecore.Pathfinder.Mvc.Presentation;

namespace Sitecore.Pathfinder.Mvc.Response.GetRenderer
{
    public class GetMustacheHtmlRenderer : GetRendererProcessor
    {
        public override void Process([Diagnostics.NotNull] GetRendererArgs args)
        {
            if (args.Result != null)
            {
                return;
            }

            args.Result = GetRenderer(args.Rendering, args);
        }

        [Diagnostics.CanBeNull]
        protected virtual Renderer GetRenderer([Diagnostics.NotNull] Rendering rendering, [Diagnostics.NotNull] GetRendererArgs args)
        {
            var filePath = GetViewPath(rendering, args);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            if (!filePath.EndsWith(".mustache.html", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new MustacheHtmlRenderer
            {
                FilePath = filePath,
                Rendering = rendering
            };
        }

        [Diagnostics.CanBeNull]
        protected virtual string GetViewPath([Diagnostics.NotNull] Rendering rendering, [Diagnostics.NotNull] GetRendererArgs args)
        {
            return GetViewPathFromRenderingType(rendering, args) ?? GetViewPathFromRenderingItem(rendering);
        }

        [Diagnostics.CanBeNull]
        private string GetViewPathFromLayoutItem([Diagnostics.NotNull] GetRendererArgs args)
        {
            var filePath = args.LayoutItem.ValueOrDefault(item => item.FilePath);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            if (!MvcSettings.IsViewExtension(Path.GetExtension(filePath)))
            {
                return null;
            }

            return filePath;
        }

        [Diagnostics.CanBeNull]
        private string GetViewPathFromPathProperty([Diagnostics.NotNull] Rendering rendering)
        {
            var filePath = rendering["Path"];

            return string.IsNullOrWhiteSpace(filePath) ? null : filePath;
        }

        [Diagnostics.CanBeNull]
        private string GetViewPathFromRenderingItem([Diagnostics.NotNull] Rendering rendering)
        {
            var renderingItem = rendering.RenderingItem;
            if (renderingItem == null || renderingItem.InnerItem.TemplateID != TemplateIds.ViewRendering)
            {
                return null;
            }

            var filePath = renderingItem.InnerItem["path"];
            return string.IsNullOrWhiteSpace(filePath) ? null : filePath;
        }

        [Diagnostics.CanBeNull]
        private string GetViewPathFromRenderingType([Diagnostics.NotNull] Rendering rendering, [Diagnostics.NotNull] GetRendererArgs args)
        {
            if (rendering.RenderingType == "View")
            {
                return GetViewPathFromPathProperty(rendering);
            }

            if (rendering.RenderingType == "Layout")
            {
                return GetViewPathFromLayoutItem(args);
            }

            return null;
        }
    }
}
