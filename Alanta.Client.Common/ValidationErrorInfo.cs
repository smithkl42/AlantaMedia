namespace Alanta.Client.Common
{
	public class ValidationErrorInfo
	{
		public int ErrorCode { get; set; }
		public string ErrorMessage { get; set; }

		public override string ToString()
		{
			return ErrorMessage;
		}
	}
}
