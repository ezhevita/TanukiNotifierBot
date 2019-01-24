using System;
using System.IO;
using Newtonsoft.Json;

namespace TelegramTanukiNotifierBot {
	internal class Configuration {
		[JsonProperty(Required = Required.Always)]
		internal string BotToken;

		[JsonProperty(Required = Required.Always)]
		internal string Channel;

		private Configuration() {
		}

		internal static Configuration Create() {
			Configuration config = new Configuration();

			Console.WriteLine(nameof(BotToken) + ":");
			config.BotToken = Console.ReadLine();

			Console.WriteLine(nameof(Channel) + ":");
			config.Channel = Console.ReadLine();

			return config;
		}

		internal static Configuration Load() {
			if (!File.Exists("config.json")) {
				return null;
			}

			string content = File.ReadAllText("config.json");
			Configuration loadedConfig;
			try {
				loadedConfig = JsonConvert.DeserializeObject<Configuration>(content);
			} catch {
				return null;
			}

			return loadedConfig;
		}

		internal void Save() {
			string content = JsonConvert.SerializeObject(this);
			File.WriteAllText("config.json", content);
		}
	}
}
