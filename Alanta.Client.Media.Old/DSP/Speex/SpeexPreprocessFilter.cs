using System;

/* Copyright (C) 2003 Epic Games (written by Jean-Marc Valin)
   Copyright (C) 2004-2006 Epic Games 
   
   File: preprocess.c
   Preprocessor with denoising based on the algorithm by Ephraim and Malah

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions are
   met:

   1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

   2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.

   3. The name of the author may not be used to endorse or promote products
   derived from this software without specific prior written permission.

   THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
   IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
   OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
   DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
   INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
   SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
   HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
   STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
   ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
   POSSIBILITY OF SUCH DAMAGE.
*/

/*
   Recommended papers:
   
   Y. Ephraim and D. Malah, "Speech enhancement using minimum mean-square error
   short-time spectral amplitude estimator". IEEE Transactions on Acoustics, 
   Speech and Signal Processing, vol. ASSP-32, no. 6, pp. 1109-1121, 1984.
   
   Y. Ephraim and D. Malah, "Speech enhancement using minimum mean-square error
   log-spectral amplitude estimator". IEEE Transactions on Acoustics, Speech and 
   Signal Processing, vol. ASSP-33, no. 2, pp. 443-445, 1985.
   
   I. Cohen and B. Berdugo, "Speech enhancement for non-stationary noise environments".
   Signal Processing, vol. 81, no. 2, pp. 2403-2418, 2001.

   Stefan Gustafsson, Rainer Martin, Peter Jax, and Peter Vary. "A psychoacoustic 
   approach to combined acoustic echo cancellation and noise reduction". IEEE 
   Transactions on Speech and Audio Processing, 2002.
   
   J.-M. Valin, J. Rouat, and F. Michaud, "Microphone array post-filter for separation
   of simultaneous non-stationary sources". In Proceedings IEEE International 
   Conference on Acoustics, Speech, and Signal Processing, 2004.
*/

namespace Alanta.Client.Media.Dsp.Speex
{
	public class SpeexPreprocessFilter : IAudioInplaceFilter
	{
		#region Constructors

		public SpeexPreprocessFilter(int samplesPerFrame, int samplesPerSecond, MediaConfig config, IAudioTwoWayFilter echoCancelFilter, string instanceName = "")
		{
			this.samplesPerSecond = samplesPerSecond;
#if SILVERLIGHT
			st = this;
			InstanceName = instanceName;
			st.config = config;

			speex_preprocess_state_init(samplesPerFrame, samplesPerSecond);

			AgcLevel = 8000;
			DereverbEnabled = false;

			// ks 3/14/11 - VAD is supposedly a "kludge" right now, i.e., it's based on the overall power of the frame,
			// and that's it. See http://lists.xiph.org/pipermail/speex-dev/2006-March/004271.html for the "fix" that eventually
			// made its way into Speex as the "kludge". But turning it on seems to help, especially with AGC.
			VadEnabled = true;

			// ks 3/14/11 - Adjusted these, because the defaults amplify background noise too much.
			// See http://lists.xiph.org/pipermail/speex-dev/2007-May/005696.html
			AgcMaxGain = 15; // Default is 30
			// NoiseSuppression = -30; // Default is -15. Recommended is -30, but that sounds awful in my environment.
			EchoState = echoCancelFilter as SpeexEchoCanceller2; // Will store null if the provided echo canceller isn't a Speex echo canceller, which is what we want.
#endif
		}

		#endregion

		#region Fields and Properties

		/* Basic info */

		readonly int samplesPerSecond;

		/// <summary>
		/// This class
		/// </summary>
		private readonly SpeexPreprocessFilter st;

		/// <summary>
		/// Smoothed power spectrum
		/// </summary>
		private float[] S;

		/// <summary>
		/// See Cohen paper
		/// </summary>
		private float[] Smin;

		/// <summary>
		/// See Cohen paper
		/// </summary>
		private float[] Stmp;

		/// <summary>
		/// Current AGC gain
		/// </summary>
		private float agc_gain;

		private float agc_level;

		private SpeexFilterBank bank;

		/// <summary>
		/// A MediaConfig object that controls certain key parameters.
		/// </summary>
		private MediaConfig config;

		/* Parameters */
		private bool dereverb_enabled;
		private float[] echo_noise;
		private SpeexEchoCanceller2 echo_state;
		private int echo_suppress;
		private int echo_suppress_active;

		/// <summary>
		/// Fast Fourier Transform implementation
		/// </summary>
		private SpeexFft fft;

		/* DSP-related arrays */

		/// <summary>
		/// Processing frame (2*ps_size)
		/// </summary>
		private float[] frame;

		private const int frame_shift = 0;

		/// <summary>
		/// Number of samples processed each time
		/// </summary>
		private int frame_size;

		/// <summary>
		/// Processing frame in freq domain (2*ps_size)
		/// </summary>
		private float[] ft;

		/// <summary>
		/// Ephraim Malah gain
		/// </summary>
		private float[] gain;

		/// <summary>
		/// Adjusted gains
		/// </summary>
		private float[] gain2;

		/// <summary>
		/// Minimum gain allowed
		/// </summary>
		private float[] gain_floor;

		/* Misc */

		/// <summary>
		/// Input buffer (overlapped analysis)
		/// </summary>
		private float[] inbuf;

		/// <summary>
		/// Current gain limit during initialization
		/// </summary>
		private float init_max;

		/// <summary>
		/// Loudness estimate
		/// </summary>
		private float loudness;

		private float loudness_accum;

		/// <summary>
		/// Perceptual loudness curve
		/// </summary>
		private float[] loudness_weight;

		/// <summary>
		/// Maximum decrease in gain from one frame to another
		/// </summary>
		private float max_decrease_step;

		/// <summary>
		/// Maximum gain allowed
		/// </summary>
		private float max_gain;

