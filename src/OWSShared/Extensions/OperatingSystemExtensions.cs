using System;
using System.IO;
using System.Linq;

namespace OWSShared.Extensions
{
	public static class OperatingSystemExtension
	{
        public static string PathCombine(params string[] additional)
        {
            var splits = additional.Select(s => s.Split(pathSplitCharacters)).ToArray();
            var totalLength = splits.Sum(arr => arr.Length);
            var segments = new string[totalLength + 1];
            segments[0] = "";

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                if (additional.First().StartsWith("/") || additional.First().StartsWith(@"\"))
                {
                    segments[0] = "/";
                }
                else if (additional.First().StartsWith("~/"))
                {
                    additional = additional.Skip(1).ToArray();
                    splits = additional.Select(s => s.Split(pathSplitCharacters)).ToArray();
                    totalLength = splits.Sum(arr => arr.Length);
                    segments = new string[totalLength + 1];
                    segments[0] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                }
            }

            var i = 0;
            foreach (var split in splits)
            {
                foreach (var value in split)
                {
                    i++;
                    segments[i] = value;
                }
            }
            return Path.Combine(segments);
        }

        static char[] pathSplitCharacters = new char[] { '/', '\\' };
    }
}
