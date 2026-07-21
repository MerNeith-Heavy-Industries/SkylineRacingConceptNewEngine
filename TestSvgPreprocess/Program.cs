using System.Text.RegularExpressions;

// --- Copy of PreprocessSvgClasses from NanoSVG.cs ---

var svgPath = Path.Combine(AppContext.BaseDirectory, "test.svg");
var svg = File.ReadAllText(svgPath);
Console.WriteLine($"Input SVG length: {svg.Length}");
Console.WriteLine($"Contains <style>: {svg.Contains("<style")}");
Console.WriteLine();

// Regexes (exact copies from NanoSVG.cs)
var styleBlockRegex = new Regex(@"<style[^>]*>(.*?)</style>", RegexOptions.Singleline | RegexOptions.Compiled);
var cssRuleRegex = new Regex(@"\.([a-zA-Z0-9_-]+)\s*\{([^}]+)\}", RegexOptions.Compiled);
var cssPropertyRegex = new Regex(@"([a-zA-Z-]+)\s*:\s*([^;]+);?", RegexOptions.Compiled);
var classAttrRegex = new Regex(@"\bclass\s*=\s*""([^""]*)""", RegexOptions.Compiled);

// Step 1: Extract <style> blocks
var rawMatch = styleBlockRegex.Match(svg);
Console.WriteLine($"StyleBlockRegex matched: {rawMatch.Success}");
if (!rawMatch.Success)
{
    Console.WriteLine("FATAL: <style> block not found!");
    return;
}

var styleContent = rawMatch.Groups[1].Value;
Console.WriteLine($"Style content length: {styleContent.Length}");
Console.WriteLine($"First 200 chars: {styleContent[..Math.Min(200, styleContent.Length)]}");
Console.WriteLine();

// Step 2: Parse CSS rules
var classMap = new Dictionary<string, Dictionary<string, string>>();
var ruleMatches = cssRuleRegex.Matches(styleContent);
Console.WriteLine($"CssRuleRegex matched {ruleMatches.Count} rules");

foreach (Match rule in ruleMatches)
{
    var cls = rule.Groups[1].Value;
    Console.WriteLine($"  Found rule: .{cls}");

    if (!classMap.TryGetValue(cls, out var props))
    {
        props = new Dictionary<string, string>();
        classMap[cls] = props;
    }

    var propMatches = cssPropertyRegex.Matches(rule.Groups[2].Value);
    foreach (Match p in propMatches)
    {
        var propName = p.Groups[1].Value.Trim();
        var propValue = p.Groups[2].Value.Trim();
        props[propName] = propValue;
        Console.WriteLine($"    {propName}: {propValue}");
    }
}

Console.WriteLine($"\nclassMap has {classMap.Count} classes");
Console.WriteLine();

// Step 3: Find class= attributes
var classMatches = classAttrRegex.Matches(svg);
Console.WriteLine($"ClassAttrRegex matched {classMatches.Count} attributes");
foreach (Match m in classMatches)
{
    Console.WriteLine($"  class=\"{m.Groups[1].Value}\" at index {m.Index}");
    var classes = m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var cls in classes)
    {
        Console.Write($"    .{cls} -> ");
        if (classMap.TryGetValue(cls, out var props))
            Console.WriteLine(string.Join(", ", props.Select(kv => $"{kv.Key}=\"{kv.Value}\"")));
        else
            Console.WriteLine("NOT FOUND in classMap");
    }
}

var processed = styleBlockRegex.Replace(svg, _ => "");
processed = classAttrRegex.Replace(processed, m =>
{
    var classes = m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var added = new System.Text.StringBuilder();
    var seen = new HashSet<string>();
    foreach (var cls in classes)
    {
        if (classMap.TryGetValue(cls, out var props))
            foreach (var (k, v) in props)
                if (seen.Add(k))
                    added.Append($" {k}=\"{v}\"");
    }
    return m.Value + added.ToString();
});
Console.WriteLine("\n--- Preprocessed SVG (first 500 chars) ---");
Console.WriteLine(processed[..Math.Min(500, processed.Length)]);
Console.WriteLine("...");

Console.WriteLine("\n--- cls-8 path element preview ---");
var cls8Idx = processed.IndexOf("class=\"cls-8\"");
if (cls8Idx >= 0)
{
    // find the <path tag start
    var tagStart = processed.LastIndexOf('<', cls8Idx);
    var tagEnd = processed.IndexOf("/>", tagStart);
    if (tagEnd < 0) tagEnd = processed.IndexOf('>', tagStart);
    Console.WriteLine(processed[tagStart..(tagEnd + 2)]);
}