		/// <summary>
		/// Maximum increase in gain from one frame to another
		/// </summary>
		private float max_increase_step;

		/// <summary>
		/// Number of frames processed so far
		/// </summary>
		private int min_count;

		/// <summary>
		/// Number of frames used for adaptation so far
		/// </summary>
		private int nb_adapt;

		private int nbands;

		/// <summary>
		/// Noise estimate
		/// </summary>
		private float[] noise;

		private int noise_suppress;

		/// <summary>
		/// Power spectrum for last frame
		/// </summary>
		private float[] old_ps;

		/// <summary>
		/// Output buffer (for overlapped and add)
		/// </summary>
		private float[] outbuf;

		/// <summary>
		/// A-posteriori SNR
		/// </summary>
		private float[] post;

		/// <summary>
		/// Loudness of previous frame
		/// </summary>
		private float prev_loudness;

		/// <summary>
		/// A-priori SNR
		/// </summary>
		private float[] prior;

		/// <summary>
		/// Current power spectrum
		/// </summary>
		private float[] ps;

		/// <summary>
		/// Number of points in the power spectrum
		/// </summary>
		private int ps_size;

		private float[] residual_echo;
		private float reverb_decay;

		/// <summary>
		/// Estimate of reverb energy
		/// </summary>
		private float[] reverb_estimate;

		/// <summary>
		/// Sampling rate of the input/output
		/// </summary>
		private int sampling_rate;

		/// <summary>
		/// Probability last frame was speech.
		/// </summary>
		private float speech_prob;

		private float speech_prob_continue;
		private float speech_prob_start;

		/// <summary>
		/// Probability of speech presence for noise update
		/// </summary>
		private int[] update_prob;

		private bool vad_enabled;

		private bool was_speech;

		/// <summary>
		/// Analysis/Synthesis window
		/// </summary>
		private float[] window;

		/// <summary>
		/// Smoothed a-priori SNR
		/// </summary>
		private float[] zeta;

		public string InstanceName { get; set; }

		#endregion

		#region Control Properties

		public float AgcLevel
		{
			get { return agc_level; }
			set
			{
				if (value < 1)
				{
					agc_level = 1;
				}
				else if (value > 32768)
				{
					agc_level = 32768;
				}
				else
				{
					agc_level = value;
				}
			}
		}

		public float AgcIncrement
		{
			get { return (float)Math.Floor(.5 + 8.6858 * Math.Log(max_increase_step) * sampling_rate / frame_size); }
			set { max_increase_step = (float)Math.Exp(0.11513f * value * frame_size / sampling_rate); }
		}

		public float AgcDecrement
		{
			get { return (float)Math.Floor(.5 + 8.6858 * (float)Math.Log(st.max_decrease_step) * st.sampling_rate / st.frame_size); }
			set { max_decrease_step = (float)Math.Exp(0.11513f * value * st.frame_size / st.sampling_rate); }
		}

		public float AgcMaxGain
		{
			get { return (float)Math.Floor(.5 + 8.6858 * (float)Math.Log(st.max_gain)); }
			set { max_gain = (float)Math.Exp(0.11513f * value); }
		}

		public bool VadEnabled
		{
			get { return vad_enabled; }
			set { vad_enabled = value; }
		}

		/// <summary>
		/// Determines whether Dereverb is enabled. 
		/// </summary>
		/// <remarks>
		/// ks 12/9/10 - So far as I can tell, dereverb was a planned feature that never got implemented.
		/// </remarks>
		public bool DereverbEnabled
		{
			get { return dereverb_enabled; }
			set { dereverb_enabled = value; }
		}

		public int SpeechProbabilityStart
		{
			get { return (int)(speech_prob_start * 100); }
			set
			{
				int probability = Math.Min(100, Math.Max(0, value));
				st.speech_prob_start = DIV32_16(MULT16_16(Q15ONE, probability), 100);
			}
		}

		public int SpeechProbabilityContinue
		{
			get { return (int)(speech_prob_continue * 100); }
			set
			{
				int probability = Math.Max(0, value);
				speech_prob_continue = probability / 100;
			}
		}

		public int NoiseSuppression
		{
			get { return noise_suppress; }
			set { noise_suppress = Math.Abs(value); }
		}

		public int EchoSuppression
		{
			get { return echo_suppress; }
			set { echo_suppress = -Math.Abs(value); }
		}

		public int EchoSuppressionActive
		{
			get { return echo_suppress_active; }
			set { echo_suppress_active = -Math.Abs(value); }
		}

		public SpeexEchoCanceller2 EchoState
		{
			get { return echo_state; }
			set { echo_state = value; }
		}

		public float AgcLoudness
		{
			get { return (float)Math.Pow(loudness, 1.0 / LOUDNESS_EXP); }
		}

		public float AgcGain
		{
			get { return (float)Math.Floor(.5 + 8.6858 * (float)Math.Log(st.agc_gain)); }
		}

		public int NoisePsdSize
		{
			get { return ps_size; }
		}

		public int[] Psd
		{
			get
			{
				var psd = new int[ps_size];
				for (int i = 0; i < st.ps_size; i++)
				{
					psd[i] = (int)st.ps[i];
				}
				return psd;
			}
		}

		public int[] NoisePsd
		{
			get
			{
				var noisePsd = new int[ps_size];
				for (int i = 0; i < ps_size; i++)
				{
					noisePsd[i] = (int)st.noise[i];
				}
				return noisePsd;
			}
		}

		public float SpeechProbability
		{
			get { return speech_prob * 100; }
		}

		public int AgcTarget
		{
			get { return (int)agc_level; }
			set
			{
				if (value < 1)
				{
					agc_level = 1;
				}
				else if (value > short.MaxValue)
				{
					agc_level = short.MaxValue;
				}
				else
				{
					agc_level = value;
				}
			}
		}

		#endregion

		#region Pseudo-macros

