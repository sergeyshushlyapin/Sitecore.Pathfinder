﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Sitecore.Pathfinder.Diagnostics
{
    [Export(typeof(IConsoleService))]
    public class ConsoleService : IConsoleService
    {
        public ConsoleColor BackgroundColor
        {
            get { return Console.BackgroundColor; }
            set { Console.BackgroundColor = value; }
        }

        public ConsoleColor ForegroundColor
        {
            get { return Console.ForegroundColor; }
            set { Console.ForegroundColor = value; }
        }

        public bool IsInteractive { get; set; } = true;

        public string Pick(string promptText, Dictionary<string, string> options)
        {
            Console.WriteLine();

            var index = 1;
            foreach (var option in options)
            {
                Console.WriteLine($"{index}: {option.Key}");
                index++;
            }

            while (true)
            {
                Console.Write(promptText);
                var input = Console.ReadLine();

                if (input == "0")
                {
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(input))
                {
                    return options.Values.First();
                }

                int selection;
                if (!int.TryParse(input, out selection))
                {
                    continue;
                }

                if (selection < 1)
                {
                    continue;
                }

                if (selection > options.Count)
                {
                    continue;
                }

                return options.Values.ElementAt(selection - 1);
            }
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public string ReadLine(string promptText, string defaultValue)
        {
            if (!IsInteractive)
            {
                return defaultValue;
            }

            Console.Write(promptText);

            var input = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                input = defaultValue;
            }

            return input;
        }

        public void Write(string format, params object[] arg)
        {
            Console.Write(format, arg);
        }

        public void Write(string text)
        {
            Console.Write(text);
        }

        public void WriteLine(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }

        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public bool? YesNo(string promptText, bool? defaultValue)
        {
            if (!IsInteractive)
            {
                return defaultValue;
            }

            while (true)
            {
                Console.Write(promptText);

                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    return defaultValue;
                }

                if (string.Equals(input, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(input, "N", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                WriteLine("Enter 'Y' or 'N'.");
            }
        }
    }
}
