using System;
using System.ComponentModel;

namespace Alanta.Client.Media.Dsp
{
	public class VoiceActivityDetector : INotifyPropertyChanged
	{
		#region Fields and Properties
		public enum Aggressiveness
		{
			Quality = 0,
			Normal = 1,
			Aggressive = 2,
			VeryAggressive = 3
		}

		private readonly short[] nbSpeechFrame; // Downsampled speech frame buffer (not threadsafe!)

		public int[] downsampling_filter_states = new int[4];
		public int frame_counter;
		public short[] hp_filter_state = new short[4];
		public short[] index_vector = new short[16 * Definitions.NUM_CHANNELS + 1];
		public short[] individual = new short[3];
		public short init_flag;
		public short[] low_value_vector = new short[16 * Definitions.NUM_CHANNELS + 1];
		public short[] lower_state = new short[5];
		public short[] mean_value = new short[Definitions.NUM_CHANNELS];
		private short mode;
		public short[] noise_means = new short[Definitions.NUM_TABLE_VALUES];
		public short[] noise_stds = new short[Definitions.NUM_TABLE_VALUES];
		public short num_of_speech;
		public short over_hang; // Over Hang
		public short[] over_hang_max_1 = new short[3];
		public short[] over_hang_max_2 = new short[3];
		public short[] speech_means = new short[Definitions.NUM_TABLE_VALUES];
		public short[] speech_stds = new short[Definitions.NUM_TABLE_VALUES];
		public short[] total = new short[3];
		public short[] upper_state = new short[5];
		public short vad;
		#endregion

		public VoiceActivityDetector(AudioFormat audioFormat, Aggressiveness mode)
		{
			int i;
			this.mode = (short)mode;

			nbSpeechFrame = new short[audioFormat.SamplesPerFrame / 2];

			// Initialization of struct
			vad = 1;
			frame_counter = 0;
			over_hang = 0;
			num_of_speech = 0;

			// Initialization of downsampling filter state
			downsampling_filter_states[0] = 0;
			downsampling_filter_states[1] = 0;
			downsampling_filter_states[2] = 0;
			downsampling_filter_states[3] = 0;

			// Read initial PDF parameters
			for (i = 0; i < Definitions.NUM_TABLE_VALUES; i++)
			{
				noise_means[i] = Definitions.kNoiseDataMeans[i];
				speech_means[i] = Definitions.kSpeechDataMeans[i];
				noise_stds[i] = Definitions.kNoiseDataStds[i];
				speech_stds[i] = Definitions.kSpeechDataStds[i];
			}

			// Index and Minimum value vectors are initialized
			for (i = 0; i < 16 * Definitions.NUM_CHANNELS; i++)
			{
				low_value_vector[i] = 10000;
				index_vector[i] = 0;
			}

			for (i = 0; i < 5; i++)
			{
				upper_state[i] = 0;
				lower_state[i] = 0;
			}

			for (i = 0; i < 4; i++)
			{
				hp_filter_state[i] = 0;
			}

			// Init mean value memory, for FindMin function
			mean_value[0] = 1600;
			mean_value[1] = 1600;
			mean_value[2] = 1600;
			mean_value[3] = 1600;
			mean_value[4] = 1600;
			mean_value[5] = 1600;

			if (mode == 0)
			{
				// Quality mode
				over_hang_max_1[0] = Definitions.OHMAX1_10MS_Q; // Overhang short speech burst
				over_hang_max_1[1] = Definitions.OHMAX1_20MS_Q; // Overhang short speech burst
				over_hang_max_1[2] = Definitions.OHMAX1_30MS_Q; // Overhang short speech burst
				over_hang_max_2[0] = Definitions.OHMAX2_10MS_Q; // Overhang long speech burst
				over_hang_max_2[1] = Definitions.OHMAX2_20MS_Q; // Overhang long speech burst
				over_hang_max_2[2] = Definitions.OHMAX2_30MS_Q; // Overhang long speech burst

				individual[0] = Definitions.INDIVIDUAL_10MS_Q;
				individual[1] = Definitions.INDIVIDUAL_20MS_Q;
				individual[2] = Definitions.INDIVIDUAL_30MS_Q;

				total[0] = Definitions.TOTAL_10MS_Q;
				total[1] = Definitions.TOTAL_20MS_Q;
				total[2] = Definitions.TOTAL_30MS_Q;
			}
			else if (mode == Aggressiveness.Normal)
			{
				// Low bitrate mode
				over_hang_max_1[0] = Definitions.OHMAX1_10MS_LBR; // Overhang short speech burst
				over_hang_max_1[1] = Definitions.OHMAX1_20MS_LBR; // Overhang short speech burst
				over_hang_max_1[2] = Definitions.OHMAX1_30MS_LBR; // Overhang short speech burst
				over_hang_max_2[0] = Definitions.OHMAX2_10MS_LBR; // Overhang long speech burst
				over_hang_max_2[1] = Definitions.OHMAX2_20MS_LBR; // Overhang long speech burst
				over_hang_max_2[2] = Definitions.OHMAX2_30MS_LBR; // Overhang long speech burst

				individual[0] = Definitions.INDIVIDUAL_10MS_LBR;
				individual[1] = Definitions.INDIVIDUAL_20MS_LBR;
				individual[2] = Definitions.INDIVIDUAL_30MS_LBR;

				total[0] = Definitions.TOTAL_10MS_LBR;
				total[1] = Definitions.TOTAL_20MS_LBR;
				total[2] = Definitions.TOTAL_30MS_LBR;
			}
			else if (mode == Aggressiveness.Aggressive)
			{
				// Aggressive mode
				over_hang_max_1[0] = Definitions.OHMAX1_10MS_AGG; // Overhang short speech burst
				over_hang_max_1[1] = Definitions.OHMAX1_20MS_AGG; // Overhang short speech burst
				over_hang_max_1[2] = Definitions.OHMAX1_30MS_AGG; // Overhang short speech burst
				over_hang_max_2[0] = Definitions.OHMAX2_10MS_AGG; // Overhang long speech burst
				over_hang_max_2[1] = Definitions.OHMAX2_20MS_AGG; // Overhang long speech burst
				over_hang_max_2[2] = Definitions.OHMAX2_30MS_AGG; // Overhang long speech burst

				individual[0] = Definitions.INDIVIDUAL_10MS_AGG;
				individual[1] = Definitions.INDIVIDUAL_20MS_AGG;
				individual[2] = Definitions.INDIVIDUAL_30MS_AGG;

				total[0] = Definitions.TOTAL_10MS_AGG;
				total[1] = Definitions.TOTAL_20MS_AGG;
				total[2] = Definitions.TOTAL_30MS_AGG;
			}
			else
			{
				// Very aggressive mode
				over_hang_max_1[0] = Definitions.OHMAX1_10MS_VAG; // Overhang short speech burst
				over_hang_max_1[1] = Definitions.OHMAX1_20MS_VAG; // Overhang short speech burst
				over_hang_max_1[2] = Definitions.OHMAX1_30MS_VAG; // Overhang short speech burst
				over_hang_max_2[0] = Definitions.OHMAX2_10MS_VAG; // Overhang long speech burst
				over_hang_max_2[1] = Definitions.OHMAX2_20MS_VAG; // Overhang long speech burst
				over_hang_max_2[2] = Definitions.OHMAX2_30MS_VAG; // Overhang long speech burst

				individual[0] = Definitions.INDIVIDUAL_10MS_VAG;
				individual[1] = Definitions.INDIVIDUAL_20MS_VAG;
				individual[2] = Definitions.INDIVIDUAL_30MS_VAG;

				total[0] = Definitions.TOTAL_10MS_VAG;
				total[1] = Definitions.TOTAL_20MS_VAG;
				total[2] = Definitions.TOTAL_30MS_VAG;
			}

			init_flag = Definitions.kInitCheck;
		}