		private const float Q15_ONE = 1.0f;
		private const float Q15ONE = 1.0f;
		private const float FLOAT_ONE = 1.0f;
		private const float FLOAT_ZERO = 0.0f;

		private const float LOUDNESS_EXP = 5.0f;
		private const float AMP_SCALE = .001f;
		private const float AMP_SCALE_1 = 1000.0f;

		private const int NB_BANDS = 24;

		private const float SPEECH_PROB_START_DEFAULT = 0.35f;
		private const float SPEECH_PROB_CONTINUE_DEFAULT = 0.20f;
		private const int NOISE_SUPPRESS_DEFAULT = -15;
		private const int ECHO_SUPPRESS_DEFAULT = -40;
		private const int ECHO_SUPPRESS_ACTIVE_DEFAULT = -15;

		private const int NULL = 0;
		private const float SNR_SCALING = 1.0f;
		private const float SNR_SCALING_1 = 1.0f;
		private const int SNR_SHIFT = 0;
		private const float FRAC_SCALING = 1.0f;
		private const float FRAC_SCALING_1 = 1.0f;
		private const int FRAC_SHIFT = 0;
		private const int NOISE_SHIFT = 0;
		private const int EXPIN_SHIFT = 11;

		private const float EXPIN_SCALING = 1.0f;
		private const float EXPIN_SCALING_1 = 1.0f;
		private const float EXPOUT_SCALING_1 = 1.0f;

		private static float NEG16(float x)
		{
			return -x;
		}

		private static float NEG32(float x)
		{
			return -x;
		}

		private static short NEG16(short x)
		{
			return (short)-x;
		}

		private static int NEG32(int x)
		{
			return -x;
		}

		private static float ADD16(float a, float b)
		{
			return a + b;
		}

		private static float ADD32(float a, float b)
		{
			return a + b;
		}

		private static short ADD16(short a, short b)
		{
			return (short)(a + b);
		}

		private static int ADD32(short a, short b)
		{
			return a + b;
		}

		private static float SUB16(float a, float b)
		{
			return a - b;
		}

		private static float SUB32(float a, float b)
		{
			return a - b;
		}

		private static short SUB16(short a, short b)
		{
			return (short)(a - b);
		}

		private static int SUB32(short a, short b)
		{
			return a - b;
		}

		private static float DIV32(float a, float b)
		{
			return a / b;
		}

		private static float DIV32_16_Q8(float a, float b)
		{
			return a / b;
		}

		private static float DIV32_16_Q15(float a, float b)
		{
			return a / b;
		}

		private static float PDIV32_16(float a, float b)
		{
			return a / b;
		}

		private static float FLOAT_MUL32(float a, float b)
		{
			return a * b;
		}

		private static float MAC16_16(float c, float a, float b)
		{
			return c + a * b;
		}

		private static float MULT16_16(float a, float b)
		{
			return a * b;
		}

		private static float MULT16_32_P15(float a, float b)
		{
			return a * b;
		}

		private static float MULT16_16_Q14(float a, float b)
		{
			return a * b;
		}

		private static float MULT16_16_Q15(float a, float b)
		{
			return a * b;
		}

		private static float FLOAT_AMULT(float a, float b)
		{
			return a * b;
		}

		private static float DIV32_16(float a, float b)
		{
			return a / b;
		}

		private static float MULT16_32_Q15(float a, float b)
		{
			return a * b;
		}

		private static float MULT16_16_P15(float a, float b)
		{
			return a * b;
		}

		private static float FLOAT_ADD(float a, float b)
		{
			return a + b;
		}

		private static float FLOAT_MULT(float a, float b)
		{
			return a * b;
		}

		private static float FLOAT_MUL32U(float a, float b)
		{
			return a * b;
		}

		private static float FLOAT_DIVU(float a, float b)
		{
			return a / b;
		}

		private static float FLOAT_DIV32(float a, float b)
		{
			return a / b;
		}

		private static float FLOAT_DIV32_FLOAT(float a, float b)
		{
			return a / b;
		}

		private static float FLOAT_SUB(float a, float b)
		{
			return a - b;
		}

		private static float FLOAT_EXTRACT16(float a)
		{
			return a;
		}

		private static bool FLOAT_GT(float a, float b)
		{
			return a > b;
		}

		private static bool FLOAT_LT(float a, float b)
		{
			return a < b;
		}

		private static float ABS32(float a)
		{
			return Math.Abs(a);
		}

		private static float EXTRACT16(float a)
		{
			return a;
		}

		private static float SATURATE16(float x, float a)
		{
			return x;
		}

		private static float SATURATE32(float x, float a)
		{
			return x;
		}

		private static float PSHR32(float a, int shift)
		{
			return a;
		}

		private static float SHR32(float a, int shift)
		{
			return a;
		}

		private static float SHL32(float a, int shift)
		{
			return a;
		}

		private static float SHR16(float a, int shift)
		{
			return a;
		}

		private static float SHL16(float a, int shift)
		{
			return a;
		}

		private static float QCONST16(float a, int bits)
		{
			return a;
		}

		private static float QCONST32(float a, int bits)
		{
			return a;
		}

		private static float EXTEND32(float a)
		{
			return a;
		}

		private static float FLOAT_SHL(float a, int b)
		{
			return a;
		}

		private static float PSEUDOFLOAT(float a)
		{
			return a;
		}

		private static short FLOAT_TO_SHORT(float x)
		{
			return Convert.ToInt16((x) < -32767.5f ? -32768 : ((x) > 32766.5f ? 32767 : Math.Floor(.5 + (x))));
		}

		private static float SQR16_Q15(float a)
		{
			return a * a;
		}

		private static float spx_cos_norm(float x)
		{
			return (float)(Math.Cos((.5f * Math.PI) * (x)));
		}

		#endregion

