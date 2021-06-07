using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using ProductData = System.Collections.Generic.Dictionary<ushort, TanukiNotifierBot.MenuResponse.Properties.State.ProductsInfo.Product>;

namespace TanukiNotifierBot {
	internal static class Program {
		private const string TanukiHost = "https://www.tanuki.ru";
		private static ProductData? CachedProductData;

		private static readonly HashSet<ushort> ProductsToSkip = new() {
			13943
		};

		private static readonly IFlurlClient Client = new FlurlClient(
			new HttpClient {
				BaseAddress = new Uri(TanukiHost),
				DefaultRequestHeaders = {
					UserAgent = {
						new ProductInfoHeaderValue(nameof(TanukiNotifierBot), Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
					}
				},
				Timeout = TimeSpan.FromSeconds(10)
			}
		);

		private static readonly object ApiPostData = new {
			header = new {
				version = "2.0",
				userId = "Bot",
				debugMode = false,
				agent = new {
					device = "desktop",
					version = "Bot"
				},
				cityId = "1"
			},
			method = new {
				name = "getSpecialGoods"
			},
			data = new { }
		};

		private static readonly HtmlParser Parser = new();

		private static async Task<ProductData> GetProducts() {
			Stream? menuStream = await Client.Request("/menu").GetStreamAsync().ConfigureAwait(false);

			IHtmlDocument? document = await Parser.ParseDocumentAsync(menuStream).ConfigureAwait(false);

			INode? node = document.Body.SelectSingleNode("/html/body/script[1]");
			if (node == null) {
				throw new TanukiException(nameof(node) + " is null");
			}

			string scriptContent = node.TextContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];

			const string prefix = "__NEXT_DATA__ = ";
			scriptContent = scriptContent.Trim()[prefix.Length..];

			MenuResponse? menuResponse = JsonConvert.DeserializeObject<MenuResponse?>(scriptContent);
			if (menuResponse == null) {
				throw new TanukiException(nameof(menuResponse) + " is null");
			}

			if (!string.IsNullOrEmpty(menuResponse.Error)) {
				throw new TanukiException($"Menu error: {menuResponse.Error}");
			}

			if (menuResponse.PropertiesInfo == null) {
				throw new TanukiException(nameof(menuResponse.PropertiesInfo) + " is null");
			}

			MenuResponse.Properties.State.ProductsInfo productsInfo = menuResponse.PropertiesInfo.InitialState.Products;
			if (!string.IsNullOrEmpty(productsInfo.Error)) {
				throw new TanukiException($"Products error: {productsInfo.Error}");
			}

			if (productsInfo.Products == null) {
				throw new TanukiException(nameof(productsInfo.Products) + " is null");
			}

			List<ushort> invalidProducts = new();
			
			// Tanuki gives us invalid link in the JSON, so we have to correct it using data from HTML
			foreach ((ushort id, MenuResponse.Properties.State.ProductsInfo.Product product) in productsInfo.Products) {
				if (ProductsToSkip.Contains(id)) {
					continue;
				}

				IElement? productNode = (IElement?) document.Body.SelectSingleNode($"//div[@data-id='{id}']/div/div[@class='product__box']/a");
				if (productNode == null) {
					// Product is not shown, remove it from data - it may appear later and we will have a broken link
					invalidProducts.Add(id);
					continue;
				}

				string productUrl = productNode.GetAttribute("href");
				if (string.IsNullOrEmpty(productUrl)) {
					throw new TanukiException($"{id} - {nameof(productUrl)} is null");
				}

				product.Link = TanukiHost + productUrl;
			}

			foreach (ushort invalidProduct in invalidProducts) {
				productsInfo.Products.Remove(invalidProduct);
			}

			return productsInfo.Products;
		}

		private static void Log(string log) {
			string formattedLog = $"{DateTime.UtcNow:O}|{log}";
			Console.WriteLine(formattedLog);
			File.AppendAllText("log.txt", formattedLog + Environment.NewLine);
		}

		private static async Task Main() {
			Log($"Starting {nameof(TanukiNotifierBot)}");

			Configuration? config = await Configuration.Load().ConfigureAwait(false);
			if (config == null) {
				config = Configuration.Create();
				await config.Save().ConfigureAwait(false);
			}

			TelegramBotClient botClient = new(config.BotToken);

			try {
				bool testResult = await botClient.TestApiAsync().ConfigureAwait(false);
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
				try {
					if (!firstTime) {
						await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
					} else {
						firstTime = false;
					}

					ApiResponse? apiResponse = await Client.Request("/api/")
						.PostJsonAsync(ApiPostData)
						.ReceiveJson<ApiResponse>()
						.ConfigureAwait(false);

					if (apiResponse == null) {
						throw new TanukiException($"{nameof(apiResponse)} is null");
					}

					if (apiResponse.ResponseInfo.ResultInfo.ErrorCode != 0) {
						ApiResponse.InternalResponse.Result errorResult = apiResponse.ResponseInfo.ResultInfo;

						throw new TanukiException($"Failed with code {errorResult.Code}, error code {errorResult.ErrorCode}, message: {errorResult.Message}");
					}

					if (apiResponse.ResponseBody == null) {
						throw new TanukiException(nameof(apiResponse.ResponseBody) + " is null");
					}

					if (!apiResponse.ResponseBody.Result) {
						throw new TanukiException("Failed with false body result");
					}

					if (apiResponse.ResponseBody.Items == null) {
						throw new TanukiException(nameof(apiResponse.ResponseBody.Items) + " is null");
					}

					if (!apiResponse.ResponseBody.Items.TryGetValue("current", out ApiResponse.InternalResponseBody.Item? item)) {
						throw new TanukiException("Items collection doesn't contain current offer");
					}

					if ((item.ID == 0) || (item.Price == 0)) {
						throw new TanukiException($"{nameof(item)} is invalid");
					}

					ushort id = item.ID;
					if ((CachedProductData == null) || !CachedProductData.TryGetValue(id, out var product)) {
						ProductData productData = await GetProducts().ConfigureAwait(false);

						CachedProductData = productData ?? throw new TanukiException($"{nameof(productData)} is null!");

						if (!CachedProductData.TryGetValue(id, out product)) {
							throw new TanukiException($"{id} doesn't exist in {nameof(CachedProductData)}");
						}
					}

					string response = $"{product.Title}\nСтарая цена: {product.Price}₽\nЦена по акции: {item.Price}₽\n[Ссылка]({product.Link})";
					await botClient.SendPhotoAsync(config.Channel, new InputOnlineFile(product.ImageLink), response, ParseMode.Markdown).ConfigureAwait(false);

					Log("Successfully sent info about current offer!");
					await Task.Delay(TimeSpan.FromSeconds(item.TimeInfo.LeftSeconds)).ConfigureAwait(false);
				} catch (TanukiException e) when (e.Message == "Failed with false body result") {
					await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
				} catch (TanukiException e) {
					Log("Tanuki error: " + e.Message);
					await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
				} catch (Exception e) {
					Log("Unknown exception: " + e);
					await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
				}
			}
		}
	}
}
