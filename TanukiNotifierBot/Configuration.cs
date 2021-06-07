using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TanukiNotifierBot {
	public class Configuration {
		[JsonConstructor]
		// ReSharper disable once MemberCanBePrivate.Global
		public Configuration(string botToken, string channel) {
			BotToken = botToken;
			Channel = channel;
		}

		public string BotToken { get; }

		public string Channel { get; }

		internal static Configuration Create() {
			string? botToken = null;
			while (string.IsNullOrEmpty(botToken)) {
				Console.WriteLine(nameof(BotToken) + ":");
				botToken = Console.ReadLine();
			}

			string? channel = null;
			while (string.IsNullOrEmpty(channel)) {
				Console.WriteLine(nameof(Channel) + ":");
				channel = Console.ReadLine();
			}

			return new Configuration(botToken, channel);
		}

		internal static async Task<Configuration?> Load() {
			if (!File.Exists("config.json")) {
				return null;
			}

			Configuration? loadedConfig;
			FileStream file = File.OpenRead("config.json");
			await using (file.ConfigureAwait(false)) {
				try {
					loadedConfig = await JsonSerializer.DeserializeAsync<Configuration>(file).ConfigureAwait(false);
				} catch {
					return null;
				}
			}

			return loadedConfig;
		}

		internal async Task Save() {
			FileStream file = File.OpenWrite("config.json");
			await using (file.ConfigureAwait(false)) {
				await JsonSerializer.SerializeAsync(file, this).ConfigureAwait(false);
			}
		}
	}
}