		#region Methods
		public void Filter(short[] sampleData)
		{
			speex_preprocess_run(sampleData);
		}
		private static void conj_window(float[] w, int len)
		{
			int i;
			for (i = 0; i < len; i++)
			{
				float x = DIV32_16(MULT16_16(QCONST16(4.0f, 13), i), len);
				int inv = 0;
				if (x < QCONST16(1.0f, 13))
				{
				}
				else if (x < QCONST16(2.0f, 13))
				{
					x = QCONST16(2.0f, 13) - x;
					inv = 1;
				}
				else if (x < QCONST16(3.0f, 13))
				{
					x = x - QCONST16(2.0f, 13);
					inv = 1;
				}
				else
				{
					x = QCONST16(2.0f, 13) - x + QCONST16(2.0f, 13); /* 4 - x */
				}
				x = MULT16_16_Q14(QCONST16(1.271903f, 14), x);
				float tmp = SQR16_Q15(QCONST16(.5f, 15) - MULT16_16_P15(QCONST16(.5f, 15), spx_cos_norm(SHL32(EXTEND32(x), 2))));
				if (inv != 0)
				{
					tmp = SUB16(Q15_ONE, tmp);
				}
				w[i] = (float)Math.Sqrt(SHL32(EXTEND32(tmp), 15));
			}
		}

		/// <summary>
		/// This function approximates the gain function 
		/// y = gamma(1.25)^2 * M(-.25;1;-x) / sqrt(x)  
		/// which multiplied by xi/(1+xi) is the optimal gain
		/// in the loudness domain ( sqrt[amplitude] )
		/// </summary>
		/// <param name="xx"></param>
		/// <returns></returns>
		private static float hypergeom_gain(float xx)
		{
			var table = new[]
			{
				0.82157f, 1.02017f, 1.20461f, 1.37534f, 1.53363f, 1.68092f, 1.81865f,
				1.94811f, 2.07038f, 2.18638f, 2.29688f, 2.40255f, 2.50391f, 2.60144f,
				2.69551f, 2.78647f, 2.87458f, 2.96015f, 3.04333f, 3.12431f, 3.20326f
			};
			float x = EXPIN_SCALING_1 * xx;
			float integer = (float)Math.Floor(2 * x);
			int ind = (int)integer;
			if (ind < 0)
			{
				return FRAC_SCALING;
			}
			if (ind > 19)
			{
				return FRAC_SCALING * (1 + .1296f / x);
			}
			float frac = 2 * x - integer;
			return (float)(FRAC_SCALING * ((1 - frac) * table[ind] + frac * table[ind + 1]) / Math.Sqrt(x + .0001f));
		}
		private static float qcurve(float x)
		{
			return 1.0f / (1.0f + .15f / (SNR_SCALING_1 * x));
		}
		private static void compute_gain_floor(int noiseSuppress, int effectiveEchoSuppress,
											   float[] noise, int noiseoffset,
											   float[] echo, int echooffset,
											   float[] gainFloor, int gainFloorOffset,
											   int len)
		{
			var noiseFloor = (float)Math.Exp(.2302585f * noiseSuppress);
			var echoFloor = (float)Math.Exp(.2302585f * effectiveEchoSuppress);

			/* Compute the gain floor based on different floors for the background noise and residual echo */
			for (int i = 0; i < len; i++)
			{
				float noiseVal = noise[noiseoffset + i];
				float echoVal = echo[echooffset + i];
				gainFloor[gainFloorOffset + i] =
					(float)(FRAC_SCALING * Math.Sqrt(noiseFloor * noiseVal + echoFloor * echoVal) / Math.Sqrt(1 + noiseVal + echoVal));
			}
		}
		private void speex_preprocess_state_init(int frameSize, int sampling_rate)
		{
			int i;
			int N, N3, N4, M;

			st.frame_size = frameSize;

			/* Round ps_size down to the nearest power of two */
			st.ps_size = st.frame_size;

			N = st.ps_size;
			N3 = 2 * N - st.frame_size;
			N4 = st.frame_size - N3;

			st.sampling_rate = sampling_rate;
			// st.denoise_enabled = true;
			st.vad_enabled = false;
			st.dereverb_enabled = false;
			st.reverb_decay = 0;
			st.noise_suppress = NOISE_SUPPRESS_DEFAULT;
			st.echo_suppress = ECHO_SUPPRESS_DEFAULT;
			st.echo_suppress_active = ECHO_SUPPRESS_ACTIVE_DEFAULT;

			st.speech_prob_start = SPEECH_PROB_START_DEFAULT;
			st.speech_prob_continue = SPEECH_PROB_CONTINUE_DEFAULT;

			st.echo_state = null;

			st.nbands = NB_BANDS;
			M = st.nbands;
			st.bank = SpeexFilterBank.filterbank_new(M, sampling_rate, N, 1);

			st.frame = new float[2 * N]; // (float[] )speex_alloc(2*N*sizeof(float));
			st.window = new float[2 * N]; // (float[] )speex_alloc(2*N*sizeof(float));
			st.ft = new float[2 * N]; // (float[] )speex_alloc(2*N*sizeof(float));

			st.ps = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.noise = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.echo_noise = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.residual_echo = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.reverb_estimate = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.old_ps = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.prior = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.post = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.gain = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.gain2 = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.gain_floor = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));
			st.zeta = new float[N + M]; // (float[] )speex_alloc((N+M)*sizeof(float));

			st.S = new float[N]; // (float[] )speex_alloc(N*sizeof(float));
			st.Smin = new float[N]; //(float[] )speex_alloc(N*sizeof(float));
			st.Stmp = new float[N]; //(float[] )speex_alloc(N*sizeof(float));
			st.update_prob = new int[N]; // (int*)speex_alloc(N*sizeof(int));

			st.inbuf = new float[N3]; //(float[] )speex_alloc(N3*sizeof(float));
			st.outbuf = new float[N3]; //(float[] )speex_alloc(N3*sizeof(float));

