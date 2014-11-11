using System.Runtime.InteropServices;

namespace Alanta.Client.Media
{
	public class Approximate
	{
		/// <summary>
		/// Gives a rough (+/- 2%) but very fast approximation of the square root. ~56% faster than Math.Sqrt().
		/// See http://en.wikipedia.org/wiki/Methods_of_computing_square_roots#Approximations_that_depend_on_IEEE_representation
		/// </summary>
		public static float Sqrt(float z)
		{
			if (z == 0) return 0;
			FloatIntUnion u;
			u.tmp = 0;
			u.f = z;
			u.tmp -= 1 << 23;	// Subtract 2^m.
			u.tmp >>= 1;		// Divide by 2.
			u.tmp += 1 << 29;	// Add ((b + 1) / 2) * 2^m.
			return u.f;
		}

		/// <summary>
		/// Gives a surprisingly accurate (+/- .01%) and reasonably fast approximation of the square root. ~44% faster than Math.Sqrt().
		/// See http://www.codemaestro.com/reviews/9.
		/// </summary>
		public static float Sqrt2(float z)
		{
			if (z == 0) return 0;
			FloatIntUnion u;
			u.tmp = 0;
			float xhalf = 0.5f * z;
			u.f = z;
			u.tmp = 0x5f375a86 - (u.tmp >> 1);		// gives initial guess y0
			u.f = u.f * (1.5f - xhalf * u.f * u.f); // Newton step, repeating increases accuracy
			return u.f * z;							// Multiply the inverse square root by the original number to get the original square root.
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct FloatIntUnion
		{
			[FieldOffset(0)]
			public float f;

			[FieldOffset(0)]
			public int tmp;
		}
	}
}
