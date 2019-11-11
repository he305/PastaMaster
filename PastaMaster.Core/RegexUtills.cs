using System;
using System.Linq;
using System.Text.RegularExpressions;
using Fastenshtein;

namespace PastaMaster.Core
{
    public static class RegexUtills
    {
        public static int GetLevenshteinDistance(string val1, string val2)
        {
            return Levenshtein.Distance(val1, val2);
        }

        public static int GetLevenshteinDistancePercent(string val1, string val2)
        {
            float distance = GetLevenshteinDistance(val1, val2);
            float bigger = Math.Max(val1.Length, val2.Length);
            return (int) ((bigger - distance) / bigger * 100);
        }
    }
}