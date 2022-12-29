using System;
using System.Collections.Generic;
using System.IO;

namespace OWSShared.Extensions
{
	public static class OperatingSystemExtension
	{
        /// <summary>
        /// Cross Platform PathCombine
        /// 
        /// Includes Linux And MacOS Base and Home Directory ~/
        /// </summary>
        /// <param name="pathstring"></param>
        /// <returns></returns>
        public static string PathCombine(string pathstring)
        {
            List<string> segments = new List<string>();

            // Linux & MacOS Fix
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Linux & MacOS Base Directory
                if (pathstring.StartsWith("/") || pathstring.StartsWith(@"\"))
                {
                    segments.Add("/");
                }
                // Linux & MacOS Home Directory
                else if (pathstring.StartsWith("~/"))
                {
                    pathstring = pathstring.Replace("~/", String.Empty);
                    segments.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                }
            }

            char[] separators = { '/', '\\' };
            string[] paths = pathstring.Split(separators);
            segments.AddRange(paths);

            return Path.Combine(segments.ToArray());
        }
    }
}