			conj_window(st.window, 2 * N3);
			for (i = 2 * N3; i < 2 * st.ps_size; i++)
			{
				st.window[i] = Q15_ONE;
			}

			if (N4 > 0)
			{
				for (i = N3 - 1; i >= 0; i--)
				{
					st.window[i + N3 + N4] = st.window[i + N3];
					st.window[i + N3] = 1;
				}
			}
			for (i = 0; i < N + M; i++)
			{
				st.noise[i] = QCONST32(1.0f, NOISE_SHIFT);
				st.reverb_estimate[i] = 0;
				st.old_ps[i] = 1;
				st.gain[i] = Q15_ONE;
				st.post[i] = SHL16(1.0f, SNR_SHIFT);
				st.prior[i] = SHL16(1.0f, SNR_SHIFT);
			}

			for (i = 0; i < N; i++)
			{
				st.update_prob[i] = 1;
			}
			for (i = 0; i < N3; i++)
			{
				st.inbuf[i] = 0;
				st.outbuf[i] = 0;
			}
			// st.agc_enabled = false;
			st.agc_level = 8000;
			st.loudness_weight = new float[N]; // (float*)speex_alloc(N * sizeof(float));
			for (i = 0; i < N; i++)
			{
				float ff = (i) * .5f * sampling_rate / (N);
				/*st.loudness_weight[i] = .5f*(1.0f/(1.0f+ff/8000.0f))+1.0f*Math.Exp(-.5f*(ff-3800.0f)*(ff-3800.0f)/9e5f);*/
				st.loudness_weight[i] = (float)(.35f - .35f * ff / 16000.0f + .73f * Math.Exp(-.5f * (ff - 3800) * (ff - 3800) / 9e5f));
				if (st.loudness_weight[i] < .01f)
				{
					st.loudness_weight[i] = .01f;
				}
				st.loudness_weight[i] *= st.loudness_weight[i];
			}
			/*st.loudness = pow(AMP_SCALE*st.agc_level,LOUDNESS_EXP);*/
			st.loudness = 1e-15f;
			st.agc_gain = 1;
			st.max_gain = 30;
			st.max_increase_step = (float)Math.Exp(0.11513f * 12.0f * st.frame_size / st.sampling_rate);
			st.max_decrease_step = (float)Math.Exp(-0.11513f * 40.0f * st.frame_size / st.sampling_rate);
			st.prev_loudness = 1;
			st.init_max = 1;
			st.was_speech = false;

			st.fft = new SpeexFft(2 * N);

