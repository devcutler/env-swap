using System.Text.RegularExpressions;

return ParseArguments([.. Environment.GetCommandLineArgs().Skip(1)]);

static int ParseArguments(string[] args)
{
	if (args.Length == 0)
	{
		return ShowHelp(invalidUsage: true);
	}

	if (args.Contains("--help") || args.Contains("-h"))
	{
		return ShowHelp(invalidUsage: false);
	}

	if (args.Contains("--list"))
	{
		return ListEnvironments();
	}

	if (args.Contains("--add"))
	{
		return ParseAddCommand(args);
	}

	string? envName = args.FirstOrDefault(static arg => !arg.StartsWith("--"));
	if (envName != null && IsValidEnvironmentName(envName))
	{
		bool isLocal = args.Contains("--local");
		string[] nonFlagArgs = [.. args.Where(static arg => !arg.StartsWith("--"))];
		string? customTarget = nonFlagArgs.Length > 1 && !isLocal ? nonFlagArgs[1] : null;

		return SwitchEnvironment(envName, isLocal, customTarget);
	}

	return ShowHelp(invalidUsage: true);
}

static int ShowHelp(bool invalidUsage = false)
{
	Console.WriteLine("Usage:");
	Console.WriteLine("  env-swap [environment]                           - Switch to environment");
	Console.WriteLine("  env-swap [environment] [target]                  - Switch to environment (custom target file)");
	Console.WriteLine("  env-swap [environment] --local                   - Switch to environment (copy to .env.local)");
	Console.WriteLine("  env-swap --add [name] [path] [--allow-missing]   - Add environment file path");
	Console.WriteLine("  env-swap --list                                  - List all available environments");
	return invalidUsage ? 1 : 0;
}

static int ListEnvironments()
{
	Dictionary<string, List<string>> config = JsonConfig.LoadConfig();

	if (config.Count == 0)
	{
		Console.WriteLine("No environments configured");
		return 0;
	}

	foreach ((string envName, List<string> paths) in config.OrderBy(static e => e.Key))
	{
		IOrderedEnumerable<string> sortedPaths = paths
			.OrderBy(static p => p.Count(static c => c == '/'))
			.ThenBy(static p => p.Length)
			.ThenBy(static p => p);

		foreach (string? path in sortedPaths)
		{
			string target = GetTargetPath(path).Replace(Path.DirectorySeparatorChar, '/');

			Colors.WriteWithEnvColor(envName, envName);
			Console.Write(": ");
			Colors.WriteWithEnvColor(path, envName);
			Console.Write($" -> {target}");

			if (!File.Exists(path))
			{
				Console.Write(" ");
				Colors.WriteRed("(missing)");
			}
			else
			{
				Console.WriteLine();
			}
		}
	}

	return 0;
}

static int AddEnvironment(string name, string path, bool allowMissing)
{
	if (!File.Exists(path) && !allowMissing)
	{
		Colors.WriteRed($"Error: Source file not found {path}");
		Colors.WriteRed("Use --allow-missing to add non-existent files");
		return 1;
	}

	string normalizedPath = path.Replace(Path.DirectorySeparatorChar, '/');
	Dictionary<string, List<string>> config = JsonConfig.LoadConfig();
	config.TryAdd(name, []);

	if (!config[name].Contains(normalizedPath))
	{
		config[name].Add(normalizedPath);
		JsonConfig.SaveConfig(config);
		Console.Write("Added ");
		Colors.WriteWithEnvColor(normalizedPath, name);
		Console.Write(" to ");
		Colors.WriteWithEnvColor(name, name);
		Console.WriteLine();
	}
	else
	{
		Console.Write("Already exists: ");
		Colors.WriteWithEnvColor(normalizedPath, name);
		Console.Write(" in ");
		Colors.WriteWithEnvColor(name, name);
		Console.WriteLine();
	}

	return 0;
}

static int SwitchEnvironment(string name, bool isLocal = false, string? customTarget = null)
{
	Dictionary<string, List<string>> config = JsonConfig.LoadConfig();

	if (!config.TryGetValue(name, out List<string>? value))
	{
		Colors.WriteRed($"Environment not found: {name}");
		return 1;
	}

	int errorCount = 0;

	foreach (string envPath in value)
	{
		try
		{
			string targetFile = GetTargetPath(envPath, isLocal, customTarget);

			if (!File.Exists(envPath))
			{
				Colors.WriteRed($"Error: Source file not found {envPath}");
				errorCount++;
				continue;
			}

			File.WriteAllText(targetFile, File.ReadAllText(envPath));
			Console.Write("Copied ");
			Colors.WriteWithEnvColor(envPath, name);
			Console.WriteLine($" -> {targetFile}");
		}
		catch (Exception ex)
		{
			Colors.WriteRed($"Error: Failed to process {envPath}: {ex.Message}");
			errorCount++;
		}
	}

	return errorCount == 0 ? 0 : 1;
}

static int ParseAddCommand(string[] args)
{
	int addIndex = Array.IndexOf(args, "--add");
	if (addIndex + 2 >= args.Length)
	{
		return ShowHelp(invalidUsage: true);
	}

	string envName = args[addIndex + 1];
	string path = args[addIndex + 2];
	bool allowMissing = args.Contains("--allow-missing");

	return AddEnvironment(envName, path, allowMissing);
}

static bool IsValidEnvironmentName(string name)
{
	return EnvNameMatcher().IsMatch(name);
}


static string GetTargetPath(string sourcePath, bool local = false, string? customTarget = null)
{
	string dir = Path.GetDirectoryName(sourcePath) ?? ".";

	string targetFileName = customTarget ?? (local ? ".env.local" : ".env");

	return Path.Combine(dir, targetFileName).Replace(Path.DirectorySeparatorChar, '/');
}

internal partial class Program
{
	// way faster to use generated regex
	[GeneratedRegex(@"^[a-zA-Z0-9._:-]+$")]
	internal static partial Regex EnvNameMatcher();
}