		/// <summary>
		/// Aggressiveness setting (0, 1, 2, or 3)
		/// </summary>
		public short Mode
		{
			get { return mode; }
			set
			{
				mode = value;

				switch (mode)
				{
					case 0:
						over_hang_max_1[0] = Definitions.OHMAX1_10MS_Q; // Overhang short speech burst
						over_hang_max_1[1] = Definitions.OHMAX1_20MS_Q; // Overhang short speech burst
						over_hang_max_1[2] = Definitions.OHMAX1_30MS_Q; // Overhang short speech burst
						over_hang_max_2[0] = Definitions.OHMAX2_10MS_Q; // Overhang long speech burst
						over_hang_max_2[1] = Definitions.OHMAX2_20MS_Q; // Overhang long speech burst
						over_hang_max_2[2] = Definitions.OHMAX2_30MS_Q; // Overhang long speech burst
						individual[0] = Definitions.INDIVIDUAL_10MS_Q;
						individual[1] = Definitions.INDIVIDUAL_20MS_Q;
						individual[2] = Definitions.INDIVIDUAL_30MS_Q;
						total[0] = Definitions.TOTAL_10MS_Q;
						total[1] = Definitions.TOTAL_20MS_Q;
						total[2] = Definitions.TOTAL_30MS_Q;
						break;
					case 1:
						over_hang_max_1[0] = Definitions.OHMAX1_10MS_LBR; // Overhang short speech burst
						over_hang_max_1[1] = Definitions.OHMAX1_20MS_LBR; // Overhang short speech burst
						over_hang_max_1[2] = Definitions.OHMAX1_30MS_LBR; // Overhang short speech burst
						over_hang_max_2[0] = Definitions.OHMAX2_10MS_LBR; // Overhang long speech burst
						over_hang_max_2[1] = Definitions.OHMAX2_20MS_LBR; // Overhang long speech burst
						over_hang_max_2[2] = Definitions.OHMAX2_30MS_LBR; // Overhang long speech burst
						individual[0] = Definitions.INDIVIDUAL_10MS_LBR;
						individual[1] = Definitions.INDIVIDUAL_20MS_LBR;
						individual[2] = Definitions.INDIVIDUAL_30MS_LBR;
						total[0] = Definitions.TOTAL_10MS_LBR;
						total[1] = Definitions.TOTAL_20MS_LBR;
						total[2] = Definitions.TOTAL_30MS_LBR;
						break;
					case 2:
						over_hang_max_1[0] = Definitions.OHMAX1_10MS_AGG; // Overhang short speech burst
						over_hang_max_1[1] = Definitions.OHMAX1_20MS_AGG; // Overhang short speech burst
						over_hang_max_1[2] = Definitions.OHMAX1_30MS_AGG; // Overhang short speech burst
						over_hang_max_2[0] = Definitions.OHMAX2_10MS_AGG; // Overhang long speech burst
						over_hang_max_2[1] = Definitions.OHMAX2_20MS_AGG; // Overhang long speech burst
						over_hang_max_2[2] = Definitions.OHMAX2_30MS_AGG; // Overhang long speech burst
						individual[0] = Definitions.INDIVIDUAL_10MS_AGG;
						individual[1] = Definitions.INDIVIDUAL_20MS_AGG;
						individual[2] = Definitions.INDIVIDUAL_30MS_AGG;
						total[0] = Definitions.TOTAL_10MS_AGG;
						total[1] = Definitions.TOTAL_20MS_AGG;
						total[2] = Definitions.TOTAL_30MS_AGG;
						break;
					case 3:
						over_hang_max_1[0] = Definitions.OHMAX1_10MS_VAG; // Overhang short speech burst
						over_hang_max_1[1] = Definitions.OHMAX1_20MS_VAG; // Overhang short speech burst
						over_hang_max_1[2] = Definitions.OHMAX1_30MS_VAG; // Overhang short speech burst
						over_hang_max_2[0] = Definitions.OHMAX2_10MS_VAG; // Overhang long speech burst
						over_hang_max_2[1] = Definitions.OHMAX2_20MS_VAG; // Overhang long speech burst
						over_hang_max_2[2] = Definitions.OHMAX2_30MS_VAG; // Overhang long speech burst
						individual[0] = Definitions.INDIVIDUAL_10MS_VAG;
						individual[1] = Definitions.INDIVIDUAL_20MS_VAG;
						individual[2] = Definitions.INDIVIDUAL_30MS_VAG;
						total[0] = Definitions.TOTAL_10MS_VAG;
						total[1] = Definitions.TOTAL_20MS_VAG;
						total[2] = Definitions.TOTAL_30MS_VAG;
						break;
				}

				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs("Mode"));
				}
				//return 0;
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		public short CalcVad8khz(short[] speechFrame, int frameLength)
		{
			var featureVector = new short[Definitions.NUM_CHANNELS];

			// Get power in the bands
			short totalPower = get_features(speechFrame, frameLength, featureVector);

			// Make a VAD
			vad = WebRtcVad_GmmProbability(featureVector, totalPower, frameLength);

			return vad;
		}

		private static short WebRtcSpl_GetSizeInBits(uint value)
		{
			short bits = 0;

			// Fast binary search to find the number of bits used
			if ((0xFFFF0000 & value) != 0)
			{
				bits = 16;
			}
			if ((0x0000FF00 & (value >> bits)) != 0)
			{
				bits += 8;
			}
			if ((0x000000F0 & (value >> bits)) != 0)
			{
				bits += 4;
			}
			if ((0x0000000C & (value >> bits)) != 0)
			{
				bits += 2;
			}
			if ((0x00000002 & (value >> bits)) != 0)
			{
				bits += 1;
			}
			if ((0x00000001 & (value >> bits)) != 0)
			{
				bits += 1;
			}

			return bits;
		}

		private static int WebRtcSpl_NormW32(uint value)
		{
			int zeros = 0;

			if (value <= 0)
			{
				value ^= 0xFFFFFFFF;
			}

			// Fast binary search to determine the number of left shifts required to 32-bit normalize
			// the value
			if ((0xFFFF8000 & value) == 0)
			{
				zeros = 16;
			}
			if ((0xFF800000 & (value << zeros)) == 0)
			{
				zeros += 8;
			}
			if ((0xF8000000 & (value << zeros)) == 0)
			{
				zeros += 4;
			}
			if ((0xE0000000 & (value << zeros)) == 0)
			{
				zeros += 2;
			}
			if ((0xC0000000 & (value << zeros)) == 0)
			{
				zeros += 1;
			}

			return zeros;
		}

		private static int WebRtcSpl_GetScalingSquare(short[] in_vector, int in_vector_length, uint times)
		{
			int nbits = WebRtcSpl_GetSizeInBits(times);
			int i;
			short smax = -1;
			int sptr = 0;
			int looptimes = in_vector_length;

			for (i = looptimes; i > 0; i--)
			{
				short sabs = in_vector[sptr] > 0 ? in_vector[sptr] : (short)-in_vector[sptr];
				sptr++;
				smax = (sabs > smax ? sabs : smax);
			}
			int t = WebRtcSpl_NormW32((uint)smax * (uint)smax);

			if (smax == 0)
			{
				return 0; // Since norm(0) returns 0
			}
			return (t > nbits) ? 0 : nbits - t;
		}

		private static int WebRtcSpl_Energy(short[] vector, int vector_length, out int scale_factor)
		{
			int en = 0;
			int i;
			int scaling = WebRtcSpl_GetScalingSquare(vector, vector_length, (uint)vector_length);
			int looptimes = vector_length;

			for (i = 0; i < looptimes; i++)
			{
				en += vector[i] * vector[i] >> scaling;
			}
			scale_factor = scaling;

			return en;
		}

