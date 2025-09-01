internal static class Colors
{
	internal static readonly ConsoleColor[] Palette =
	[
		ConsoleColor.Blue,
		ConsoleColor.Green,
		ConsoleColor.Cyan,
		ConsoleColor.Yellow,
		ConsoleColor.Magenta,
	];

	public static void WriteRed(string text)
	{
		WriteColor(text, ConsoleColor.Red);
	}

	public static void WriteWithEnvColor(string text, string envName)
	{
		string prefix = envName.Length <= 3 ? envName : envName[..3];
		uint hash = GetColorHash(prefix);
		int colorIndex = (int)(hash % (uint)Palette.Length);
		WriteColor(text, Palette[colorIndex], newLine: false);
	}

	internal static uint GetColorHash(string text)
	{
		// slightly modified FNV-1a hashing
		uint hash = 29u ^ 2166136261u;
		foreach (char c in text)
		{
			hash ^= c;
			hash *= 16777619u;
		}
		return hash;
	}

	public static void WriteColor(string text, ConsoleColor color, bool newLine = true)
	{
		if (Console.IsOutputRedirected)
		{
			if (newLine)
			{
				Console.WriteLine(text);
			}
			else
			{
				Console.Write(text);
			}

			return;
		}

		ConsoleColor originalColor = Console.ForegroundColor;
		Console.ForegroundColor = color;

		if (newLine)
		{
			Console.WriteLine(text);
		}
		else
		{
			Console.Write(text);
		}

		Console.ForegroundColor = originalColor;
	}
}