using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using ProductData = System.Collections.Generic.Dictionary<ushort, TelegramTanukiNotifierBot.MenuResponse.Properties.State.ProductsInfo.Product>;

namespace TelegramTanukiNotifierBot {
	internal static class Program {
		private const string TanukiHost = "https://www.tanuki.ru";
		private static TelegramBotClient BotClient;
		private static ProductData CachedProductData;

		private static Configuration Config;

		private static readonly HttpClient HttpClient = new HttpClient {
			DefaultRequestHeaders = {
				UserAgent = {
					new ProductInfoHeaderValue(nameof(TelegramTanukiNotifierBot), "2.0")
				}
			},
			Timeout = TimeSpan.FromSeconds(10)
		};

		private static async Task Main() {
			Log($"Starting {nameof(TelegramTanukiNotifierBot)}");

			Configuration loadedConfig = Configuration.Load();
			if (loadedConfig == null) {
				loadedConfig = Configuration.Create();
				loadedConfig.Save();
			}

			Config = loadedConfig;
			BotClient = new TelegramBotClient(Config.BotToken);

			try {
				bool testResult = await BotClient.TestApiAsync().ConfigureAwait(false);
				if (!testResult) {
					Log("Invalid API token");
					return;
				}
			} catch (ApiRequestException e) {
				Log($"Exception when testing Telegram API: {e}");
				return;
			}

			bool firstTime = true;
			while (true) {
				if (!firstTime) {
					await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				} else {
					firstTime = false;
				}

				HttpResponseMessage responseMessage = await HttpClient.PostAsync(TanukiHost + "/api/", new StringContent("{\"header\":{\"version\":\"2.0\",\"userId\":\"Bot\",\"debugMode\":false,\"agent\":{\"device\":\"desktop\",\"version\":\"Bot\"},\"cityId\":\"1\"},\"method\":{\"name\":\"getSpecialGoods\"},\"data\":{}}", Encoding.UTF8, "application/json")).ConfigureAwait(false);
				if (!responseMessage.IsSuccessStatusCode) {
					Log($"Got an error on Tanuki API request: {responseMessage.StatusCode}");
					continue;
				}

				string responseText = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

				ApiResponse apiResponse;
				try {
					apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseText);
				} catch (JsonException e) {
					Log($"Exception occured: {e.Message}");
					continue;
				}

				if (apiResponse == null) {
					Log($"{nameof(apiResponse)} is null!");
					continue;
				}

				if (apiResponse.ResponseInfo.ResultInfo.ErrorCode != 0) {
					ApiResponse.InternalResponse.Result errorResult = apiResponse.ResponseInfo.ResultInfo;
					Log($"Failed with code {errorResult.Code}, error code {errorResult.ErrorCode}, message: {errorResult.Message}");
					continue;
				}

				if (!apiResponse.ResponseBody.Result) {
					Log("Failed with false body result");
					continue;
				}

				if (!apiResponse.ResponseBody.Items.ContainsKey("current")) {
					Log("Items doesn't contain current offer");
					continue;
				}

				ApiResponse.InternalResponseBody.Item item = apiResponse.ResponseBody.Items["current"];
				if ((item.ID == 0) || (item.Price == 0)) {
					Log($"{nameof(item)} is invalid");
					continue;
				}

				ushort id = item.ID;
				if ((CachedProductData == null) || !CachedProductData.ContainsKey(id)) {
					ProductData productData = await GetProducts().ConfigureAwait(false);
					if (productData == null) {
						Log($"{nameof(productData)} is null!");
						continue;
					}

					CachedProductData = productData;
				}

				if (!CachedProductData.ContainsKey(id)) {
					Log($"{nameof(id)} doesn't exist in {nameof(CachedProductData)}");
					continue;
				}

				MenuResponse.Properties.State.ProductsInfo.Product product = CachedProductData[id];

				string response = $"{product.Title}\nСтарая цена: {product.Price}₽\nЦена по акции: {item.Price}₽\n[Ссылка]({product.Link})";
				await BotClient.SendPhotoAsync(Config.Channel, new InputOnlineFile(product.ImageLink), response, ParseMode.Markdown).ConfigureAwait(false);

				Log($"Successfully sent info about current offer!");
				await Task.Delay(TimeSpan.FromSeconds(item.TimeInfo.LeftSeconds)).ConfigureAwait(false);
			}
		}

		private static async Task<ProductData> GetProducts() {
			HttpResponseMessage menuResponseMessage = await HttpClient.GetAsync(TanukiHost + "/menu").ConfigureAwait(false);
			if (!menuResponseMessage.IsSuccessStatusCode) {
				Log($"Got error on menu request: {menuResponseMessage.StatusCode}");
				return null;
			}

			string menuResponseText = await menuResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

			HtmlDocument menuHtml = new HtmlDocument();
			menuHtml.LoadHtml(menuResponseText);

			HtmlNode node = menuHtml.DocumentNode.SelectSingleNode("/html/body/script[1]");
			if (node == null) {
				Log($"{nameof(node)} is null!");
				return null;
			}

			string scriptContent = node.InnerText.Split(new []{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)[0];

			const string beginning = "__NEXT_DATA__ = ";
			scriptContent = scriptContent.Trim().Substring(beginning.Length);

			MenuResponse menuResponse;
			try {
				menuResponse = JsonConvert.DeserializeObject<MenuResponse>(scriptContent);
			} catch (JsonException e) {
				Log($"Exception occured: {e.Message}");
				return null;
			}

			if (menuResponse.Error != null) {
				Log($"Got a menu error: {menuResponse.Error}");
				return null;
			}

			MenuResponse.Properties.State.ProductsInfo productsInfo = menuResponse.PropertiesInfo.InitialState.Products;
			if (productsInfo.Error != null) {
				Log($"Got a product error: {productsInfo.Error}");
				return null;
			}

			return productsInfo.Products;
		}

		internal static void Log(string log) {
			string formattedLog = $"{DateTime.UtcNow:G}|{log}";
			Console.WriteLine(formattedLog);
			File.AppendAllText("log.txt", formattedLog);
		}
	}
}