		private static void WebRtcVad_Allpass(short[] in_vector_src, int in_vector_ptr,
											  short[] out_vector_src, int out_vector_ptr,
											  short filter_coefficients,
											  int vector_length,
											  short[] filter_state_src, int filter_state_ptr)
		{
			// The filter can only cause overflow (in the w16 output variable)
			// if more than 4 consecutive input numbers are of maximum value and
			// has the the same sign as the impulse responses first taps.
			// First 6 taps of the impulse response: 0.6399 0.5905 -0.3779
			// 0.2418 -0.1547 0.0990

			int n;

			int state32 = filter_state_src[filter_state_ptr] << 16;

			for (n = 0; n < vector_length; n++)
			{
				int tmp32 = state32 + filter_coefficients * in_vector_src[in_vector_ptr];
				var tmp16 = (short)(tmp32 >> 16);
				out_vector_src[out_vector_ptr++] = tmp16;

				int in32 = in_vector_src[in_vector_ptr] << 14;
				state32 = in32 - filter_coefficients * tmp16;
				state32 = state32 << 1;
				in_vector_ptr += 2;
			}

			filter_state_src[filter_state_ptr] = (short)(state32 >> 16);
		}

		private static int WebRtcSpl_NormW32(int value)
		{
			int zeros = 0;

			if (value <= 0)
			{
				value ^= -1;
			}

			// Fast binary search to determine the number of left shifts required to 32-bit normalize
			// the value
			if ((0xFFFF8000 & value) == 0)
			{
				zeros = 16;
			}
			if ((0xFF800000 & (value << zeros)) == 0)
			{
				zeros += 8;
			}
			if ((0xF8000000 & (value << zeros)) == 0)
			{
				zeros += 4;
			}
			if ((0xE0000000 & (value << zeros)) == 0)
			{
				zeros += 2;
			}
			if ((0xC0000000 & (value << zeros)) == 0)
			{
				zeros += 1;
			}

			return zeros;
		}

		private static int WebRtcSpl_NormU32(uint value)
		{
			int zeros = 0;

			if (value == 0)
			{
				return 0;
			}

			if ((0xFFFF0000 & value) == 0)
			{
				zeros = 16;
			}
			if ((0xFF000000 & (value << zeros)) == 0)
			{
				zeros += 8;
			}
			if ((0xF0000000 & (value << zeros)) == 0)
			{
				zeros += 4;
			}
			if ((0xC0000000 & (value << zeros)) == 0)
			{
				zeros += 2;
			}
			if ((0x80000000 & (value << zeros)) == 0)
			{
				zeros += 1;
			}

			return zeros;
		}

		private static int WEBRTC_SPL_SHIFT_W16(int x, int c)
		{
			return (((c) >= 0) ? ((x) << (c)) : ((x) >> (-(c))));
		}

		private static int WEBRTC_SPL_SHIFT_W32(int x, int c)
		{
			return (((c) >= 0) ? ((x) << (c)) : ((x) >> (-(c))));
		}

		private static int WEBRTC_SPL_MUL_16_16_RSFT(int a, int b, int c)
		{
			int m = ((short)(a)) * ((short)(b));

			return ((m) >> (c));
		}

		private static void WebRtcVad_LogOfEnergy(short[] vector,
										   out short enerlogval,
										   ref short power,
										   short offset,
										   int vector_length)
		{
			short enerSum;

			int shfts;

			int energy = WebRtcSpl_Energy(vector, vector_length, out shfts);

			if (energy > 0)
			{
				int shfts2 = 16 - WebRtcSpl_NormW32(energy);
				shfts += shfts2;
				// "shfts" is the total number of right shifts that has been done to enerSum.
				enerSum = (short)WEBRTC_SPL_SHIFT_W32(energy, -shfts2);

				// Find:
				// 160*log10(enerSum*2^shfts) = 160*log10(2)*log2(enerSum*2^shfts) =
				// 160*log10(2)*(log2(enerSum) + log2(2^shfts)) =
				// 160*log10(2)*(log2(enerSum) + shfts)

				var zeros = (short)WebRtcSpl_NormU32((uint)enerSum);
				var frac = (short)(((uint)(enerSum << zeros) & 0x7FFFFFFF) >> 21);
				var log2 = (short)(((31 - zeros) << 10) + frac);

				enerlogval = (short)(WEBRTC_SPL_MUL_16_16_RSFT(Definitions.kLogConst, log2, 19)
									  + WEBRTC_SPL_MUL_16_16_RSFT(shfts, Definitions.kLogConst, 9));

				if (enerlogval < 0)
				{
					enerlogval = 0;
				}
			}
			else
			{
				enerlogval = 0;
				shfts = -15;
				enerSum = 0;
			}

			enerlogval += offset;

			// Total power in frame
			if (power <= Definitions.MIN_ENERGY)
			{
				if (shfts > 0)
				{
					power += Definitions.MIN_ENERGY + 1;
				}
				else if (WEBRTC_SPL_SHIFT_W16(enerSum, shfts) > Definitions.MIN_ENERGY)
				{
					power += Definitions.MIN_ENERGY + 1;
				}
				else
				{
					power += (short)WEBRTC_SPL_SHIFT_W16(enerSum, shfts);
				}
			}
		}

		private static void WebRtcVad_SplitFilter(short[] in_vector_src, int in_vector_ptr,
												  short[] out_vector_hp_src, int out_vector_hp_ptr,
												  short[] out_vector_lp_src, int out_vector_lp_ptr,
												  short[] upper_state_src, int upper_state_ptr,
												  short[] lower_state_src, int lower_state_ptr,
												  int in_vector_length)
		{
			int k;

			// Downsampling by 2 and get two branches
			int halflen = in_vector_length >> 1;

			// All-pass filtering upper branch
			WebRtcVad_Allpass(in_vector_src, 0, out_vector_hp_src, out_vector_hp_ptr, Definitions.kAllPassCoefsQ150, halflen, upper_state_src, upper_state_ptr);

			// All-pass filtering lower branch
			WebRtcVad_Allpass(in_vector_src, 1, out_vector_lp_src, out_vector_lp_ptr, Definitions.kAllPassCoefsQ151, halflen, lower_state_src, lower_state_ptr);

			// Make LP and HP signals
			for (k = 0; k < halflen; k++)
			{
				short tmpOut = out_vector_hp_src[out_vector_hp_ptr];
				out_vector_hp_src[out_vector_hp_ptr++] -= out_vector_lp_src[out_vector_lp_ptr];
				out_vector_lp_src[out_vector_lp_ptr++] += tmpOut;
			}
		}

		private static void WebRtcVad_HpOutput(short[] in_vector, int in_vector_length, short[] out_vector, short[] filter_state)
		{
			short i;
			int pi = 0; //in_vector pointer
			int outPtr = 0; // out_vector pointer

			// The sum of the absolute values of the impulse response:
			// The zero/pole-filter has a max amplification of a single sample of: 1.4546
			// Impulse response: 0.4047 -0.6179 -0.0266  0.1993  0.1035  -0.0194
			// The all-zero section has a max amplification of a single sample of: 1.6189
			// Impulse response: 0.4047 -0.8094  0.4047  0       0        0
			// The all-pole section has a max amplification of a single sample of: 1.9931
			// Impulse response: 1.0000  0.4734 -0.1189 -0.2187 -0.0627   0.04532
			for (i = 0; i < in_vector_length; i++)
			{
				// all-zero section (filter coefficients in Q14)
				int tmpW32 = Definitions.kHpZeroCoefs0 * in_vector[pi];
				tmpW32 += Definitions.kHpZeroCoefs1 * filter_state[0];
				tmpW32 += Definitions.kHpZeroCoefs2 * filter_state[1]; // Q14
				filter_state[1] = filter_state[0];
				filter_state[0] = in_vector[pi++];

				// all-pole section
				tmpW32 -= Definitions.kHpPoleCoefs1 * filter_state[2]; // Q14
				tmpW32 -= Definitions.kHpPoleCoefs2 * filter_state[3];
				filter_state[3] = filter_state[2];
				filter_state[2] = (short)(tmpW32 >> 14);
				out_vector[outPtr++] = filter_state[2];
			}
		}

