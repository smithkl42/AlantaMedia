// Oscillator.cs (c) Charles Petzold, 2009

using System;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media
{
	public enum Waveform
	{
		Sine,
		Square,
		Triangle,
		Sawtooth,
		ReverseSawtooth,
		//        Custom
	}

	public class Oscillator
	{
		double frequency;
		uint phaseAngleIncrement;
		uint phaseAngle = 0;

		public Waveform Waveform { get; set; }

		public double Frequency
		{
			set
			{
				frequency = value;
				phaseAngleIncrement = (uint)(frequency * uint.MaxValue / AudioFormat.Default.SamplesPerSecond);
			}
			get
			{
				return frequency;
			}
		}

		public short GetNextSample()
		{
			ushort wholePhaseAngle = (ushort)(phaseAngle >> 16);
			short amplitude = 0;

			switch (Waveform)
			{
				case Waveform.Sine:
					amplitude = (short)(short.MaxValue * Math.Sin(2 * Math.PI * wholePhaseAngle / ushort.MaxValue));
					break;

				case Waveform.Square:
					amplitude = wholePhaseAngle < (ushort)short.MaxValue ? short.MinValue : short.MaxValue;
					break;

				case Waveform.Triangle:
					amplitude = (short)(wholePhaseAngle < (ushort.MaxValue) ? 1 * short.MinValue + 2 * wholePhaseAngle :
																			  3 * short.MaxValue - 2 * wholePhaseAngle);
					break;

				case Waveform.Sawtooth:
					amplitude = (short)(short.MinValue + wholePhaseAngle);
					break;

				case Waveform.ReverseSawtooth:
					amplitude = (short)(short.MaxValue - wholePhaseAngle);
					break;
			}

			phaseAngle += phaseAngleIncrement;

			return amplitude;
		}
	}
}
