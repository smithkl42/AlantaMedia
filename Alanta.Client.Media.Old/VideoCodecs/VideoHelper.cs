using System;
using System.Diagnostics;

namespace Alanta.Client.Media.VideoCodecs
{
	public static class VideoHelper
	{
		/// <summary>
		/// This is borrowed from http://www.compuphase.com/cmetric.htm
		/// </summary>
		/// <returns>A double representing the cartesian color distance between the two pixels</returns>
		public static int GetColorDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
		{
			//double rmean = (r1 + r2) / 2.0d;
			//int r = r1 - r2;
			//int g = g1 - g2;
			//int b = b1 - b2;
			//double weightR = 2 + rmean / 256;
			//const double weightG = 4.0;
			//double weightB = 2 + (255 - rmean) / 256;
			//return Math.Sqrt(weightR * r * r + weightG * g * g + weightB * b * b);

			// ks - Testing shows that this runs about 40% faster
			int rmean = (r1 + r2) / 2;
			int r = r1 - r2;
			int g = g1 - g2;
			int b = b1 - b2;
			int weightR = 2 + rmean / 256;
			const int weightG = 4;
			int weightB = 2 + (255 - rmean) / 256;
			return weightR * r * r + weightG * g * g + weightB * b * b;
		}


		public static float GetColorDistance(byte[] img1, byte[] img2)
		{
			float totalDistance = 0;
			float maxDistance = float.MinValue;
			for (int i = 0; i < img1.Length; i++)
			{
				byte b1 = img1[i];
				byte b2 = img2[i++];
				byte g1 = img1[i];
				byte g2 = img2[i++];
				byte r1 = img1[i];
				byte r2 = img2[i++];
				var distance = GetColorDistance(r1, g1, b1, r2, g2, b2);
				//if (distance > 300)
				//{
				//    Debug.WriteLine("Pixel {0}; r1 {1}; r2:{2}; g1:{3}; g2:{4}; b1:{5}; b2:{6}; distance:{7:0.00}",
				//        i, r1, r2, g1, g2, b1, b2, distance);
				//}
				totalDistance += distance; // *distance;
				maxDistance = Math.Max(distance, maxDistance);
			}
			var averageDistance = (float)(totalDistance / (img1.Length / 4.0));
			return averageDistance;
		}

		/// <summary>
		/// ks 10/19/11 - At the moment, this flips *and* reverses the image. We probably want to just flip it, 
		/// which is slightly more complicated, probably slower, and I don't want to think about it right now.
		/// </summary>
		public static void FlipAndReverse(byte[] original, byte[] flipped)
		{
			// Reverse the image
			var buffer1 = new int[original.Length / sizeof(int)];
			var buffer2 = new int[original.Length / sizeof(int)];
			Buffer.BlockCopy(original, 0, buffer1, 0, original.Length);
			for (int i = 0; i < buffer1.Length; i++)
			{
				buffer2[buffer2.Length - i - 1] = buffer1[i];
			}
			Buffer.BlockCopy(buffer2, 0, flipped, 0, original.Length);
		}
	}
}
