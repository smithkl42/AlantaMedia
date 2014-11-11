using System;

namespace Alanta.Client.Media.Dsp
{
	public class TimeDomainEchoCancelFilter : EchoCancelFilter
	{
		#region Constructors

		public TimeDomainEchoCancelFilter(int systemLatency, int filterLength, AudioFormat recordedAudioFormat,  AudioFormat playedAudioFormat, IAudioFilter playedResampler = null, IAudioFilter recordedResampler = null) :
			base(systemLatency, filterLength, recordedAudioFormat, playedAudioFormat, playedResampler, recordedResampler)
		{
			TimeDomainInit(recordedAudioFormat.SamplesPerSecond);
		}

		private void TimeDomainInit(int samplesPerSecond)
		{
			hangover = 0;

			NLMS_LEN = FilterLength;
			x = new float[NLMS_LEN + NLMS_EXT];  // tap delayed loudspeaker signal
			xf = new float[NLMS_LEN + NLMS_EXT]; // pre-whitening tap delayed signal
			h = new float[NLMS_LEN];             // tap weights

			j = NLMS_EXT;
			delta = 0.0f;
			Ambient = NoiseFloor;
			dfast = dslow = M75dB_PCM;
			xfast = xslow = M80dB_PCM;
			gain = 1.0f;
			Fx.Init(2000.0f / samplesPerSecond);
			Fe.Init(2000.0f / samplesPerSecond);

			aes_y2 = M0dB;
		}

		#endregion

		#region Fields and Properties

		// Time domain Filters
		IIR_HP acMic = new IIR_HP();
		IIR_HP acSpk = new IIR_HP();        // DC-level remove Highpass)

		/// <summary>
		/// 150Hz cut-off Highpass filter
		/// </summary>
		FIR_HP_300Hz cutoff = new FIR_HP_300Hz();
		float gain;                    // Mic signal amplify

		/// <summary>
		/// Pre-whitening highpass filter for x
		/// </summary>
		PreWhiteningFilter Fx = new PreWhiteningFilter();

		/// <summary>
		/// Pre-whitening highpass filter for e
		/// </summary>
		PreWhiteningFilter Fe = new PreWhiteningFilter();

		// Adrian soft decision DTD (Double Talk Detector)
		float dfast, xfast;
		float dslow, xslow;

		// NLMS-pw

		/// <summary>
		/// Tap delayed loudspeaker signal
		/// </summary>
		float[] x;

		/// <summary>
		/// Pre-whitened tap delayed signal
		/// </summary>
		float[] xf;

		/// <summary>
		/// Estimated echo path filter coefficients, aka "tap weights"
		/// </summary>
		float[] h;

		/// <summary>
		/// Index into local arrays.
		/// </summary>
		int j;                        // optimize: less memory copies

		/// <summary>
		/// Iterative dot-product of xf vector.
		/// </summary>
		/// <remarks>double to avoid loss of precision</remarks>
		double dotp_xf_xf;

		/// <summary>
		/// noise floor to stabilize NLMS
		/// </summary>
		float delta;

		// AES
		float aes_y2;                 // not in use!

		//// w vector visualization
		///// <summary>
		///// tap weight sums
		///// </summary>
		//float[] ws;

		int hangover;
		float stepsize;

		/* dB Values */
		const float M0dB = 1.0f;
		const float M3dB = 0.71f;
		const float M6dB = 0.50f;
		const float M9dB = 0.35f;
		const float M12dB = 0.25f;
		const float M18dB = 0.125f;
		const float M24dB = 0.063f;

		/* dB values for 16bit PCM */
		/* MxdB_PCM = 32767 * 10 ^(x / 20) */
		const float M10dB_PCM = 10362.0f;
		const float M20dB_PCM = 3277.0f;
		const float M25dB_PCM = 1843.0f;
		const float M30dB_PCM = 1026.0f;
		const float M35dB_PCM = 583.0f;
		const float M40dB_PCM = 328.0f;
		const float M45dB_PCM = 184.0f;
		const float M50dB_PCM = 104.0f;
		const float M55dB_PCM = 58.0f;
		const float M60dB_PCM = 33.0f;
		const float M65dB_PCM = 18.0f;
		const float M70dB_PCM = 10.0f;
		const float M75dB_PCM = 6.0f;
		const float M80dB_PCM = 3.0f;
		const float M85dB_PCM = 2.0f;
		const float M90dB_PCM = 1.0f;

		const float MAXPCM = 32767.0f;

		/* Design constants (Change to fine tune the algorithms */

		/* The following values are for hardware AEC and studio quality microphone */

		/// <summary>
		/// NLMS filter length in taps (samples). 
		/// </summary>
		/// <remarks>
		/// A longer filter length gives better Echo Cancellation, but maybe slower convergence speed and
		/// needs more CPU power (Order of NLMS is linear) */
		/// </remarks>
		private int NLMS_LEN; // (100 * WIDEB * 8);

