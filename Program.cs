using System.Text.RegularExpressions;
using Newtonsoft.Json;

internal partial class Program
{
	// way faster to use generated regex
	[GeneratedRegex(@"^[a-zA-Z0-9._:-]+$")]
	internal static partial Regex EnvNameMatcher();

	private static int Main(string[] args)
	{
		var parser = new ArgParser(Environment.GetCommandLineArgs().Skip(1).ToArray());

		if (!parser.HasMore)
		{
			return ShowHelp(invalidUsage: true);
		}

		try
		{
			var firstArg = parser.Peek()!;
			if (firstArg is "--help" or "-h" or "--list" or "--add" or "--remove")
			{
				var arg = parser.Consume()!;
				return arg switch
				{
					"--help" or "-h" => ShowHelp(),
					"--list" => ListEnvironments(),
					"--add" => HandleAddCommand(parser),
					"--remove" => HandleRemoveCommand(parser),
					_ => throw new ArgumentException("option order")
				};
			}

			return HandleSwitchCommand(parser);
		}
		catch (ArgumentException)
		{
			return ShowHelp(invalidUsage: true);
		}

		static int HandleAddCommand(ArgParser parser)
		{
			var args = parser.Expect(2);

			if (args[0].StartsWith("--") || args[1].StartsWith("--"))
			{
				throw new ArgumentException("Environment name and path cannot start with '--'");
			}

			parser.Skip(2);
			return AddEnvironment(args[0], args[1], parser.Contains("--allow-missing"));
		}

		static int HandleRemoveCommand(ArgParser parser)
		{
			if (!parser.HasMore)
			{
				throw new ArgumentException("Remove command requires environment name");
			}

			var envName = parser.Peek()!;
			if (envName.StartsWith("--"))
			{
				throw new ArgumentException("Environment name cannot start with '--'");
			}

			parser.Skip(1);

			if (parser.HasMore && !parser.Peek()!.StartsWith("--"))
			{
				var path = parser.Consume()!;
				return RemoveEnvironment(envName, path);
			}
			else
			{
				if (!parser.Contains("--yes"))
				{
					Colors.WriteRed("This will remove an entire environment. To accept this, pass \"--yes\"");
					return 1;
				}

				var config = LoadConfig();
				if (config.ContainsKey(envName))
				{
					config.Remove(envName);
					SaveConfig(config);
					Console.Write("Removed environment ");
					Colors.WriteWithEnvColor(envName, envName);
					Console.WriteLine();
					return 0;
				}
				else
				{
					Colors.WriteRed($"Environment not found: {envName}");
					return 1;
				}
			}
		}

		static int HandleSwitchCommand(ArgParser parser)
		{
			string? environmentName = null;
			var useLocal = false;
			string? customTarget = null;

			while (parser.HasMore)
			{
				var arg = parser.Consume()!;

				switch (arg)
				{
					case "--local":
						useLocal = true;
						break;
					case "--target":
						if (!parser.TryConsume(out customTarget))
						{
							throw new ArgumentException("--target requires a filename");
						}
						break;
					default:
						if (arg.StartsWith("--"))
						{
							throw new ArgumentException($"Unknown option: {arg}");
						}
						if (environmentName != null)
						{
							throw new ArgumentException("Multiple environment names specified");
						}
						environmentName = arg;
						break;
				}
			}

			if (environmentName == null)
			{
				throw new ArgumentException("Environment name required");
			}

			if (!IsValidEnvironmentName(environmentName))
				return ShowHelp(invalidUsage: true);

			return SwitchEnvironment(environmentName, useLocal, customTarget);
		}

		static bool IsValidEnvironmentName(string name) => EnvNameMatcher().IsMatch(name);

		static int ShowHelp(bool invalidUsage = false)
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("  env-swap [environment] [options]                 - Switch to environment");
			Console.WriteLine("  env-swap --add [name] [path] [--allow-missing]   - Add environment file path");
			Console.WriteLine("  env-swap --remove [name] [path]                  - Remove environment file path");
			Console.WriteLine("  env-swap --list                                  - List all available environments");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("  --local            Copy to .env.local");
			Console.WriteLine("  --target [name]    Custom target filename");
			Console.WriteLine("  --allow-missing    Add non-existent files");
			Console.WriteLine("  --help, -h         Show this help");
			return invalidUsage ? 1 : 0;
		}

		static int ListEnvironments()
		{
			Dictionary<string, List<string>> config = LoadConfig();

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
						Colors.WriteRed(" (missing)");
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
			Dictionary<string, List<string>> config = LoadConfig();
			config.TryAdd(name, []);

			if (!config[name].Contains(normalizedPath))
			{
				config[name].Add(normalizedPath);
				SaveConfig(config);
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
			Dictionary<string, List<string>> config = LoadConfig();

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

		static int RemoveEnvironment(string name, string path)
		{
			string normalizedPath = path.Replace(Path.DirectorySeparatorChar, '/');
			Dictionary<string, List<string>> config = LoadConfig();

			if (!config.TryGetValue(name, out List<string>? paths))
			{
				Colors.WriteRed($"Environment not found: {name}");
				return 1;
			}

			if (!paths.Contains(normalizedPath))
			{
				Colors.WriteRed($"Path not found in environment {name}: {normalizedPath}");
				return 1;
			}

			paths.Remove(normalizedPath);

			if (paths.Count == 0)
			{
				config.Remove(name);
				Console.Write("Removed environment ");
				Colors.WriteWithEnvColor(name, name);
				Console.WriteLine(" (no paths remaining)");
			}
			else
			{
				Console.Write("Removed ");
				Colors.WriteWithEnvColor(normalizedPath, name);
				Console.Write(" from ");
				Colors.WriteWithEnvColor(name, name);
				Console.WriteLine();
			}

			SaveConfig(config);
			return 0;
		}



		static string GetTargetPath(string sourcePath, bool local = false, string? customTarget = null)
		{
			string dir = Path.GetDirectoryName(sourcePath) ?? ".";

			string targetFileName = customTarget ?? (local ? ".env.local" : ".env");

			return Path.Combine(dir, targetFileName).Replace(Path.DirectorySeparatorChar, '/');
		}

		static Dictionary<string, List<string>> LoadConfig()
		{
			const string configFile = ".env.json";
			if (!File.Exists(configFile))
			{
				return [];
			}

			string json = File.ReadAllText(configFile);
			return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? [];
		}

		static void SaveConfig(Dictionary<string, List<string>> config)
		{
			const string configFile = ".env.json";
			string json = JsonConvert.SerializeObject(config, Formatting.Indented);
			File.WriteAllText(configFile, json);
		}
	}
}