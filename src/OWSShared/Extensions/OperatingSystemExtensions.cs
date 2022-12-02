using System.IO;
using System.Linq;

namespace OWSShared.Extensions
{
	public static class OperatingSystemExtension
	{
        static char[] pathSplitCharacters = new char[] { '\\', '/' };

        public static string PathCombine(params string[] additional)
        {
            var splits = additional.Select(s => s.Split(pathSplitCharacters)).ToArray();
            var totalLength = splits.Sum(arr => arr.Length);
            var segments = new string[totalLength];
            var i = 0;
            foreach (var split in splits)
            {
                foreach (var value in split)
                {
                    segments[i] = value;
                    i++;
                }
            }
            return Path.Combine(segments);
        } 
	}
}