		private short get_features(short[] in_vector, int frame_size, short[] out_vector)
		{
			short[] vecHP1 = new short[120], vecLP1 = new short[120];
			short[] vecHP2 = new short[60], vecLP2 = new short[60];
			short power = 0;

			// Split at 2000 Hz and downsample
			int filtno = 0;
			// curlen = frame_size;
			WebRtcVad_SplitFilter(in_vector, 0, vecHP1, 0, vecLP1, 0, upper_state, filtno, lower_state, filtno, frame_size);

			// Split at 3000 Hz and downsample
			filtno = 1;
			int curlen = frame_size >> 1;
			WebRtcVad_SplitFilter(vecHP1, 0, vecHP2, 0, vecLP2, 0, upper_state, filtno, lower_state, filtno, curlen);

			// Energy in 3000 Hz - 4000 Hz
			curlen = curlen >> 1;
			WebRtcVad_LogOfEnergy(vecHP2, out out_vector[5], ref power, Definitions.kOffsetVector[5], curlen);

			// Energy in 2000 Hz - 3000 Hz
			WebRtcVad_LogOfEnergy(vecLP2, out out_vector[4], ref power, Definitions.kOffsetVector[4], curlen);

			// Split at 1000 Hz and downsample
			filtno = 2;
			curlen = (frame_size >> 1);
			WebRtcVad_SplitFilter(vecLP1, 0, vecHP2, 0, vecLP2, 0, upper_state, filtno, lower_state, filtno, curlen);

			// Energy in 1000 Hz - 2000 Hz
			curlen = (curlen >> 1);
			WebRtcVad_LogOfEnergy(vecHP2, out out_vector[3], ref power, Definitions.kOffsetVector[3], curlen);

			// Split at 500 Hz
			filtno = 3;
			WebRtcVad_SplitFilter(vecLP2, 0, vecHP1, 0, vecLP1, 0, upper_state, filtno, lower_state, filtno, curlen);

			// Energy in 500 Hz - 1000 Hz
			curlen = (curlen >> 1);
			WebRtcVad_LogOfEnergy(vecHP1, out out_vector[2], ref power, Definitions.kOffsetVector[2], curlen);

			// Split at 250 Hz
			filtno = 4;
			WebRtcVad_SplitFilter(vecLP1, 0, vecHP2, 0, vecLP2, 0, upper_state, filtno, lower_state, filtno, curlen);

			// Energy in 250 Hz - 500 Hz
			curlen = curlen >> 1;
			WebRtcVad_LogOfEnergy(vecHP2, out out_vector[1], ref power, Definitions.kOffsetVector[1], curlen);

			// Remove DC and LFs
			WebRtcVad_HpOutput(vecLP2, curlen, vecHP1, hp_filter_state);

			// Power in 80 Hz - 250 Hz
			WebRtcVad_LogOfEnergy(vecHP1, out out_vector[0], ref power, Definitions.kOffsetVector[0], curlen);

			return power;
		}

		private static short WEBRTC_SPL_LSHIFT_W16(int x, int n)
		{
			return (short)(x << n);
		}

		private static short WEBRTC_SPL_RSHIFT_W16(int x, int n)
		{
			return (short)(x >> n);
		}

		private static int WEBRTC_SPL_RSHIFT_W32(int x, int n)
		{
			return x >> n;
		}

		private static int WebRtcSpl_DivW32W16(int num, int den)
		{
			// Guard against division with 0
			if (den != 0)
			{
				return num / den;
			}
			return 0x7FFFFFFF;
		}

		private static int WEBRTC_SPL_MUL_16_16(int a, int b)
		{
			return (a * b);
		}

		private static int WEBRTC_SPL_LSHIFT_W32(int x, int n)
		{
			return x << n;
		}

		private static int WebRtcVad_GaussianProbability(short in_sample, short mean, short std, out short delta)
		{
			short expVal;

			// Calculate tmpDiv=1/std, in Q10
			int tmp32 = (std >> 1) + 131072;
			var tmpDiv = (short)WebRtcSpl_DivW32W16(tmp32, std);

			// Calculate tmpDiv2=1/std^2, in Q14
			var tmp16 = (short)(tmpDiv >> 2);
			var tmpDiv2 = (short)((tmp16 * tmp16) >> 2);

			tmp16 = WEBRTC_SPL_LSHIFT_W16(in_sample, 3); // Q7
			tmp16 = (short)(tmp16 - mean); // Q7 - Q7 = Q7

			// To be used later, when updating noise/speech model
			// delta = (x-m)/std^2, in Q11
			delta = (short)WEBRTC_SPL_MUL_16_16_RSFT(tmpDiv2, tmp16, 10); //(Q14*Q7)>>10 = Q11

			// Calculate tmp32=(x-m)^2/(2*std^2), in Q10
			tmp32 = WEBRTC_SPL_MUL_16_16_RSFT(delta, tmp16, 9); // One shift for /2

			// Calculate expVal ~= exp(-(x-m)^2/(2*std^2)) ~= exp2(-log2(exp(1))*tmp32)
			if (tmp32 < Definitions.kCompVar)
			{
				// Calculate tmp16 = log2(exp(1))*tmp32 , in Q10
				tmp16 = (short)WEBRTC_SPL_MUL_16_16_RSFT((short)tmp32, Definitions.kLog10Const, 12);
				tmp16 = (short)-tmp16;
				var tmp16_2 = (short)(0x0400 | (tmp16 & 0x03FF));
				var tmp16_1 = (short)(tmp16 ^ 0xFFFF);
				tmp16 = WEBRTC_SPL_RSHIFT_W16(tmp16_1, 10);
				tmp16 += 1;
				// Calculate expVal=log2(-tmp32), in Q10
				expVal = (short)WEBRTC_SPL_RSHIFT_W32(tmp16_2, tmp16);
			}
			else
			{
				expVal = 0;
			}

			// Calculate y32=(1/std)*exp(-(x-m)^2/(2*std^2)), in Q20
			int y32 = WEBRTC_SPL_MUL_16_16(tmpDiv, expVal);

			return y32; // Q20
		}

		// Downsampling filter based on the splitting filter and the allpass functions
		// in vad_filterbank.c
		private static void WebRtcVad_Downsampling(short[] signal_in,
												   short[] signal_out,
												   int[] filter_state,
												   int inlen)
		{
			short tmp16_1, tmp16_2;
			int tmp32_1, tmp32_2;
			int n, halflen;

			// Downsampling by 2 and get two branches
			halflen = WEBRTC_SPL_RSHIFT_W16((short)inlen, 1);

			tmp32_1 = filter_state[0];
			tmp32_2 = filter_state[1];

			int signal_out_ptr = 0;
			int signal_in_ptr = 0;
			// Filter coefficients in Q13, filter state in Q0
			for (n = 0; n < halflen; n++)
			{
				// All-pass filtering upper branch
				tmp16_1 = (short)(WEBRTC_SPL_RSHIFT_W32(tmp32_1, 1) + WEBRTC_SPL_MUL_16_16_RSFT((Definitions.kAllPassCoefsQ130), signal_in[signal_in_ptr], 14));
				signal_out[signal_out_ptr] = tmp16_1;
				tmp32_1 = signal_in[signal_in_ptr++] - WEBRTC_SPL_MUL_16_16_RSFT((Definitions.kAllPassCoefsQ130), tmp16_1, 12);

				// All-pass filtering lower branch
				tmp16_2 = (short)(WEBRTC_SPL_RSHIFT_W32(tmp32_2, 1) + WEBRTC_SPL_MUL_16_16_RSFT((Definitions.kAllPassCoefsQ131), signal_in[signal_in_ptr], 14));
				signal_out[signal_out_ptr++] += tmp16_2;
				tmp32_2 = signal_in[signal_in_ptr++] - WEBRTC_SPL_MUL_16_16_RSFT((Definitions.kAllPassCoefsQ131), tmp16_2, 12);
			}
			filter_state[0] = tmp32_1;
			filter_state[1] = tmp32_2;
		}