		/// <summary>
		/// Vector w visualization length in taps (samples).
		/// </summary>
		const int DUMP_LEN = 40 * AudioConstants.BitsPerSample; // (40 * WIDEB * 8);

		/// <summary>
		/// minimum energy in xf. Range: M70dB_PCM to M50dB_PCM. Should be equal to microphone ambient Noise level
		/// </summary>
		const float NoiseFloor = M55dB_PCM;

		/// <summary>
		/// Leaky hangover in taps. 
		/// </summary>
		const int Thold = 60 * AudioConstants.BitsPerSample; // 60 * WIDEB * 8;

		// Adrian soft decision DTD 
		// left point. X is ratio, Y is stepsize
		const float STEPX1 = 1.0f, STEPY1 = 1.0f;
		// right point. STEPX2=2.0 is good double talk, 3.0 is good single talk.
		const float STEPX2 = 2.5f, STEPY2 = 0;
		const float ALPHAFAST = 1.0f / 100.0f;
		const float ALPHASLOW = 1.0f / 20000.0f;

		/// <summary>
		/// Ageing multiplier for LMS memory vector w 
		/// </summary>
		const float Leaky = 0.9999f;

		/// <summary>
		/// Double Talk Detector Speaker/Microphone Threshold. Range <=1
		/// </summary>
		/// <remarks>
		/// Large value (M0dB) is good for Single-Talk Echo cancellation, small value (M12dB) is good for Double-Talk AEC 
		/// </remarks>
		const float GeigelThreshold = M6dB;

		/// <summary>
		/// For Non Linear Processor. Range >0 to 1. 
		/// </summary>
		/// <remarks>Large value (M0dB) is good, for Double-Talk, small value (M12dB) is good for Single-Talk</remarks>
		const float NLPAttenuation = M12dB;

		/// <summary>
		/// Extention in taps to reduce mem copies
		/// </summary>
		const int NLMS_EXT = (10 * 8);

		/// <summary>
		/// block size in taps to optimize DTD calculation 
		/// </summary>
		const int DTD_LEN = 16;

		public float Ambient
		{
			get
			{
				return dfast;
			}
			set
			{
				float Min_xf = value;
				dotp_xf_xf -= delta;  // subtract old delta
				delta = (NLMS_LEN - 1) * Min_xf * Min_xf;
				dotp_xf_xf += delta;  // add new delta
			}
		}

		public float Aes
		{
			get
			{
				return aes_y2;
			}
			set
			{
				aes_y2 = value;
			}
		}


		#endregion

		#region Protected Methods

