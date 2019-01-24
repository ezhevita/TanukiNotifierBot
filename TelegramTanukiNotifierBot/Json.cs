using System.Collections.Generic;
using Newtonsoft.Json;
#pragma warning disable 649

namespace TelegramTanukiNotifierBot {
	internal sealed class ApiResponse {
		[JsonProperty(Required = Required.DisallowNull)]
		internal InternalResponseBody ResponseBody;

		[JsonProperty(PropertyName = "Response", Required = Required.Always)]
		internal InternalResponse ResponseInfo;

		internal sealed class InternalResponse {
			[JsonProperty(PropertyName = "method", Required = Required.Always)]
			internal string Method;

			[JsonProperty(PropertyName = "Result", Required = Required.Always)]
			internal Result ResultInfo;

			internal sealed class Result {
				[JsonProperty(PropertyName = "code", Required = Required.Always)]
				internal ushort Code;

				[JsonProperty(PropertyName = "errorCode", Required = Required.Always)]
				internal ushort ErrorCode;

				[JsonProperty(PropertyName = "message", Required = Required.Always)]
				internal string Message;
			}
		}

		internal sealed class InternalResponseBody {
			[JsonProperty(PropertyName = "items", Required = Required.DisallowNull)]
			internal Dictionary<string, Item> Items;

			[JsonProperty(PropertyName = "result", Required = Required.Always)]
			internal bool Result;

			internal sealed class Item : TelegramTanukiNotifierBot.Item {
				[JsonProperty(PropertyName = "time", Required = Required.Always)]
				internal Time TimeInfo;

				internal sealed class Time {
					[JsonProperty(PropertyName = "end", Required = Required.Always)]
					internal ulong EndTimestamp;

					[JsonProperty(PropertyName = "left", Required = Required.Always)]
					internal ushort LeftSeconds;

					[JsonProperty(PropertyName = "start", Required = Required.Always)]
					internal ulong StartTimestamp;
				}
			}
		}
	}

	internal class Item {
		[JsonIgnore]
		internal ushort ID;

		[JsonIgnore]
		internal ushort Price;

		[JsonProperty(PropertyName = "id", Required = Required.Always)]
		private string IDText {
			get => ID.ToString();

			set {
				if (string.IsNullOrEmpty(value)) {
					Program.Log($"{nameof(IDText)}: {nameof(value)} is null!");
					return;
				}

				if (!ushort.TryParse(value, out ID) || (ID == 0)) {
					Program.Log($"{nameof(IDText)}: {nameof(value)} is invalid!");
				}
			}
		}

		[JsonProperty(PropertyName = "price", Required = Required.Always)]
		private string PriceText {
			get => Price.ToString();

			set {
				if (string.IsNullOrEmpty(value)) {
					Program.Log($"{nameof(PriceText)}: {nameof(value)} is null!");
					return;
				}

				if (!ushort.TryParse(value, out Price) || (Price == 0)) {
					Program.Log($"{nameof(PriceText)}: {nameof(value)} is invalid!");
				}
			}
		}
	}

	internal sealed class MenuResponse {
		[JsonProperty(PropertyName = "err", Required = Required.AllowNull)]
		internal string Error;

		[JsonProperty(PropertyName = "props", Required = Required.Always)]
		internal Properties PropertiesInfo;

		internal sealed class Properties {
			[JsonProperty(PropertyName = "initialState", Required = Required.DisallowNull)]
			internal State InitialState;

			internal sealed class State {
				[JsonProperty(PropertyName = "products", Required = Required.Always)]
				internal ProductsInfo Products;

				internal sealed class ProductsInfo {
					[JsonProperty(PropertyName = "error", Required = Required.AllowNull)]
					internal string Error;

					[JsonProperty(PropertyName = "data", Required = Required.Always)]
					internal Dictionary<ushort, Product> Products;

					internal sealed class Product : Item {
						[JsonProperty(PropertyName = "img", Required = Required.Always)]
						internal string ImageLink;

						[JsonProperty(PropertyName = "share", Required = Required.Always)]
						internal string Link;

						[JsonProperty(PropertyName = "title", Required = Required.Always)]
						internal string Title;
					}
				}
			}
		}
	}
}
