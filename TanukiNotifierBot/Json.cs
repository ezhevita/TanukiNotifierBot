using System.Collections.Generic;
using Newtonsoft.Json;

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
				internal Time TimeInfo { get; private set; } = null!;

				internal sealed class Time {
					[JsonProperty(PropertyName = "left", Required = Required.Always)]
					internal ushort LeftSeconds { get; private set; }
				}
			}
		}
	}

	public record Item {
		[JsonProperty(Required = Required.Always)]
		public ushort ID { get; private set; }

		[JsonProperty(Required = Required.Always)]
		public ushort Price { get; private set; }
	}

	public record MenuResponse {
		[JsonProperty(PropertyName = "err", Required = Required.AllowNull)]
		public string? Error { get; private set; }

		[JsonProperty(PropertyName = "props", Required = Required.AllowNull)]
		public Properties? PropertiesInfo { get; private set; }

		public record Properties {
			[JsonProperty(Required = Required.Always)]
			public State InitialState { get; private set; } = null!;

			public record State {
				[JsonProperty(Required = Required.Always)]
				public ProductsInfo Products { get; private set; } = null!;

				public record ProductsInfo {
					[JsonProperty(Required = Required.AllowNull)]
					public string? Error { get; private set; }

					[JsonProperty(PropertyName = "data", Required = Required.AllowNull)]
					public Dictionary<ushort, Product>? Products { get; private set; }

					public record Product : Item {
						[JsonProperty(PropertyName = "img", Required = Required.Always)]
						public string ImageLink { get; private set; } = null!;

						[JsonProperty(PropertyName = "share", Required = Required.Always)]
						public string Link { get; set; } = null!;
						
						[JsonProperty(Required = Required.Always)]
						public string Title { get; private set; } = null!;
					}
				}
			}
		}
	}
}