		public short WebRtcVad_CalcVad16khz(short[] speechFrame, int frameLength)
		{
			// Wideband: Downsample signal before doing VAD
			WebRtcVad_Downsampling(speechFrame, nbSpeechFrame, downsampling_filter_states, frameLength);

			short len = WEBRTC_SPL_RSHIFT_W16((short)frameLength, 1);
			short vadResult = CalcVad8khz(nbSpeechFrame, len);

			return vadResult;
		}

		/// <summary>
		/// 
		/// Find the five lowest values of x in 100 frames long window. Return a mean
		///  value of these five values.
		/// *
		/// * Input:
		///  *      - feature_value : Feature value
		///  *      - channel       : Channel number
		///  *
		///  * Input & Output:
		///  *      - inst          : State information
		///  *
		/// * Output:
		/// *      return value    : Weighted minimum value for a moving window.
		/// </summary>
		private short WebRtcVad_FindMinimum(short x, int n)
		{
			int i, j, k, II = -1, offset;
			short meanV, alpha;
			int tmp32, tmp32_1;
			int valptr, // low_value_vector pointer
				idxptr, // index_vector pointer
				p1, // low_value_vector pointer
				p2, // index_vector pointer
				p3; // index_vector pointer

			// Offset to beginning of the 16 minimum values in memory
			offset = WEBRTC_SPL_LSHIFT_W16(n, 4);

			// Pointer to memory for the 16 minimum values and the age of each value
			idxptr = offset;
			valptr = offset;

			// Each value in low_value_vector is getting 1 loop older.
			// Update age of each value in indexVal, and remove old values.
			for (i = 0; i < 16; i++)
			{
				p3 = idxptr + i;
				if (index_vector[p3] != 100)
				{
					index_vector[p3] += 1;
				}
				else
				{
					p1 = valptr + i + 1;
					p2 = p3 + 1;
					for (j = i; j < 16; j++)
					{
						low_value_vector[valptr + j] = low_value_vector[p1++];
						index_vector[idxptr + j] = index_vector[p2++];
					}
					index_vector[idxptr + 15] = 101;
					low_value_vector[valptr + 15] = 10000;
				}
			}

			// Check if x smaller than any of the values in low_value_vector.
			// If so, find position.
			if (x < low_value_vector[valptr + 7])
			{
				if (x < low_value_vector[valptr + 3])
				{
					if (x < low_value_vector[valptr + 1])
					{
						II = x < low_value_vector[valptr] ? 0 : 1;
					}
					else if (x < low_value_vector[valptr + 2])
					{
						II = 2;
					}
					else
					{
						II = 3;
					}
				}
				else if (x < low_value_vector[valptr + 5])
				{
					if (x < low_value_vector[valptr + 4])
					{
						II = 4;
					}
					else
					{
						II = 5;
					}
				}
				else if (x < low_value_vector[valptr + 6])
				{
					II = 6;
				}
				else
				{
					II = 7;
				}
			}
			else if (x < low_value_vector[valptr + 15])
			{
				if (x < low_value_vector[valptr + 11])
				{
					if (x < low_value_vector[valptr + 9])
					{
						II = x < low_value_vector[valptr + 8] ? 8 : 9;
					}
					else if (x < low_value_vector[valptr + 10])
					{
						II = 10;
					}
					else
					{
						II = 11;
					}
				}
				else if (x < low_value_vector[valptr + 13])
				{
					II = x < low_value_vector[valptr + 12] ? 12 : 13;
				}
				else if (x < low_value_vector[valptr + 14])
				{
					II = 14;
				}
				else
				{
					II = 15;
				}
			}

			// Put new min value on right position and shift bigger values up
			if (II > -1)
			{
				for (i = 15; i > II; i--)
				{
					k = i - 1;
					low_value_vector[valptr + i] = low_value_vector[valptr + k];
					index_vector[idxptr + i] = index_vector[idxptr + k];
				}
				low_value_vector[valptr + II] = x;
				index_vector[idxptr + II] = 1;
			}

			meanV = 0;
			j = (frame_counter) > 4 ? 5 : frame_counter;

			if (j > 2)
			{
				meanV = low_value_vector[valptr + 2];
			}
			else if (j > 0)
			{
				meanV = low_value_vector[valptr];
			}
			else
			{
				meanV = 1600;
			}

			if (frame_counter > 0)
			{
				if (meanV < mean_value[n])
				{
					alpha = Definitions.ALPHA1; // 0.2 in Q15
				}
				else
				{
					alpha = Definitions.ALPHA2; // 0.99 in Q15
				}
			}
			else
			{
				alpha = 0;
			}

			tmp32 = WEBRTC_SPL_MUL_16_16((alpha + 1), mean_value[n]);
			tmp32_1 = WEBRTC_SPL_MUL_16_16(Int16.MaxValue - alpha, meanV);
			tmp32 += tmp32_1;
			tmp32 += 16384;
			mean_value[n] = (short)WEBRTC_SPL_RSHIFT_W32(tmp32, 15);

			return mean_value[n];
		}