		/// <summary>
		/// Performs echo cancellation.
		/// </summary>
		/// <param name="recorded">A short[] array of samples recorded from the microphone</param>
		/// <param name="played">A short[] array of samples submitted to the speakers</param>
		/// <param name="outFrame">A short[] buffer onto which the cancelled output should be placed</param>
		protected override void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame)
		{
			for (int i = 0; i < recorded.Length; i++)
			{
				outFrame[i] = DoAEC(recorded[i], played[i]);
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Computes a Vector Dot Product
		/// </summary>
		/// <param name="a">The first vector (array)</param>
		/// <param name="aoffset">The offset into the first vector</param>
		/// <param name="b">The second vector (array)</param>
		/// <param name="boffset">The offset into the second vector</param>
		/// <returns>A float[] array which represents the dot product of the two input arrays.</returns>
		private float CalculateDotProduct(float[] a, int aoffset, float[] b, int boffset)
		{
			float sum = 0.0f;
			for (int j = 0; j < NLMS_LEN; j++)
			{
				sum += a[aoffset + j] * b[boffset + j];
			}
			return sum;
		}

		private float CalculateDotProduct(float[] a, float[] b, int boffset)
		{
			float sum = 0.0f;
			for (int j = 0; j < NLMS_LEN; j++)
			{
				sum += a[j] * b[boffset + j];
			}
			return sum;
		}

		private float max = float.MaxValue / 10;

		private bool IsScrewy(float n)
		{
			return Math.Abs(n) > max || float.IsInfinity(n) || float.IsNaN(n);
		}

		private bool IsScrewy(double n)
		{
			return Math.Abs(n) > max || double.IsInfinity(n) || double.IsNaN(n);
		}

		private short DoAEC(short recordedSample, short microphoneSample)
		{
			float mic = (float)recordedSample;
			float spk = (float)microphoneSample;

			// Mic Highpass Filter - to remove DC
			mic = acMic.highpass(mic);

			// Mic Highpass Filter - cut-off below 300Hz
			mic = cutoff.highpass(mic);

			// Amplify, for e.g. Soundcards with -6dB max. volume
			mic *= gain;

			// Spk Highpass Filter - to remove DC
			spk = acSpk.highpass(spk);

			// Double Talk Detector
			stepsize = CheckForDoubleTalk(mic, spk);

			// Leaky (ageing of vector w)
			leaky();

			// Acoustic Echo Cancellation
			mic = DoNLMS(mic, spk, stepsize);

			if (mic > short.MaxValue)
			{
				return short.MaxValue;
			}
			else if (mic < short.MinValue)
			{
				return short.MinValue;
			}
			else
			{
				return (short)mic;
			}
		}

		/// <summary>
		/// Adrian soft decision DTD
		/// </summary>
		/// <param name="d">Recorded audio sample (?)</param>
		/// <param name="x">Played audio sample (?)</param>
		/// <returns>A float representing the amount of double-talk detected</returns>
		/// <remarks>
		/// (Dual Average Near-End to Far-End signal Ratio DTD)
		/// This algorithm uses exponential smoothing with differnt 
		/// ageing parameters to get fast and slow near-end and far-end 
		/// signal averages. The ratio of NFRs term 
		/// (dfast / xfast) / (dslow / xslow) is used to compute the stepsize 
		/// A ratio value of 2.5 is mapped to stepsize 0, a ratio of 0 is 
		/// mapped to 1.0 with a limited linear function.
		/// </remarks>
		private float CheckForDoubleTalk(float d, float x)
		{
			float stepsize;

			// fast near-end and far-end average
			dfast += ALPHAFAST * (Math.Abs(d) - dfast);
			xfast += ALPHAFAST * (Math.Abs(x) - xfast);

			// slow near-end and far-end average
			dslow += ALPHASLOW * (Math.Abs(d) - dslow);
			xslow += ALPHASLOW * (Math.Abs(x) - xslow);

			if (xfast < M70dB_PCM)
			{
				return 0.0f;   // no Spk signal
			}

			if (dfast < M70dB_PCM)
			{
				return 0.0f;   // no Mic signal
			}

			// ratio of NFRs
			float ratio = (dfast * xslow) / (dslow * xfast);

			// begrenzte lineare Kennlinie
			const float M = (STEPY2 - STEPY1) / (STEPX2 - STEPX1);
			if (ratio < STEPX1)
			{
				stepsize = STEPY1;
			}
			else if (ratio > STEPX2)
			{
				stepsize = STEPY2;
			}
			else
			{
				// Punktrichtungsform einer Geraden
				stepsize = M * (ratio - STEPX1) + STEPY1;
			}
			return stepsize;
		}

		/// <summary>
		/// This is my implementation of Leaky NLMS.
		/// </summary>
		/// <remarks>
		/// The xfast signal is used to charge the hangover timer to Thold.
		/// When hangover expires (no Spk signal for some time) the vector w
		/// is erased. 
		/// </remarks>
		private void leaky()
		{
			if (xfast >= M70dB_PCM)
			{
				// vector w is valid for hangover Thold time
				hangover = Thold;
			}
			else
			{
				if (hangover > 1)
				{
					--hangover;
				}
				else if (1 == hangover)
				{
					--hangover;
					// My Leaky NLMS is to erase vector w when hangover expires
					Array.Clear(h, 0, h.Length);
					// memset(w, 0, sizeof(w));
				}
			}
		}

		private float DoNLMS(float mic, float spk, float stepsize)
		{
			x[j] = spk;

			// Add some white noise so that the input signal will be less correlated with itself and hence more distinct.
			xf[j] = Fx.Highpass(spk);

			// calculate error value 
			// (mic signal - estimated mic signal from spk signal)
			float e = mic;
			if (hangover > 0)
			{
				float dp = CalculateDotProduct(h, x, j);

				// Handle saturation (added by ks 11/3/10)
				if (dp > MAXPCM) dp = MAXPCM;
				else if (dp < -MAXPCM) dp = -MAXPCM;

				e -= dp;
			}

			// Add the same white noise to the error (output) signal that when we calculate the adaptive filter coefficients
			// they will have been transformed in the same manner as the input signal.
			float ef = Fe.Highpass(e);

			// optimization: iterative dotp(xf, xf)
			int xfPos = j + NLMS_LEN - 1;
			dotp_xf_xf += (xf[j] * xf[j] - xf[xfPos] * xf[xfPos]);

			if (stepsize > 0.0)
			{
				// calculate variable step size
				float mikro_ef = (float)(stepsize * ef / dotp_xf_xf);

				// update tap weights (filter learning)
				for (int i = 0; i < NLMS_LEN; i++)
				{
					h[i] += mikro_ef * xf[i + j];
				}
			}

			if (--j < 0)
			{
				// optimization: decrease number of memory copies
				j = NLMS_EXT;

				Buffer.BlockCopy(x, 0, x, (j + 1) * sizeof(float), (NLMS_LEN - 1) * sizeof(float));
				Buffer.BlockCopy(xf, 0, xf, (j + 1) * sizeof(float), (NLMS_LEN - 1) * sizeof(float));
			}

			// Handle saturation
			if (e > MAXPCM)
			{
				return MAXPCM;
			}
			else if (e < -MAXPCM)
			{
				return -MAXPCM;
			}
			else
			{
				return e;
			}
		}

		#endregion

	}

}
