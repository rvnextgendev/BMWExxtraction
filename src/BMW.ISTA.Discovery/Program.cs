using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace BMW.ISTA.Discovery;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var options = CaptureOptions.Parse(args);
            if (options.Help)
            {
                PrintHelp();
                return 0;
            }

            using var automation = new UIA3Automation();
            if (options.ListWindows)
            {
                foreach (var window in automation.GetDesktop().FindAllChildren()
                             .Where(x => x.ControlType == FlaUI.Core.Definitions.ControlType.Window))
                {
                    Console.WriteLine($"PID={Safe(() => window.Properties.ProcessId.ValueOrDefault, 0)}  " +
                                      $"Title={Safe(() => window.Name, "(unavailable)")}");
                }
                return 0;
            }

            if (options.Pid is null && string.IsNullOrWhiteSpace(options.Title))
                throw new ArgumentException("Provide --pid <number> or --title <text>. Try --list-windows.");

            var windows = automation.GetDesktop().FindAllChildren()
                .Where(x => x.ControlType == FlaUI.Core.Definitions.ControlType.Window)
                .ToArray();

            var selected = windows.FirstOrDefault(window =>
                options.Pid.HasValue
                    ? Safe(() => window.Properties.ProcessId.ValueOrDefault, -1) == options.Pid
                    : Safe(() => window.Name, "").Contains(options.Title!, StringComparison.OrdinalIgnoreCase));

            if (selected is null)
                throw new InvalidOperationException("No matching top-level window. Try --list-windows.");

            var recorder = new UiTreeRecorder(options.MaxDepth, options.MaxNodes, options.IncludeValues);
            var snapshot = new CaptureDocument(
                DateTimeOffset.UtcNow,
                Safe(() => selected.Properties.ProcessId.ValueOrDefault, -1),
                Safe(() => selected.Name, ""),
                options.MaxDepth,
                options.MaxNodes,
                options.IncludeValues,
                recorder.Capture(selected));

            var output = options.Output ?? Path.Combine("outputs",
                $"{Sanitize(options.Label)}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json");
            var folder = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(output, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
            Console.WriteLine($"Saved: {Path.GetFullPath(output)}");
            Console.WriteLine($"Captured nodes: {recorder.NodeCount}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static T Safe<T>(Func<T> getter, T fallback)
    {
        try { return getter(); }
        catch { return fallback; }
    }

    private static string Sanitize(string name)
    {
        var safe = new string((string.IsNullOrWhiteSpace(name) ? "capture" : name)
            .Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        return safe.Length > 60 ? safe[..60] : safe;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        BMW ISTA UIA Discovery (read-only)
          --list-windows          List top-level UIA windows and their PIDs
          --pid <number>          Target window by process ID
          --title <substring>     Target window by title (alternative to --pid)
          --label <name>          Output label (default: discovery)
          --output <file.json>    Save to an explicit JSON path
          --max-depth <number>    Tree depth limit (default: 10)
          --max-nodes <number>    Node count limit (default: 5000)
          --include-values        Include ValuePattern text (may be sensitive)
          --help                  Show this text
        """);
    }
}

internal sealed record CaptureDocument(
    DateTimeOffset CapturedAtUtc,
    int ProcessId,
    string WindowTitle,
    int MaxDepth,
    int MaxNodes,
    bool IncludedValues,
    UiNode Root);

internal sealed record UiNode(
    string Name,
    string AutomationId,
    string ClassName,
    string ControlType,
    bool? IsEnabled,
    string? BoundingRectangle,
    string? Value,
    List<string> Errors,
    List<UiNode> Children);

internal sealed class UiTreeRecorder(int maxDepth, int maxNodes, bool includeValues)
{
    public int NodeCount { get; private set; }

    public UiNode Capture(AutomationElement element) => Visit(element, 0);

    private UiNode Visit(AutomationElement element, int depth)
    {
        NodeCount++;
        var errors = new List<string>();
        T Read<T>(string name, Func<T> fn, T fallback)
        {
            try { return fn(); }
            catch (Exception ex)
            {
                errors.Add($"{name}: {ex.GetType().Name}: {ex.Message}");
                return fallback;
            }
        }

        string? value = null;
        if (includeValues)
        {
            value = Read("ValuePattern", () =>
            {
                if (!element.Patterns.Value.IsSupported) return null;
                return element.Patterns.Value.Pattern.Value.ValueOrDefault;
            }, (string?)null);
        }

        var node = new UiNode(
            Read("Name", () => element.Name, ""),
            Read("AutomationId", () => element.AutomationId, ""),
            Read("ClassName", () => element.ClassName, ""),
            Read("ControlType", () => element.ControlType.ToString(), ""),
            Read("IsEnabled", () => (bool?)element.IsEnabled, null),
            Read("BoundingRectangle", () => element.BoundingRectangle.ToString(), (string?)null),
            value,
            errors,
            new List<UiNode>());

        if (depth >= maxDepth || NodeCount >= maxNodes) return node;
        var children = Read("Children", () => element.FindAllChildren(), Array.Empty<AutomationElement>());
        foreach (var child in children)
        {
            if (NodeCount >= maxNodes) break;
            node.Children.Add(Visit(child, depth + 1));
        }
        return node;
    }
}

internal sealed record CaptureOptions(
    bool Help, bool ListWindows, int? Pid, string? Title, string Label,
    string? Output, int MaxDepth, int MaxNodes, bool IncludeValues)
{
    public static CaptureOptions Parse(string[] args)
    {
        bool help = false, listWindows = false, includeValues = false;
        int? pid = null;
        string? title = null, output = null;
        string label = "discovery";
        int depth = 10, nodes = 5000;
        for (var i = 0; i < args.Length; i++)
        {
            string Next()
            {
                if (++i >= args.Length) throw new ArgumentException($"Missing value for {args[i - 1]}");
                return args[i];
            }
            switch (args[i])
            {
                case "--help":
                case "-h": help = true; break;
                case "--list-windows": listWindows = true; break;
                case "--include-values": includeValues = true; break;
                case "--pid": pid = int.Parse(Next()); break;
                case "--title": title = Next(); break;
                case "--label": label = Next(); break;
                case "--output": output = Next(); break;
                case "--max-depth": depth = int.Parse(Next()); break;
                case "--max-nodes": nodes = int.Parse(Next()); break;
                default: throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }
        if (depth < 0 || depth > 100) throw new ArgumentException("--max-depth must be between 0 and 100.");
        if (nodes < 1 || nodes > 100000) throw new ArgumentException("--max-nodes must be between 1 and 100000.");
        if (pid is < 1) throw new ArgumentException("--pid must be positive.");
        return new(help, listWindows, pid, title, label, output, depth, nodes, includeValues);
    }
}