		private short WebRtcVad_GmmProbability(short[] feature_vector, short total_power, int frame_length)
		{
			int n, k;
			short backval;
			short h0, h1;
			short ratvec, xval;
			short vadflag;
			short shifts0, shifts1;
			short tmp16, tmp16_1, tmp16_2;
			short diff, nr, pos;
			short nmk, nmk2, nmk3, smk, smk2, nsk, ssk;
			short delt, ndelt;
			short maxspe, maxmu;
			short[] deltaN = new short[Definitions.NUM_TABLE_VALUES], deltaS = new short[Definitions.NUM_TABLE_VALUES];
			short[] ngprvec = new short[Definitions.NUM_TABLE_VALUES], sgprvec = new short[Definitions.NUM_TABLE_VALUES];
			int h0test, h1test;
			int tmp32_1, tmp32_2;
			int dotVal;
			int nmid, smid;
			int[] probn = new int[Definitions.NUM_MODELS], probs = new int[Definitions.NUM_MODELS];
			int nmean1ptr, nmean2ptr, // noise_means pointer
				smean1ptr, smean2ptr, // speech_means pointer
				nstd1ptr, nstd2ptr, // noise_stds pointer
				sstd1ptr, sstd2ptr; // speech_stds pointer
			short overhead1, overhead2, individualTest, totalTest;

			// Set the thresholds to different values based on frame length
			if (frame_length == 80)
			{
				// 80 input samples
				overhead1 = over_hang_max_1[0];
				overhead2 = over_hang_max_2[0];
				individualTest = individual[0];
				totalTest = total[0];
			}
			else if (frame_length == 160)
			{
				// 160 input samples
				overhead1 = over_hang_max_1[1];
				overhead2 = over_hang_max_2[1];
				individualTest = individual[1];
				totalTest = total[1];
			}
			else
			{
				// 240 input samples
				overhead1 = over_hang_max_1[2];
				overhead2 = over_hang_max_2[2];
				individualTest = individual[2];
				totalTest = total[2];
			}

			if (total_power > Definitions.MIN_ENERGY)
			{
				// If signal present at all

				// Set pointers to the gaussian parameters
				nmean1ptr = 0;
				nmean2ptr = Definitions.NUM_CHANNELS;
				smean1ptr = 0;
				smean2ptr = Definitions.NUM_CHANNELS;
				nstd1ptr = 0;
				nstd2ptr = Definitions.NUM_CHANNELS;
				sstd1ptr = 0;
				sstd2ptr = Definitions.NUM_CHANNELS;

				vadflag = 0;
				dotVal = 0;
				for (n = 0; n < Definitions.NUM_CHANNELS; n++)
				{
					// For all channels

					pos = (short)(n << 1);
					xval = feature_vector[n];

					// Probability for Noise, Q7 * Q20 = Q27
					tmp32_1 = WebRtcVad_GaussianProbability(xval, noise_means[nmean1ptr++], noise_stds[nstd1ptr++],
															out deltaN[pos]);
					probn[0] = (Definitions.kNoiseDataWeights[n] * tmp32_1);
					tmp32_1 = WebRtcVad_GaussianProbability(xval, noise_means[nmean2ptr++], noise_stds[nstd2ptr++],
															out deltaN[pos + 1]);
					probn[1] = (Definitions.kNoiseDataWeights[n + Definitions.NUM_CHANNELS] * tmp32_1);
					h0test = probn[0] + probn[1]; // Q27
					h0 = (short)WEBRTC_SPL_RSHIFT_W32(h0test, 12); // Q15

					// Probability for Speech
					tmp32_1 = WebRtcVad_GaussianProbability(xval, speech_means[smean1ptr++], speech_stds[sstd1ptr++],
															out deltaS[pos]);
					probs[0] = (Definitions.kSpeechDataWeights[n] * tmp32_1);
					tmp32_1 = WebRtcVad_GaussianProbability(xval, speech_means[smean2ptr++], speech_stds[sstd2ptr++],
															out deltaS[pos + 1]);
					probs[1] = (Definitions.kSpeechDataWeights[n + Definitions.NUM_CHANNELS] * tmp32_1);
					h1test = probs[0] + probs[1]; // Q27
					h1 = (short)WEBRTC_SPL_RSHIFT_W32(h1test, 12); // Q15

					// Get likelihood ratio. Approximate log2(H1/H0) with shifts0 - shifts1
					shifts0 = (short)WebRtcSpl_NormW32(h0test);
					shifts1 = (short)WebRtcSpl_NormW32(h1test);

					if ((h0test > 0) && (h1test > 0))
					{
						ratvec = (short)(shifts0 - shifts1);
					}
					else if (h1test > 0)
					{
						ratvec = (short)(31 - shifts1);
					}
					else if (h0test > 0)
					{
						ratvec = (short)(shifts0 - 31);
					}
					else
					{
						ratvec = 0;
					}

					// VAD decision with spectrum weighting
					dotVal += WEBRTC_SPL_MUL_16_16(ratvec, Definitions.kSpectrumWeight[n]);

					// Individual channel test
					if ((ratvec << 2) > individualTest)
					{
						vadflag = 1;
					}

					// Probabilities used when updating model
					if (h0 > 0)
					{
						tmp32_1 = (int)(probn[0] & 0xFFFFF000); // Q27
						tmp32_2 = WEBRTC_SPL_LSHIFT_W32(tmp32_1, 2); // Q29
						ngprvec[pos] = (short)WebRtcSpl_DivW32W16(tmp32_2, h0);
						ngprvec[pos + 1] = (short)(16384 - ngprvec[pos]);
					}
					else
					{
						ngprvec[pos] = 16384;
						ngprvec[pos + 1] = 0;
					}

					// Probabilities used when updating model
					if (h1 > 0)
					{
						tmp32_1 = (int)(probs[0] & 0xFFFFF000);
						tmp32_2 = WEBRTC_SPL_LSHIFT_W32(tmp32_1, 2);
						sgprvec[pos] = (short)WebRtcSpl_DivW32W16(tmp32_2, h1);
						sgprvec[pos + 1] = (short)(16384 - sgprvec[pos]);
					}
					else
					{
						sgprvec[pos] = 0;
						sgprvec[pos + 1] = 0;
					}
				}

				// Overall test
				if (dotVal >= totalTest)
				{
					vadflag |= 1;
				}

				// Set pointers to the means and standard deviations.
				nmean1ptr = 0;
				smean1ptr = 0;
				nstd1ptr = 0;
				sstd1ptr = 0;

				maxspe = 12800;

				// Update the model's parameters
				for (n = 0; n < Definitions.NUM_CHANNELS; n++)
				{
					pos = WEBRTC_SPL_LSHIFT_W16((short)n, 1);

					// Get min value in past which is used for long term correction
					backval = WebRtcVad_FindMinimum(feature_vector[n], n); // Q4

					// Compute the "global" mean, that is the sum of the two means weighted
					nmid = WEBRTC_SPL_MUL_16_16(Definitions.kNoiseDataWeights[n], noise_means[nmean1ptr]); // Q7 * Q7
					nmid += WEBRTC_SPL_MUL_16_16(Definitions.kNoiseDataWeights[n + Definitions.NUM_CHANNELS],
												 noise_means[nmean1ptr + Definitions.NUM_CHANNELS]);
					tmp16_1 = (short)WEBRTC_SPL_RSHIFT_W32(nmid, 6); // Q8

					for (k = 0; k < Definitions.NUM_MODELS; k++)
					{
						nr = (short)(pos + k);

						nmean2ptr = nmean1ptr + k * Definitions.NUM_CHANNELS;
						smean2ptr = smean1ptr + k * Definitions.NUM_CHANNELS;
						nstd2ptr = nstd1ptr + k * Definitions.NUM_CHANNELS;
						sstd2ptr = sstd1ptr + k * Definitions.NUM_CHANNELS;
						nmk = noise_means[nmean2ptr];
						smk = speech_means[smean2ptr];
						nsk = noise_stds[nstd2ptr];
						ssk = speech_stds[sstd2ptr];

						// Update noise mean vector if the frame consists of noise only
						nmk2 = nmk;
						if (vadflag == 0)
						{
							// deltaN = (x-mu)/sigma^2
							// ngprvec[k] = probn[k]/(probn[0] + probn[1])

							delt = (short)WEBRTC_SPL_MUL_16_16_RSFT(ngprvec[nr],
																	 deltaN[nr], 11); // Q14*Q11
							nmk2 = (short)(nmk + (short)WEBRTC_SPL_MUL_16_16_RSFT(delt,
																					Definitions.kNoiseUpdateConst,
																					22)); // Q7+(Q14*Q15>>22)
						}

						// Long term correction of the noise mean
						ndelt = WEBRTC_SPL_LSHIFT_W16(backval, 4);
						ndelt -= tmp16_1; // Q8 - Q8
						nmk3 = (short)(nmk2 + (short)WEBRTC_SPL_MUL_16_16_RSFT(ndelt,
																				 Definitions.kBackEta,
																				 9)); // Q7+(Q8*Q8)>>9

						// Control that the noise mean does not drift to much
						tmp16 = WEBRTC_SPL_LSHIFT_W16(k + 5, 7);
						if (nmk3 < tmp16)
						{
							nmk3 = tmp16;
						}
						tmp16 = WEBRTC_SPL_LSHIFT_W16(72 + k - n, 7);
						if (nmk3 > tmp16)
						{
							nmk3 = tmp16;
						}
						noise_means[nmean2ptr] = nmk3;

						if (vadflag != 0)
						{
							// Update speech mean vector:
							// deltaS = (x-mu)/sigma^2
							// sgprvec[k] = probn[k]/(probn[0] + probn[1])

							delt = (short)WEBRTC_SPL_MUL_16_16_RSFT(sgprvec[nr], deltaS[nr], 11); // (Q14*Q11)>>11=Q14
							tmp16 = (short)(WEBRTC_SPL_MUL_16_16_RSFT(delt, Definitions.kSpeechUpdateConst, 21) + 1);
							smk2 = (short)(smk + (tmp16 >> 1)); // Q7 + (Q14 * Q15 >> 22)

							// Control that the speech mean does not drift too much
							maxmu = (short)(maxspe + 640);
							if (smk2 < Definitions.kMinimumMean[k])
							{
								smk2 = Definitions.kMinimumMean[k];
							}
							if (smk2 > maxmu)
							{
								smk2 = maxmu;
							}

							speech_means[smean2ptr] = smk2;

							// (Q7>>3) = Q4
							tmp16 = WEBRTC_SPL_RSHIFT_W16((smk + 4), 3);

							tmp16 = (short)(feature_vector[n] - tmp16); // Q4
							tmp32_1 = WEBRTC_SPL_MUL_16_16_RSFT(deltaS[nr], tmp16, 3);
							tmp32_2 = tmp32_1 - 4096; // Q12
							tmp16 = WEBRTC_SPL_RSHIFT_W16((sgprvec[nr]), 2);
							tmp32_1 = (tmp16 * tmp32_2); // (Q15>>3)*(Q14>>2)=Q12*Q12=Q24

							tmp32_2 = WEBRTC_SPL_RSHIFT_W32(tmp32_1, 4); // Q20

							// 0.1 * Q20 / Q7 = Q13
							if (tmp32_2 > 0)
							{
								tmp16 = (short)WebRtcSpl_DivW32W16(tmp32_2, ssk * 10);
							}
							else
							{
								tmp16 = (short)WebRtcSpl_DivW32W16(-tmp32_2, ssk * 10);
								tmp16 = (short)(-tmp16);
							}
							// divide by 4 giving an update factor of 0.025
							tmp16 += 128; // Rounding
							ssk += WEBRTC_SPL_RSHIFT_W16(tmp16, 8);
							// Division with 8 plus Q7
							if (ssk < Definitions.MIN_STD)
							{
								ssk = Definitions.MIN_STD;
							}
							speech_stds[sstd2ptr] = ssk;
						}
						else
						{
							// Update GMM variance vectors
							// deltaN * (feature_vector[n] - nmk) - 1, Q11 * Q4
							tmp16 = (short)(feature_vector[n] - WEBRTC_SPL_RSHIFT_W16(nmk, 3));

							// (Q15>>3) * (Q14>>2) = Q12 * Q12 = Q24
							tmp32_1 = WEBRTC_SPL_MUL_16_16_RSFT(deltaN[nr], tmp16, 3) - 4096;
							tmp16 = WEBRTC_SPL_RSHIFT_W16((ngprvec[nr] + 2), 2);
							tmp32_2 = tmp16 * tmp32_1;
							tmp32_1 = WEBRTC_SPL_RSHIFT_W32(tmp32_2, 14);
							// Q20  * approx 0.001 (2^-10=0.0009766)

							// Q20 / Q7 = Q13
							// tmp16 = (short)WebRtcSpl_DivW32W16(tmp32_1, nsk);
							if (tmp32_1 > 0)
							{
								tmp16 = (short)WebRtcSpl_DivW32W16(tmp32_1, nsk);
							}
							else
							{
								tmp16 = (short)WebRtcSpl_DivW32W16(-tmp32_1, nsk);
								tmp16 = (short)(-tmp16);
							}
							tmp16 += 32; // Rounding
							nsk += WEBRTC_SPL_RSHIFT_W16(tmp16, 6);

							if (nsk < Definitions.MIN_STD)
							{
								nsk = Definitions.MIN_STD;
							}

							noise_stds[nstd2ptr] = nsk;
						}
					}

					// Separate models if they are too close - nmid in Q14
					nmid = WEBRTC_SPL_MUL_16_16(Definitions.kNoiseDataWeights[n], noise_means[nmean1ptr]);
					nmid += WEBRTC_SPL_MUL_16_16(Definitions.kNoiseDataWeights[n + Definitions.NUM_CHANNELS], noise_means[nmean2ptr]);

					// smid in Q14
					smid = WEBRTC_SPL_MUL_16_16(Definitions.kSpeechDataWeights[n], speech_means[smean1ptr]);
					smid += WEBRTC_SPL_MUL_16_16(Definitions.kSpeechDataWeights[n + Definitions.NUM_CHANNELS], speech_means[smean2ptr]);

					// diff = "global" speech mean - "global" noise mean
					diff = (short)WEBRTC_SPL_RSHIFT_W32(smid, 9);
					tmp16 = (short)WEBRTC_SPL_RSHIFT_W32(nmid, 9);
					diff -= tmp16;

					if (diff < Definitions.kMinimumDifference[n])
					{
						tmp16 = (short)(Definitions.kMinimumDifference[n] - diff); // Q5

						// tmp16_1 = ~0.8 * (kMinimumDifference - diff) in Q7
						// tmp16_2 = ~0.2 * (kMinimumDifference - diff) in Q7
						tmp16_1 = (short)WEBRTC_SPL_MUL_16_16_RSFT(13, tmp16, 2);
						tmp16_2 = (short)WEBRTC_SPL_MUL_16_16_RSFT(3, tmp16, 2);

						// First Gauss, speech model
						tmp16 = (short)(tmp16_1 + speech_means[smean1ptr]);
						speech_means[smean1ptr] = tmp16;
						smid = WEBRTC_SPL_MUL_16_16(tmp16, Definitions.kSpeechDataWeights[n]);

						// Second Gauss, speech model
						tmp16 = (short)(tmp16_1 + speech_means[smean2ptr]);
						speech_means[smean2ptr] = tmp16;
						smid += WEBRTC_SPL_MUL_16_16(tmp16, Definitions.kSpeechDataWeights[n + Definitions.NUM_CHANNELS]);

						// First Gauss, noise model
						tmp16 = (short)(noise_means[nmean1ptr] - tmp16_2);
						noise_means[nmean1ptr] = tmp16;

						nmid = WEBRTC_SPL_MUL_16_16(tmp16, Definitions.kNoiseDataWeights[n]);

						// Second Gauss, noise model
						tmp16 = (short)(noise_means[nmean2ptr] - tmp16_2);
						noise_means[nmean2ptr] = tmp16;
						nmid += WEBRTC_SPL_MUL_16_16(tmp16, Definitions.kNoiseDataWeights[n + Definitions.NUM_CHANNELS]);
					}

					// Control that the speech & noise means do not drift too much
					maxspe = Definitions.kMaximumSpeech[n];
					tmp16_2 = (short)WEBRTC_SPL_RSHIFT_W32(smid, 7);
					if (tmp16_2 > maxspe)
					{
						// Upper limit of speech model
						tmp16_2 -= maxspe;

						speech_means[smean1ptr] -= tmp16_2;
						speech_means[smean2ptr] -= tmp16_2;
					}

					tmp16_2 = (short)WEBRTC_SPL_RSHIFT_W32(nmid, 7);
					if (tmp16_2 > Definitions.kMaximumNoise[n])
					{
						tmp16_2 -= Definitions.kMaximumNoise[n];

						noise_means[nmean1ptr] -= tmp16_2;
						noise_means[nmean2ptr] -= tmp16_2;
					}

					//noise_means[nmean1ptr]++;
					//speech_means[smean1ptr]++;
					//noise_stds[nstd1ptr]++;
					//speech_stds[sstd1ptr]++;
					nmean1ptr++;
					smean1ptr++;
					nstd1ptr++;
					sstd1ptr++;
				}
				frame_counter++;
			}
			else
			{
				vadflag = 0;
			}

			// Hangover smoothing
			if (vadflag == 0)
			{
				if (over_hang > 0)
				{
					vadflag = (short)(2 + over_hang);
					over_hang = (short)(over_hang - 1);
				}
				num_of_speech = 0;
			}
			else
			{
				num_of_speech = (short)(num_of_speech + 1);
				if (num_of_speech > Definitions.NSP_MAX)
				{
					num_of_speech = Definitions.NSP_MAX;
					over_hang = overhead2;
				}
				else
				{
					over_hang = overhead1;
				}
			}
			return vadflag;
		}

