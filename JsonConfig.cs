using System.Text.Json;

internal static class JsonConfig
{
	internal const string ConfigFile = ".env.json";

	public static Dictionary<string, List<string>> LoadConfig()
	{
		if (!File.Exists(ConfigFile))
		{
			return [];
		}

		JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ConfigFile));
		Dictionary<string, List<string>> result = [];

		foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
		{
			List<string> paths = [];
			foreach (JsonElement item in prop.Value.EnumerateArray())
			{
				if (item.GetString() is string path)
				{
					paths.Add(path);
				}
			}
			result[prop.Name] = paths;
		}

		return result;
	}

	public static void SaveConfig(Dictionary<string, List<string>> config)
	{
		using FileStream stream = File.Create(ConfigFile);
		using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

		writer.WriteStartObject();
		foreach ((string envName, List<string> paths) in config)
		{
			writer.WriteStartArray(envName);
			foreach (string path in paths)
			{
				writer.WriteStringValue(path);
			}

			writer.WriteEndArray();
		}
		writer.WriteEndObject();
	}
}