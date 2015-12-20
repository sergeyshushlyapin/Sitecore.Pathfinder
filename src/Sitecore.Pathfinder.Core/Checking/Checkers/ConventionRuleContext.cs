﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Rules.Contexts;

namespace Sitecore.Pathfinder.Checking.Checkers
{
    public class ConventionRuleContext : IRuleContext
    {
        public ConventionRuleContext([NotNull] object obj)
        {
            Object = obj;
        }

        public bool IsAborted { get; set; }

        public object Object { get; }
    }
}
