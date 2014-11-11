using System.Text;

namespace Alanta.Client.Common.Logging.Targets
{
	public class MemoryTarget : BaseLogTarget
	{
		readonly StringBuilder _sb = new StringBuilder();

		public MemoryTarget()
			: base(LogTargetFormat.Extended, LoggingLevel.Debug)
		{

		}

		public MemoryTarget(LogTargetFormat format)
			: base(format, LoggingLevel.Debug)
		{

		}

		protected override void OnLog(string message)
		{
			_sb.AppendLine(message);
		}

		public string GetLogData()
		{
			return _sb.ToString();
		}
	}
}
