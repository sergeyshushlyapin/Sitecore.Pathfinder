// � 2015 Sitecore Corporation A/S. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensions;

namespace Sitecore.Pathfinder.Languages.Renderings
{
    public class ViewRenderingParser : RenderingParser
    {
        [NotNull]
        private static readonly Regex PlaceholderRegex = new Regex("\\@Html\\.Sitecore\\(\\)\\.Placeholder\\(([^\"\\)]*)\"([^\"]*)\"\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ViewRenderingParser() : base(".cshtml", Constants.Templates.ViewRendering)
        {
        }

        protected override IEnumerable<string> GetPlaceholders(string contents)
        {
            var matches = PlaceholderRegex.Matches(contents);

            var result = new List<string>();
            foreach (var match in matches.OfType<Match>())
            {
                var prefix = match.Groups[1].ToString().Trim();
                var name = match.Groups[2].ToString().Trim();

                if (!string.IsNullOrEmpty(prefix))
                {
                    if (name.StartsWith("."))
                    {
                        name = name.Mid(1);
                    }

                    name = "$Id." + name;
                }

                result.Add(name);
            }

            return result;
        }
    }
}