		#region Nested type: Definitions

		public static class Definitions
		{
			public const int NUM_CHANNELS = 6; // Eight frequency bands
			public const int NUM_MODELS = 2; // Number of Gaussian models
			public const int NUM_TABLE_VALUES = NUM_CHANNELS * NUM_MODELS;

			public const int MIN_ENERGY = 10;
			public const int ALPHA1 = 6553; // 0.2 in Q15
			public const int ALPHA2 = 32439; // 0.99 in Q15
			public const int NSP_MAX = 6; // Maximum number of VAD=1 frames in a row counted
			public const int MIN_STD = 384; // Minimum standard deviation
			// Mode 0, Quality thresholds - Different thresholds for the different frame lengths
			public const int INDIVIDUAL_10MS_Q = 24;
			public const int INDIVIDUAL_20MS_Q = 21; // (log10(2)*66)<<2 ~=16
			public const int INDIVIDUAL_30MS_Q = 24;

			public const int TOTAL_10MS_Q = 57;
			public const int TOTAL_20MS_Q = 48;
			public const int TOTAL_30MS_Q = 57;

			public const int OHMAX1_10MS_Q = 8; // Max Overhang 1
			public const int OHMAX2_10MS_Q = 14; // Max Overhang 2
			public const int OHMAX1_20MS_Q = 4; // Max Overhang 1
			public const int OHMAX2_20MS_Q = 7; // Max Overhang 2
			public const int OHMAX1_30MS_Q = 3;
			public const int OHMAX2_30MS_Q = 5;

