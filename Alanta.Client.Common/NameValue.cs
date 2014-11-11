
namespace Alanta.Client.Common
{
	public class NameValue
	{
		public string Name { get; set; }
		public byte Value { get; set; }

		public NameValue(string name, byte value)
		{
			Name = name;
			Value = value;
		}
	} 
}
