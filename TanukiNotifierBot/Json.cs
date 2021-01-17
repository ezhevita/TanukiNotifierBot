using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace TanukiNotifierBot {
	internal sealed class ApiResponse {
		[JsonProperty(Required = Required.DisallowNull)]
		internal InternalResponseBody? ResponseBody { get; private set; }

		[JsonProperty(PropertyName = "Response", Required = Required.Always)]
		internal InternalResponse ResponseInfo { get; private set; } = null!;

		internal sealed class InternalResponse {
			[JsonProperty(PropertyName = "Result", Required = Required.Always)]
			internal Result ResultInfo { get; private set; } = null!;

			internal sealed class Result {
				[JsonProperty(Required = Required.Always)]
				internal ushort Code { get; private set; }

				[JsonProperty(PropertyName = "errorCode", Required = Required.Always)]
				internal ushort ErrorCode { get; private set; }

				[JsonProperty(PropertyName = "message", Required = Required.Always)]
				internal string Message { get; private set; } = null!;
			}
		}

		internal sealed class InternalResponseBody {
			[JsonProperty(PropertyName = "items", Required = Required.DisallowNull)]
			internal Dictionary<string, Item>? Items { get; private set; }

			[JsonProperty(PropertyName = "result", Required = Required.Always)]
			internal bool Result { get; private set; }

			internal record Item : TanukiNotifierBot.Item {
				[JsonProperty(PropertyName = "time", Required = Required.Always)]
				internal Time TimeInfo { get; private set; }

				internal sealed class Time {
					[JsonProperty(PropertyName = "left", Required = Required.Always)]
					internal ushort LeftSeconds { get; private set; }
				}
			}
		}
	}

	public record Item {
		public ushort ID { get; init; }

		public ushort Price { get; init; }
	}

	public record MenuResponse {
		[JsonPropertyName("err")]
		public string Error { get; init; }

		[JsonPropertyName("props")]
		public Properties PropertiesInfo { get; init; }

		public record Properties {
			public State InitialState { get; init; }

			public record State {
				public ProductsInfo Products { get; init; }

				public record ProductsInfo {
					public string Error { get; init; }

					[JsonPropertyName("data")]
					public Dictionary<ushort, Product> Products { get; init; }

					public record Product : Item {
						[JsonPropertyName("img")]
						public string ImageLink { get; init; }

						[JsonPropertyName("share")]
						public string Link { get; set; }

						public string Title { get; init; }
					}
				}
			}
		}
	}
}
