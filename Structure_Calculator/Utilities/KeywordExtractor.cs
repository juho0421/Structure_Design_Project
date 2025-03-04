using System.Text.RegularExpressions;

public static class KeywordExtractor
{
    public static (string Element, string Property, double? Value) ExtractPropertyUpdate(string query)
    {
        var elementMatch = Regex.Match(query, @"""(.*?)""");
        var element = elementMatch.Success ? elementMatch.Groups[1].Value : null;

        var propertyMatch = Regex.Match(query, @"(\w+)\s+(\w+)\s+(\d+\.?\d*)");
        if (propertyMatch.Success)
        {
            var property = propertyMatch.Groups[2].Value;
            var value = double.Parse(propertyMatch.Groups[3].Value);
            return (element, property, value);
        }

        return (element, null, null);
    }
}