			// Mode 1, Low bitrate thresholds - Different thresholds for the different frame lengths
			public const int INDIVIDUAL_10MS_LBR = 37;
			public const int INDIVIDUAL_20MS_LBR = 32;
			public const int INDIVIDUAL_30MS_LBR = 37;

			public const int TOTAL_10MS_LBR = 100;
			public const int TOTAL_20MS_LBR = 80;
			public const int TOTAL_30MS_LBR = 100;

			public const int OHMAX1_10MS_LBR = 8; // Max Overhang 1
			public const int OHMAX2_10MS_LBR = 14; // Max Overhang 2
			public const int OHMAX1_20MS_LBR = 4;
			public const int OHMAX2_20MS_LBR = 7;

			public const int OHMAX1_30MS_LBR = 3;
			public const int OHMAX2_30MS_LBR = 5;

			// Mode 2, Very aggressive thresholds - Different thresholds for the different frame lengths
			public const int INDIVIDUAL_10MS_AGG = 82;
			public const int INDIVIDUAL_20MS_AGG = 78;
			public const int INDIVIDUAL_30MS_AGG = 82;

			public const int TOTAL_10MS_AGG = 285; //580
			public const int TOTAL_20MS_AGG = 260;
			public const int TOTAL_30MS_AGG = 285;

			public const int OHMAX1_10MS_AGG = 6; // Max Overhang 1
			public const int OHMAX2_10MS_AGG = 9; // Max Overhang 2
			public const int OHMAX1_20MS_AGG = 3;
			public const int OHMAX2_20MS_AGG = 5;

			public const int OHMAX1_30MS_AGG = 2;
			public const int OHMAX2_30MS_AGG = 3;

			// Mode 3, Super aggressive thresholds - Different thresholds for the different frame lengths
			public const int INDIVIDUAL_10MS_VAG = 94;
			public const int INDIVIDUAL_20MS_VAG = 94;
			public const int INDIVIDUAL_30MS_VAG = 94;

			public const int TOTAL_10MS_VAG = 1100; //1700
			public const int TOTAL_20MS_VAG = 1050;
			public const int TOTAL_30MS_VAG = 1100;

			public const int OHMAX1_10MS_VAG = 6; // Max Overhang 1
			public const int OHMAX2_10MS_VAG = 9; // Max Overhang 2
			public const int OHMAX1_20MS_VAG = 3;
			public const int OHMAX2_20MS_VAG = 5;

			public const int OHMAX1_30MS_VAG = 2;
			public const int OHMAX2_30MS_VAG = 3;


			public const int kInitCheck = 42;

			// Spectrum Weighting

			public const short kCompVar = 22005;

			// Constant 160*log10(2) in Q9
			public const short kLogConst = 24660;

			// Constant log2(exp(1)) in Q12
			public const short kLog10Const = 5909;

			// Q15
			public const short kNoiseUpdateConst = 655;
			public const short kSpeechUpdateConst = 6554;

			// Q8
			public const short kBackEta = 154;
			public static short[] kSpectrumWeight = new short[] { 6, 8, 10, 12, 14, 16 };

			// Coefficients used by WebRtcVad_HpOutput, Q14
			// ks 10/20/11 - Slightly more efficient to access variables directly rather than through an array.
			// public static short[] kHpZeroCoefs = new short[] {6631, -13262, 6631};
			public const short kHpZeroCoefs0 = 6631;
			public const short kHpZeroCoefs1 = -13262;
			public const short kHpZeroCoefs2 = 6631;

			// ks 10/20/11 - Slightly more efficient to access variables directly rather than through an array.
			// public static short[] kHpPoleCoefs = new short[] {16384, -7756, 5620};
			public const short kHpPoleCoefs0 = 16384;
			public const short kHpPoleCoefs1 = -7756;
			public const short kHpPoleCoefs2 = 5620;

			// Allpass filter coefficients, upper and lower, in Q15
			// Upper: 0.64, Lower: 0.17
			// ks 10/20/11 - Slightly more efficient to access variables directly rather than through an array.
			// public static short[] kAllPassCoefsQ15 = new short[] {20972, 5571};
			public const short kAllPassCoefsQ150 = 20972;
			public const short kAllPassCoefsQ151 = 5571;

			// ks 10/20/11 - Slightly more efficient to access variables directly rather than through an array.
			// public static short[] kAllPassCoefsQ13 = new short[] {5243, 1392}; // Q13
			public const short kAllPassCoefsQ130 = 5243;
			public const short kAllPassCoefsQ131 = 1392;

			// Minimum difference between the two models, Q5
			public static short[] kMinimumDifference = new short[] { 544, 544, 576, 576, 576, 576 };

			// Upper limit of mean value for speech model, Q7
			public static short[] kMaximumSpeech = new short[] { 11392, 11392, 11520, 11520, 11520, 11520 };

			// Minimum value for mean value
			public static short[] kMinimumMean = new short[] { 640, 768 };

			// Upper limit of mean value for noise model, Q7
			public static short[] kMaximumNoise = new short[] { 9216, 9088, 8960, 8832, 8704, 8576 };

			// Adjustment for division with two in WebRtcVad_SplitFilter
			public static short[] kOffsetVector = new short[] { 368, 368, 272, 176, 176, 176 };

			// Start values for the Gaussian models, Q7
			// Weights for the two Gaussians for the six channels (noise)
			public static short[] kNoiseDataWeights = new short[] { 34, 62, 72, 66, 53, 25, 94, 66, 56, 62, 75, 103 };

			// Weights for the two Gaussians for the six channels (speech)
			public static short[] kSpeechDataWeights = new short[] { 48, 82, 45, 87, 50, 47, 80, 46, 83, 41, 78, 81 };

			// Means for the two Gaussians for the six channels (noise)
			public static short[] kNoiseDataMeans = new short[]
			{
				6738, 4892, 7065, 6715, 6771, 3369, 7646, 3863,
				7820, 7266, 5020, 4362
			};

			// Means for the two Gaussians for the six channels (speech)
			public static short[] kSpeechDataMeans = new short[]
			{
				8306, 10085, 10078, 11823, 11843, 6309, 9473,
				9571, 10879, 7581, 8180, 7483
			};

			// Stds for the two Gaussians for the six channels (noise)
			public static short[] kNoiseDataStds = new short[]
			{
				378, 1064, 493, 582, 688, 593, 474, 697, 475, 688,
				421, 455
			};

			// Stds for the two Gaussians for the six channels (speech)
			public static short[] kSpeechDataStds = new short[]
			{
				555, 505, 567, 524, 585, 1231, 509, 828, 492, 1540,
				1079, 850
			};
		}

		#endregion
	}
}