			st.nb_adapt = 0;
			st.min_count = 0;
		}
		private void speex_compute_agc(float Pframe, float[] ft)
		{
			int i;
			int N = st.ps_size;
			float loudness = 1.0f;

			for (i = 2; i < N; i++)
			{
				loudness += 2.0f * N * st.ps[i] * st.loudness_weight[i];
			}
			loudness = (float)Math.Sqrt(loudness);
			/*if (loudness < 2*pow(st.loudness, 1.0/LOUDNESS_EXP) &&
		 loudness*2 > pow(st.loudness, 1.0/LOUDNESS_EXP))*/
			if (Pframe > .3f)
			{
				/*rate=2.0f*Pframe*Pframe/(1+st.nb_loudness_adapt);*/
				float rate = .03f * Pframe * Pframe;
				st.loudness = (float)((1 - rate) * st.loudness + (rate) * Math.Pow(AMP_SCALE * loudness, LOUDNESS_EXP));
				st.loudness_accum = (1 - rate) * st.loudness_accum + rate;
				if (st.init_max < st.max_gain && st.nb_adapt > 20)
				{
					st.init_max *= 1.0f + .1f * Pframe * Pframe;
				}
			}
			/*printf ("%f %f %f %f\n", Pframe, loudness, pow(st.loudness, 1.0f/LOUDNESS_EXP), st.loudness2);*/

			float targetGain = AMP_SCALE * st.agc_level * (float)Math.Pow(st.loudness / (1e-4 + st.loudness_accum), -1.0f / LOUDNESS_EXP);

			if ((Pframe > .5 && st.nb_adapt > 20) || targetGain < st.agc_gain)
			{
				if (targetGain > st.max_increase_step * st.agc_gain)
				{
					targetGain = st.max_increase_step * st.agc_gain;
				}
				if (targetGain < st.max_decrease_step * st.agc_gain && loudness < 10 * st.prev_loudness)
				{
					targetGain = st.max_decrease_step * st.agc_gain;
				}
				if (targetGain > st.max_gain)
				{
					targetGain = st.max_gain;
				}
				if (targetGain > st.init_max)
				{
					targetGain = st.init_max;
				}

				st.agc_gain = targetGain;
			}
			/*fprintf (stderr, "%f %f %f\n", loudness, (float)AMP_SCALE_1*pow(st.loudness, 1.0f/LOUDNESS_EXP), st.agc_gain);*/

			for (i = 0; i < 2 * N; i++)
			{
				ft[i] *= st.agc_gain;
			}
			st.prev_loudness = loudness;
		}
		private void preprocess_analysis(short[] x)
		{
			int i;
			int N = st.ps_size;
			int N3 = 2 * N - st.frame_size;
			int N4 = st.frame_size - N3;
			float[] ps = st.ps;

			/* 'Build' input frame */
			for (i = 0; i < N3; i++)
			{
				st.frame[i] = st.inbuf[i];
			}
			for (i = 0; i < st.frame_size; i++)
			{
				st.frame[N3 + i] = x[i];
			}

			/* Update inbuf */
			for (i = 0; i < N3; i++)
			{
				st.inbuf[i] = x[N4 + i];
			}

			/* Windowing */
			for (i = 0; i < 2 * N; i++)
			{
				st.frame[i] = MULT16_16_Q15(st.frame[i], st.window[i]);
			}

			/* Perform FFT */
			st.fft.DoFft(st.frame, 0, st.ft, 0);

			/* Power spectrum */
			ps[0] = MULT16_16(st.ft[0], st.ft[0]);
			for (i = 1; i < N; i++)
			{
				ps[i] = MULT16_16(st.ft[2 * i - 1], st.ft[2 * i - 1]) + MULT16_16(st.ft[2 * i], st.ft[2 * i]);
			}
			for (i = 0; i < N; i++)
			{
				st.ps[i] = PSHR32(st.ps[i], 2 * frame_shift);
			}

			SpeexFilterBank.filterbank_compute_bank32(st.bank, ps, ps, N);
		}
		private void update_noise_prob()
		{
			int i;
			int min_range;
			int N = st.ps_size;

			for (i = 1; i < N - 1; i++)
			{
				st.S[i] = MULT16_32_Q15(QCONST16(.8f, 15), st.S[i]) + MULT16_32_Q15(QCONST16(.05f, 15), st.ps[i - 1])
						  + MULT16_32_Q15(QCONST16(.1f, 15), st.ps[i]) + MULT16_32_Q15(QCONST16(.05f, 15), st.ps[i + 1]);
			}
			st.S[0] = MULT16_32_Q15(QCONST16(.8f, 15), st.S[0]) + MULT16_32_Q15(QCONST16(.2f, 15), st.ps[0]);
			st.S[N - 1] = MULT16_32_Q15(QCONST16(.8f, 15), st.S[N - 1]) + MULT16_32_Q15(QCONST16(.2f, 15), st.ps[N - 1]);

			if (st.nb_adapt == 1)
			{
				for (i = 0; i < N; i++)
				{
					st.Smin[i] = st.Stmp[i] = 0;
				}
			}

			if (st.nb_adapt < 100)
			{
				min_range = 15;
			}
			else if (st.nb_adapt < 1000)
			{
				min_range = 50;
			}
			else if (st.nb_adapt < 10000)
			{
				min_range = 150;
			}
			else
			{
				min_range = 300;
			}
			if (st.min_count > min_range)
			{
				st.min_count = 0;
				for (i = 0; i < N; i++)
				{
					st.Smin[i] = Math.Min(st.Stmp[i], st.S[i]);
					st.Stmp[i] = st.S[i];
				}
			}
			else
			{
				for (i = 0; i < N; i++)
				{
					st.Smin[i] = Math.Min(st.Smin[i], st.S[i]);
					st.Stmp[i] = Math.Min(st.Stmp[i], st.S[i]);
				}
			}
			for (i = 0; i < N; i++)
			{
				if (MULT16_32_Q15(QCONST16(.4f, 15), st.S[i]) > st.Smin[i])
				{
					st.update_prob[i] = 1;
				}
				else
				{
					st.update_prob[i] = 0;
				}
				/*fprintf (stderr, "%f ", st.S[i]/st.Smin[i]);*/
				/*fprintf (stderr, "%f ", st.update_prob[i]);*/
			}
		}
		private int speex_preprocess(short[] x, int[] echo)
		{
			return speex_preprocess_run(x);
		}
		public int speex_preprocess_run(short[] x)
		{
			int N = st.ps_size;
			int N3 = 2 * N - st.frame_size;
			int N4 = st.frame_size - N3;

			st.nb_adapt++;
			if (st.nb_adapt > 20000)
			{
				st.nb_adapt = 20000;
			}
			st.min_count++;

			float beta = Math.Max(QCONST16(.03f, 15), DIV32_16(Q15_ONE, st.nb_adapt));
			float beta_1 = Q15_ONE - beta;
			int M = st.nbands;

			/* Deal with residual echo if provided */
			if (st.echo_state != null && config.EnableAec)
			{
				// SpeexEchoCanceller.speex_echo_get_residual(st.echo_state, st.residual_echo, N);
				st.echo_state.GetResidual(st.residual_echo);
				/* If there are NaNs or ridiculous values, it'll show up in the DC and we just reset everything to zero */
				if (!(st.residual_echo[0] >= 0 && st.residual_echo[0] < N * 1e9f))
				{
					for (int i = 0; i < N; i++)
					{
						st.residual_echo[i] = 0;
					}
				}
				for (int i = 0; i < N; i++)
				{
					st.echo_noise[i] = Math.Max(MULT16_32_Q15(QCONST16(.6f, 15), st.echo_noise[i]), st.residual_echo[i]);
				}
				SpeexFilterBank.filterbank_compute_bank32(st.bank, st.echo_noise, st.echo_noise, N);
			}
			else
			{
				for (int i = 0; i < N + M; i++)
				{
					st.echo_noise[i] = 0;
				}
			}
			preprocess_analysis(x);

			update_noise_prob();

			/* Update the noise estimate for the frequencies where it can be */
			for (int i = 0; i < N; i++)
			{
				if (st.update_prob[i] == 0 || st.ps[i] < PSHR32(st.noise[i], NOISE_SHIFT))
				{
					st.noise[i] = Math.Max(EXTEND32(0), MULT16_32_Q15(beta_1, st.noise[i]) + MULT16_32_Q15(beta, SHL32(st.ps[i], NOISE_SHIFT)));
				}
			}
			SpeexFilterBank.filterbank_compute_bank32(st.bank, st.noise, st.noise, N);

			/* Special case for first frame */
			if (st.nb_adapt == 1)
			{
				for (int i = 0; i < N + M; i++)
				{
					st.old_ps[i] = ps[i];
				}
			}

			/* Compute a posteriori SNR */
			for (int i = 0; i < N + M; i++)
			{
				/* Total noise estimate including residual echo and reverberation */
				float tot_noise = ADD32(ADD32(ADD32(EXTEND32(1), PSHR32(st.noise[i], NOISE_SHIFT)), st.echo_noise[i]), st.reverb_estimate[i]);

				/* A posteriori SNR = ps/noise - 1*/
				st.post[i] = SUB16(DIV32_16_Q8(ps[i], tot_noise), QCONST16(1.0f, SNR_SHIFT));
				st.post[i] = Math.Min(st.post[i], QCONST16(100.0f, SNR_SHIFT));

				/* Computing update gamma = .1 + .9*(old/(old+noise))^2 */
				float gamma = QCONST16(.1f, 15) + MULT16_16_Q15(QCONST16(.89f, 15), SQR16_Q15(DIV32_16_Q15(st.old_ps[i], ADD32(st.old_ps[i], tot_noise))));

				/* A priori SNR update = gamma*max(0,post) + (1-gamma)*old/noise */
				st.prior[i] = EXTRACT16(PSHR32(ADD32(MULT16_16(gamma, Math.Max(0, st.post[i])), MULT16_16(Q15_ONE - gamma, DIV32_16_Q8(st.old_ps[i], tot_noise))), 15));
				st.prior[i] = Math.Min(st.prior[i], QCONST16(100.0f, SNR_SHIFT));
			}

			/*print_vec(st.post, N+M, "");*/

			/* Recursive average of the a priori SNR. A bit smoothed for the psd components */
			st.zeta[0] = PSHR32(ADD32(MULT16_16(QCONST16(.7f, 15), st.zeta[0]), MULT16_16(QCONST16(.3f, 15), st.prior[0])), 15);
			for (int i = 1; i < N - 1; i++)
			{
				st.zeta[i] = PSHR32(ADD32(ADD32(ADD32(MULT16_16(QCONST16(.7f, 15), st.zeta[i]), MULT16_16(QCONST16(.15f, 15), st.prior[i])),
												MULT16_16(QCONST16(.075f, 15), st.prior[i - 1])), MULT16_16(QCONST16(.075f, 15), st.prior[i + 1])), 15);
			}
			for (int i = N - 1; i < N + M; i++)
			{
				st.zeta[i] = PSHR32(ADD32(MULT16_16(QCONST16(.7f, 15), st.zeta[i]), MULT16_16(QCONST16(.3f, 15), st.prior[i])), 15);
			}

			/* Speech probability of presence for the entire frame is based on the average filterbank a priori SNR */
			float Zframe = 0;
			for (int i = N; i < N + M; i++)
			{
				Zframe = ADD32(Zframe, EXTEND32(st.zeta[i]));
			}
			float Pframe = QCONST16(.1f, 15) + MULT16_16_Q15(QCONST16(.899f, 15), qcurve(DIV32_16(Zframe, st.nbands)));

			float effectiveEchoSuppress = EXTRACT16(PSHR32(ADD32(MULT16_16(SUB16(Q15_ONE, Pframe), st.echo_suppress), MULT16_16(Pframe, st.echo_suppress_active)), 15));

			compute_gain_floor(st.noise_suppress, (int)effectiveEchoSuppress, st.noise, N, st.echo_noise, N, st.gain_floor, N, M);

			/* Compute Ephraim & Malah gain speech probability of presence for each critical band (Bark scale) 
			   Technically this is actually wrong because the EM gain assumes a slightly different probability 
			   distribution */
			for (int i = N; i < N + M; i++)
			{
				/* Weiner filter gain */
				float priorRatio = PDIV32_16(SHL32(EXTEND32(st.prior[i]), 15), ADD16(st.prior[i], SHL32(1, SNR_SHIFT)));

				/* See EM and Cohen papers*/
				float theta = MULT16_32_P15(priorRatio, QCONST32(1.0f, EXPIN_SHIFT) + SHL32(EXTEND32(st.post[i]), EXPIN_SHIFT - SNR_SHIFT));

				/* Gain from hypergeometric function */
				float MM = hypergeom_gain(theta);

				/* Gain with bound */
				st.gain[i] = EXTRACT16(Math.Min(Q15_ONE, MULT16_32_Q15(priorRatio, MM)));
				/* Save old Bark power spectrum */
				st.old_ps[i] = MULT16_32_P15(QCONST16(.2f, 15), st.old_ps[i]) + MULT16_32_P15(MULT16_16_P15(QCONST16(.8f, 15), SQR16_Q15(st.gain[i])), ps[i]);

				/* a priority probability of speech presence based on Bark sub-band alone */
				float P1 = QCONST16(.199f, 15) + MULT16_16_Q15(QCONST16(.8f, 15), qcurve(st.zeta[i]));

				/* Speech absence a priori probability (considering sub-band and frame) */
				float q = Q15_ONE - MULT16_16_Q15(Pframe, P1);
				st.gain2[i] = 1 / (1.0f + (q / (1.0f - q)) * (1 + st.prior[i]) * (float)Math.Exp(-theta));
			}
			/* Convert the EM gains and speech prob to linear frequency */
			SpeexFilterBank.filterbank_compute_psd16(st.bank, st.gain2, N, st.gain2);
			SpeexFilterBank.filterbank_compute_psd16(st.bank, st.gain, N, st.gain);

			/* Linear gain resolution (best) */
			SpeexFilterBank.filterbank_compute_psd16(st.bank, st.gain_floor, N, st.gain_floor);

			/* Compute gain according to the Ephraim-Malah algorithm -- linear frequency */
			for (int i = 0; i < N; i++)
			{
				/* Wiener filter gain */
				float priorRatio = PDIV32_16(SHL32(EXTEND32(st.prior[i]), 15), ADD16(st.prior[i], SHL32(1, SNR_SHIFT)));
				float theta = MULT16_32_P15(priorRatio, QCONST32(1.0f, EXPIN_SHIFT) + SHL32(EXTEND32(st.post[i]), EXPIN_SHIFT - SNR_SHIFT));

				/* Optimal estimator for loudness domain */
				float MM = hypergeom_gain(theta);
				/* EM gain with bound */
				float g = EXTRACT16(Math.Min(Q15_ONE, MULT16_32_Q15(priorRatio, MM)));
				/* Interpolated speech probability of presence */
				float p = st.gain2[i];

				/* Constrain the gain to be close to the Bark scale gain */
				if (MULT16_16_Q15(QCONST16(.333f, 15), g) > st.gain[i])
				{
					g = MULT16_16(3, st.gain[i]);
				}
				st.gain[i] = g;

				/* Save old power spectrum */
				st.old_ps[i] = MULT16_32_P15(QCONST16(.2f, 15), st.old_ps[i]) + MULT16_32_P15(MULT16_16_P15(QCONST16(.8f, 15), SQR16_Q15(st.gain[i])), ps[i]);

				/* Apply gain floor */
				if (st.gain[i] < st.gain_floor[i])
				{
					st.gain[i] = st.gain_floor[i];
				}

				/* Exponential decay model for reverberation (unused) */
				/*st.reverb_estimate[i] = st.reverb_decay*st.reverb_estimate[i] + st.reverb_decay*st.reverb_level*st.gain[i]*st.gain[i]*st.ps[i];*/

				/* Take into account speech probability of presence (loudness domain MMSE estimator) */
				/* gain2 = [p*sqrt(gain)+(1-p)*sqrt(gain _floor) ]^2 */
				float tmp = MULT16_16_P15(p, (float)Math.Sqrt(SHL32(EXTEND32(st.gain[i]), 15))) + MULT16_16_P15(SUB16(Q15_ONE, p), (float)Math.Sqrt(SHL32(EXTEND32(st.gain_floor[i]), 15)));
				st.gain2[i] = SQR16_Q15(tmp);

				/* Use this if you want a log-domain MMSE estimator instead */
				/*st.gain2[i] = pow(st.gain[i], p) * pow(st.gain_floor[i],1.0f-p);*/
			}


			/* If noise suppression is off, don't apply the gain (but then why call this in the first place!) */
			if (!st.config.EnableDenoise)
			{
				for (int i = 0; i < N + M; i++)
				{
					st.gain2[i] = Q15_ONE;
				}
			}

			/* Apply computed gain */
			for (int i = 1; i < N; i++)
			{
				st.ft[2 * i - 1] = MULT16_16_P15(st.gain2[i], st.ft[2 * i - 1]);
				st.ft[2 * i] = MULT16_16_P15(st.gain2[i], st.ft[2 * i]);
			}
			st.ft[0] = MULT16_16_P15(st.gain2[0], st.ft[0]);
			st.ft[2 * N - 1] = MULT16_16_P15(st.gain2[N - 1], st.ft[2 * N - 1]);

			if (st.config.EnableAgc)
			{
				speex_compute_agc(Pframe, st.ft);
			}

			/* Inverse FFT with 1/N scaling */
			st.fft.DoIfft(st.ft, 0, st.frame, 0);
			/* Scale back to original (lower) amplitude */
			// for (i = 0; i < 2 * N; i++)
			// st.frame[i] = PSHR16(st.frame[i], st.frame_shift);

			/*FIXME: This *will* not work for fixed-point */
			if (st.config.EnableAgc)
			{
				float maxSample = 0;
				for (int i = 0; i < 2 * N; i++)
				{
					if (Math.Abs(st.frame[i]) > maxSample)
					{
						maxSample = Math.Abs(st.frame[i]);
					}
				}
				if (maxSample > 28000.0f)
				{
					float damp = 28000.0f / maxSample;
					for (int i = 0; i < 2 * N; i++)
					{
						st.frame[i] *= damp;
					}
				}
			}

			/* Synthesis window (for WOLA) */
			for (int i = 0; i < 2 * N; i++)
			{
				st.frame[i] = MULT16_16_Q15(st.frame[i], st.window[i]);
			}

			/* Perform overlap and add */
			for (int i = 0; i < N3; i++)
			{
				x[i] = (short)(st.outbuf[i] + st.frame[i]);
			}
			for (int i = 0; i < N4; i++)
			{
				x[N3 + i] = (short)st.frame[N3 + i];
			}

			/* Update outbuf */
			for (int i = 0; i < N3; i++)
			{
				st.outbuf[i] = st.frame[st.frame_size + i];
			}

			/* FIXME: This VAD is a kludge */
			st.speech_prob = Pframe;
			if (st.vad_enabled)
			{
				if (st.speech_prob > st.speech_prob_start || (st.was_speech && st.speech_prob > st.speech_prob_continue))
				{
					st.was_speech = true;
					return 1;
				}
				else
				{
					st.was_speech = false;
					return 0;
				}
			}
			else
			{
				return 1;
			}
		}
		public void speex_preprocess_estimate_update(short[] x)
		{
			SpeexPreprocessFilter st = this;
			int i;
			int N = st.ps_size;
			int N3 = 2 * N - st.frame_size;
			int M;
			float[] ps = st.ps;

			M = st.nbands;
			st.min_count++;

			preprocess_analysis(x);

			update_noise_prob();

			for (i = 1; i < N - 1; i++)
			{
				if (st.update_prob[i] == 0 || st.ps[i] < PSHR32(st.noise[i], NOISE_SHIFT))
				{
					st.noise[i] = MULT16_32_Q15(QCONST16(.95f, 15), st.noise[i]) + MULT16_32_Q15(QCONST16(.05f, 15), SHL32(st.ps[i], NOISE_SHIFT));
				}
			}

			for (i = 0; i < N3; i++)
			{
				st.outbuf[i] = MULT16_16_Q15(x[st.frame_size - N3 + i], st.window[st.frame_size + i]);
			}

			/* Save old power spectrum */
			for (i = 0; i < N + M; i++)
			{
				st.old_ps[i] = ps[i];
			}

			for (i = 0; i < N; i++)
			{
				st.reverb_estimate[i] = MULT16_32_Q15(st.reverb_decay, st.reverb_estimate[i]);
			}
		}
		#endregion

	}
}