
namespace Alanta.Client.Common.Logging.Targets
{
	public class SilverlightDebugTarget : BaseLogTarget
	{
		public SilverlightDebugTarget()
			: base(LogTargetFormat.Short, LoggingLevel.Debug)
		{

		}

		public SilverlightDebugTarget(LogTargetFormat format)
			: base(format, LoggingLevel.Debug)
		{

		}

		protected override void OnLog(string message)
		{
			System.Diagnostics.Debug.WriteLine(message);
		}
	}
}
