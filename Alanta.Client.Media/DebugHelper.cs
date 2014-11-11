using System;
using System.Text;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public static class DebugHelper
	{
		public static string AnalyzeAudioFrame(string source, byte[] frame, int start, int length)
		{
			var shortFrame = new short[length / sizeof(short)];
			Buffer.BlockCopy(frame, start, shortFrame, 0, length);
			return AnalyzeAudioFrame(source, shortFrame, 0, shortFrame.Length);
		}

		public static string AnalyzeAudioFrame(string source, short[] frame, int start, int length)
		{
			var sb = new StringBuilder();
			int zeroes = 0;
			for (int i = start; i < start + length; i++)
			{
				if (frame[i] == 0)
				{
					zeroes++;
				}
				if (i % 32 == 0)
				{
					sb.AppendLine();
					sb.Append(i + ":\t ");
				}
				sb.Append(frame[i] + "\t");
			}

			double zeroPercent = zeroes / (double)length;

			// Display the frame
			string results = string.Format("Frame stats: Source={0}; Length={1}; ZeroPercent={2}; Data={3}", source, length, zeroPercent, sb);
			ClientLogger.Debug(results);
			return results;
		}

	}
}
