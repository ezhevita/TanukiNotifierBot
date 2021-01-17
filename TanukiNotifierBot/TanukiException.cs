using System;

namespace TanukiNotifierBot {
	public class TanukiException : Exception {
		public TanukiException(string message) : base(message) { }
	}
}
