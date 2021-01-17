using System;

namespace TelegramTanukiNotifierBot {
	public class TanukiException : Exception {
		public TanukiException(string message) : base(message) { }
	}
}
