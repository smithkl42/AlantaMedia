
namespace Alanta.Client.Media
{
	public interface IReferenceCount
	{
		int ReferenceCount { get; set; }
		string Source { get; set; }
	}

}
