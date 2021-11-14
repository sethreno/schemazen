using System.Diagnostics;

namespace SchemaZen.Library {
	public interface ILogger {
		void Log(TraceLevel level, string message);
	}
}
