#define TWO_PATH

using System;
using System.Diagnostics;

/* Copyright (C) 2003-2008 Jean-Marc Valin

   File: mdf.c
   Echo canceller based on the MDF algorithm (see below)

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
   The echo canceller is based on the MDF algorithm described in:

   J. S. Soo, K. K. Pang Multidelay block frequency adaptive filter, 
   IEEE Trans. Acoust. Speech Signal Process., Vol. ASSP-38, No. 2, 
   February 1990.
  
 * ks 10/11/2010 - An improved version of this algorithm (not implemented) is described here:
 * http://www.eurasip.org/Proceedings/Eusipco/Eusipco2005/defevent/papers/cr1373.pdf
 * See also:
 * http://www.eurasip.org/proceedings/eusipco/eusipco2009/contents/papers/1569192722.pdf
   
   We use the Alternatively Updated MDF (AUMDF) variant. Robustness to 
   double-talk is achieved using a variable learning rate as described in:
   
   Valin, J.-M., On Adjusting the Learning Rate in Frequency Domain Echo 
   Cancellation With Double-Talk. IEEE Transactions on Audio,
   Speech and Language Processing, Vol. 15, No. 3, pp. 1030-1034, 2007.
   http://people.xiph.org/~jm/papers/valin_taslp2006.pdf
   
   There is no explicit double-talk detection, but a continuous variation
   in the learning rate based on residual echo, double-talk and background
   noise.
   
   About the fixed-point version:
   All the signals are represented with 16-bit words. The filter weights 
   are represented with 32-bit words, but only the top 16 bits are used
   in most cases. The lower 16 bits are completely unreliable (due to the
   fact that the update is done only on the top bits), but help in the
   adaptation -- probably by removing a "threshold effect" due to
   quantization (rounding going to zero) when the gradient is small.
   
   Another kludge that seems to work good: when performing the weight
   update, we only move half the way toward the "goal" this seems to
   reduce the effect of quantization noise in the update phase. This
   can be seen as applying a gradient descent on a "soft constraint"
   instead of having a hard constraint.
   
*/

namespace Alanta.Client.Media.Dsp.Speex
{
	/// <summary>
	/// Implements the basic Speex Echo Canceller as something like a real C#-style class.
	/// </summary>
	public class SpeexEchoCanceller2 : EchoCancelFilter
	{
		#region Constructors

		public SpeexEchoCanceller2(MediaConfig config, AudioFormat recordedAudioFormat, AudioFormat playedAudioFormat, IAudioFilter playedResampler = null, IAudioFilter recordedResampler = null) :
			base(config.ExpectedAudioLatency, config.FilterLength, recordedAudioFormat, playedAudioFormat, playedResampler, recordedResampler)
		{
			SpeexInit(config, recordedAudioFormat.SamplesPerFrame, recordedAudioFormat.SamplesPerSecond);
		}

		#endregion

		#region Fields and Properties

		private SpeexPreprocessFilter preprocessFilter;

		/// <summary>
		/// Sets or gets the sampling rate of the audio streams manipulated by the echo canceling filter.
		/// </summary>
		public int SamplingRate
		{
			get
			{
				return sampling_rate;
			}
			set
			{
				var st = this;
				st.sampling_rate = value;
				st.spec_average = DIV32_16(SHL32(EXTEND32(st.SamplesPerFrame), 15), st.sampling_rate);
				st.beta0 = (2.0f * st.SamplesPerFrame) / st.sampling_rate;
				st.beta_max = (.5f * st.SamplesPerFrame) / st.sampling_rate;
				if (st.sampling_rate < 12000)
					st.notch_radius = QCONST16(.9f, 15);
				else if (st.sampling_rate < 24000)
					st.notch_radius = QCONST16(.982f, 15);
				else
					st.notch_radius = QCONST16(.992f, 15);
			}
		}

		/// <summary>
		/// Gets the size of the filter's impulse response.
		/// </summary>
		public int ImpulseResponseSize
		{
			get
			{
				return M * SamplesPerFrame;
			}
		}

		public int[] ImpulseResponse
		{
			get
			{
				var st = this;
				int M = st.M, N = st.window_size, n = st.SamplesPerFrame, i, j;
				int[] filt = new int[n];
				for (j = 0; j < M; j++)
				{
					/*FIXME: Implement this for multiple channels */
					st.fft.DoIfft(st.W, j * N, st.wtmp, 0);
					for (i = 0; i < n; i++)
					{
						filt[j * n + i] = (int)(32767 * st.wtmp[i]);
					}
				}
				return filt;
			}
		}

		private int framesCancelled;

		/// <summary>
		/// The size of the samples against which echo is detected and canceled. Also known as the size of the filter coefficients.
		/// </summary>
		private int window_size;

		private int cancel_count;
		private bool adapted;
		private bool saturated;
		private int screwed_up;

		/// <summary>
		/// Roughly, the number of audio frames against which echo is to be canceled.
		/// </summary>
		private int M;

		/// <summary>
		/// Number of input channels (microphones)
		/// </summary>
		private int C;

		/// <summary>
		/// Number of output channels (loudspeakers)
		/// </summary>
		private int K;

		private int sampling_rate;
		private float spec_average;
		private float beta0;
		private float beta_max;
		private float sum_adapt;
		private float leak_estimate;

		/// <summary>
		/// Far-end (speaker signal) input buffer (2N) (time domain)
		/// </summary>
		private float[] x;

		/// <summary>
		/// Pre-whitened version of speaker signal (time domain).
		/// </summary>
		// private float[] xf;

		/// <summary>
		/// Pre-whitening filter for the speaker signal.
		/// </summary>
		// private PreWhiteningFilter Fx;

		/// <summary>
		/// Far-end buffer (M+1 frames) (frequency domain)
		/// </summary>
		private float[] X;

		/// <summary>
		/// Error (time domain). Functions as the output signal.
		/// </summary>
		private float[] e;

		/// <summary>
		/// Pre-whitened version of error (time domain).
		/// </summary>
		/// <remarks>Added ks 11/4/10. Used to store a version of the output signal that is less correlated with itself, and hence should converge faster.</remarks>
		// private float[] ef;

		/// <summary>
		/// Pre-whitening filter for the error signal.
		/// </summary>
		// private PreWhiteningFilter Fe;

		/// <summary>
		/// DC-corrected float microphone samples (time domain)
		/// </summary>
		private float[] recorded;

		/// <summary>
		/// scratch
		/// </summary>
		private float[] y;
		private float[] last_y;

		/// <summary>
		/// Filter Response (frequency domain)
		/// </summary>
		private float[] Y;

		/// <summary>
		/// Error (frequency domain)
		/// </summary>
		private float[] E;

		/// <summary>
		/// scratch
		/// </summary>
		private float[] PHI;

		/// <summary>
		/// Background filter weights (frequency domain)
		/// </summary>
		private float[] W;

#if TWO_PATH
		/// <summary>
		/// Foreground filter weights (frequency domain)
		/// </summary>
		private float[] foreground;

		/// <summary>
		/// 1st recursive average of the residual power difference
		/// </summary>
		private float Davg1;

		/// <summary>
		/// 2nd recursive average of the residual power difference
		/// </summary>
		private float Davg2;

		/// <summary>
		/// Estimated variance of 1st estimator
		/// </summary>
		private float Dvar1;

		/// <summary>
		/// Estimated variance of 2nd estimator
		/// </summary>
		private float Dvar2;
#endif

		/// <summary>
		/// Power of the far-end signal
		/// </summary>
		private float[] power;

		/// <summary>
		/// Inverse power of far-end
		/// </summary>
		private float[] power_1;

		/// <summary>
		/// Scratch
		/// </summary>
		private float[] wtmp;

		/// <summary>
		/// Power spectrum accumulation of st.E (Error)
		/// </summary>
		private float[] Rf;

		/// <summary>
		/// Power spectrum accumulation of st.Y (Filter Response)
		/// </summary>
		private float[] Yf;

		/// <summary>
		/// Power spectrum accumulation of st.X (Echo)
		/// </summary>
		private float[] Xf;

		private float[] Eh;
		private float[] Yh;
		private float Pey;
		private float Pyy;
		private float[] window;

		/// <summary>
		/// Proportional adaptation rate
		/// </summary>
		private float[] prop;

		private SpeexFft fft;
		private float[] memX;
		private float[] memD;
		private float[] memE;
		private float preemph;
		private float notch_radius;
		private float[] notch_mem;

		#endregion

		#region Constants
		const float MIN_LEAK = .005f;
		const float VAR1_SMOOTH = .36f;
		const float VAR2_SMOOTH = .7225f;
		const float VAR1_UPDATE = .5f;
		const float VAR2_UPDATE = .25f;
		const float VAR_BACKTRACK = 4.0f;
		const int PLAYBACK_DELAY = 2;
		const int WEIGHT_SHIFT = 0;
		const float Q15ONE = 1.0f;
		const float LPC_SCALING = 1.0f;
		const float SIG_SCALING = 1.0f;
		const float LSP_SCALING = 1.0f;
		const float GAMMA_SCALING = 1.0f;
		const float GAIN_SCALING = 1.0f;
		const float GAIN_SCALING_1 = 1.0f;

		const float VERY_SMALL = 1e-15f;
		const float VERY_LARGE32 = 1e15f;
		const float VERY_LARGE16 = 1e15f;
		const float Q15_ONE = 1.0f;
		const float FLOAT_ONE = 1.0f;
		const float FLOAT_ZERO = 0.0f;

		//#define QCONST16(x,bits) (x)
		//#define QCONST32(x,bits) (x)

		//#define NEG16(x) (-(x))
		//#define NEG32(x) (-(x))

		// We should inline these eventually, but leave them here for now to get it compiling.
		// I should note that these are NOT inlined by the MSIL compiler, but I would entirely expect
		// them to get inlined by the JIT compiler (that's a little more difficult to test).
		static float NEG16(float x) { return -x; }
		static float NEG32(float x) { return -x; }
		static short NEG16(short x) { return (short)-x; }
		static int NEG32(int x) { return -x; }

		static float ADD16(float a, float b) { return a + b; }
		static float ADD32(float a, float b) { return a + b; }
		static short ADD16(short a, short b) { return (short)(a + b); }
		static int ADD32(short a, short b) { return a + b; }
		static float SUB16(float a, float b) { return a - b; }
		static float SUB32(float a, float b) { return a - b; }
		static short SUB16(short a, short b) { return (short)(a - b); }
		static int SUB32(short a, short b) { return a - b; }
		static float DIV32(float a, float b) { return a / b; }
		static float FLOAT_MUL32(float a, float b) { return a * b; }
		static float MAC16_16(float c, float a, float b) { return c + a * b; }
		static float MULT16_16(float a, float b) { return a * b; }
		static float FLOAT_AMULT(float a, float b) { return a * b; }
		static float DIV32_16(float a, float b) { return a / b; }
		static float MULT16_32_Q15(float a, float b) { return a * b; }
		static float MULT16_16_P15(float a, float b) { return a * b; }
		static float FLOAT_ADD(float a, float b) { return a + b; }
		static float FLOAT_MULT(float a, float b) { return a * b; }
		static float FLOAT_MUL32U(float a, float b) { return a * b; }
		static float FLOAT_DIVU(float a, float b) { return a / b; }
		static float FLOAT_DIV32(float a, float b) { return a / b; }
		static float FLOAT_DIV32_FLOAT(float a, float b) { return a / b; }
		static float FLOAT_SUB(float a, float b) { return a - b; }
		static float FLOAT_EXTRACT16(float a) { return a; }
		static bool FLOAT_GT(float a, float b) { return a > b; }
		static bool FLOAT_LT(float a, float b) { return a < b; }
		static float ABS32(float a) { return Math.Abs(a); }

		static float EXTRACT16(float a) { return a; }
		static float SATURATE16(float x, float a) { return x; }
		static float SATURATE32(float x, float a) { return x; }
		static float PSHR32(float a, int shift) { return a; }
		static float SHR32(float a, int shift) { return a; }
		static float SHL32(float a, int shift) { return a; }
		static float SHR16(float a, int shift) { return a; }
		static float SHL16(float a, int shift) { return a; }
		static float QCONST16(float a, int bits) { return a; }
		static float QCONST32(float a, int bits) { return a; }
		static float EXTEND32(float a) { return a; }
		static float FLOAT_SHL(float a, int b) { return a; }
		static float PSEUDOFLOAT(float a) { return a; }
		static short FLOAT_TO_SHORT(float x) { return Convert.ToInt16((x) < -32767.5f ? -32768 : ((x) > 32766.5f ? 32767 : Math.Floor(.5 + (x)))); }
		#endregion

		#region Public Methods

		/** Creates a new echo canceller state */
		private void SpeexInit(MediaConfig config, int samplesPerFrame, int samplesPerSecond)
		{
			preprocessFilter = new SpeexPreprocessFilter(samplesPerFrame, samplesPerSecond, config, this, "SpeexEchoCanceller:SpeexPreprocessFilter");

			int nb_mic = 1;
			int nb_speakers = 1;
			SamplingRate = samplesPerSecond;

			int i, N, M, C, K;
			var st = this;

			st.K = nb_speakers;
			st.C = nb_mic;
			C = st.C;
			K = st.K;
			st.window_size = 2 * SamplesPerFrame;
			N = st.window_size;
			M = st.M = (FilterLength + st.SamplesPerFrame - 1) / SamplesPerFrame;
			st.cancel_count = 0;
			st.sum_adapt = 0;
			st.saturated = false;
			st.screwed_up = 0;

			/* This is the default sampling rate */
			st.sampling_rate = 8000;
			st.spec_average = DIV32_16(SHL32(EXTEND32(st.SamplesPerFrame), 15), st.sampling_rate);
			st.beta0 = (2.0f * st.SamplesPerFrame) / st.sampling_rate;
			st.beta_max = (.5f * st.SamplesPerFrame) / st.sampling_rate;
			st.leak_estimate = 0;

			st.fft = new SpeexFft(N); // SpeexFft.spx_fft_init(N);

			st.e = new float[C * N]; // (float*)speex_alloc(C*N*sizeof(float));
			st.x = new float[K * N]; //  (float*)speex_alloc(K * N * sizeof(float));
			st.recorded = new float[C * st.SamplesPerFrame]; // (float*)speex_alloc(C * st.SamplesPerFrame * sizeof(float));
			st.y = new float[C * N]; // (float*)speex_alloc(C * N * sizeof(float));
			st.last_y = new float[C * N]; // (float*)speex_alloc(C * N * sizeof(float));
			st.Yf = new float[st.SamplesPerFrame + 1]; // (float*)speex_alloc((st.SamplesPerFrame + 1) * sizeof(float));
			st.Rf = new float[st.SamplesPerFrame + 1]; // (float*)speex_alloc((st.SamplesPerFrame + 1) * sizeof(float));
			st.Xf = new float[st.SamplesPerFrame + 1]; // (float*)speex_alloc((st.SamplesPerFrame + 1) * sizeof(float));
			st.Yh = new float[st.SamplesPerFrame + 1]; // (float*)speex_alloc((st.SamplesPerFrame + 1) * sizeof(float));
			st.Eh = new float[st.SamplesPerFrame + 1]; // (float*)speex_alloc((st.SamplesPerFrame + 1) * sizeof(float));

			st.X = new float[K * (M + 1) * N]; // (float*)speex_alloc(K*(M+1)*N*sizeof(float));
			st.Y = new float[C * N]; // (float*)speex_alloc(C * N * sizeof(float));
			st.E = new float[C * N]; // (float*)speex_alloc(C * N * sizeof(float));
			st.W = new float[C * K * M * N]; // (float*)speex_alloc(C * K * M * N * sizeof(float));
#if TWO_PATH
			st.foreground = new float[M * N * C * K]; // (float*)speex_alloc(M * N * C * K * sizeof(float));
#endif
			st.PHI = new float[N]; // (float*)speex_alloc(N * sizeof(float));
			st.power = new float[SamplesPerFrame + 1]; // (float*)speex_alloc((frame_size + 1) * sizeof(float));
			st.power_1 = new float[SamplesPerFrame + 1]; // (float*)speex_alloc((frame_size + 1) * sizeof(float));
			st.window = new float[N]; // (float*)speex_alloc(N * sizeof(float));
			st.prop = new float[M]; // (float*)speex_alloc(M * sizeof(float));
			st.wtmp = new float[N]; // (float*)speex_alloc(N * sizeof(float));
			for (i = 0; i < N; i++)
				st.window[i] = (float)(.5 - .5 * Math.Cos(2 * Math.PI * i / N));
			for (i = 0; i <= st.SamplesPerFrame; i++)
				st.power_1[i] = FLOAT_ONE;

			float sum = 0;
			/* Ratio of ~10 between adaptation rate of first and last block */
			float decay = SHR32((float)(Math.Exp(NEG16(DIV32_16(QCONST16(2.4f, 11), M)))), 1);
			st.prop[0] = QCONST16(.7f, 15);
			sum = EXTEND32(st.prop[0]);
			for (i = 1; i < M; i++)
			{
				st.prop[i] = MULT16_16(st.prop[i - 1], decay);
				sum = ADD32(sum, EXTEND32(st.prop[i]));
			}
			for (i = M - 1; i >= 0; i--)
			{
				st.prop[i] = DIV32(MULT16_16(QCONST16(.8f, 15), st.prop[i]), sum);
			}

			st.memX = new float[K]; // (float*)speex_alloc(K * sizeof(float));
			st.memD = new float[C]; // (float*)speex_alloc(C * sizeof(float));
			st.memE = new float[C]; // (float*)speex_alloc(C * sizeof(float));
			st.preemph = QCONST16(.9f, 15);
			if (st.sampling_rate < 12000)
				st.notch_radius = QCONST16(.9f, 15);
			else if (st.sampling_rate < 24000)
				st.notch_radius = QCONST16(.982f, 15);
			else
				st.notch_radius = QCONST16(.992f, 15);

			st.notch_mem = new float[2 * C]; // (float*)speex_alloc(2 * C * sizeof(float));
			st.adapted = false;
			st.Pey = st.Pyy = FLOAT_ONE;

#if TWO_PATH
			st.Davg1 = st.Davg2 = 0;
			st.Dvar1 = st.Dvar2 = FLOAT_ZERO;
#endif

			//st.play_buf = new short[K * (PLAYBACK_DELAY + 1) * st.frameSize]; // (short*)speex_alloc(K*(PLAYBACK_DELAY+1)*st.frameSize*sizeof(short));
			//st.play_buf_pos = PLAYBACK_DELAY * st.frameSize;
			//st.play_buf_started = false;
		}

		/// <summary>
		/// Performs echo cancellation on a frame
		/// </summary>
		/// <param name="st">The SpeexEchoState represensenting the current echo canceller state</param>
		/// <param name="recorded">A short[] array of recorded microphone samples</param>
		/// <param name="played">A short[] array of the most recently played samples</param>
		/// <param name="outFrame">A short[] array into which the echo cancelled sound will be copied</param>
		protected override void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame)
		{
			#region Setup and housekeeping

			// Debug.WriteLine("Processing frame={0}", st.framesCancelled);
			var st = this;
			int i, j, chan, speak;
			int N, M, C, K;

			// Syy = Sum of the dot-products of the y vector (y = recorded sound)
			// See = Sum of the dot-products of the e vector (e = output sound, aka error)
			// Sxx = Sum of the dot-products of the x vector (x = played sound)
			// Sdd = Sum of the dot-products of the d vector (d = recorded sound)
			// Sff = Sum of the dot-products of the f vector (f = foreground filter)
			float Syy, See, Sxx, Sdd, Sff;
#if TWO_PATH
			float Dbf;
			bool update_foreground;
#endif
			// Sey = Sum of the dot products of the e and y vectors (e = output sound, y = recorded sound)
			float Sey;

			float ss, ss_1;
			float Pey = FLOAT_ONE, Pyy = FLOAT_ONE;
			float alpha, alpha_1;
			float RER; // Residual-to-Error Ratio
			// float tmp32;

			N = st.window_size;
			M = st.M;
			C = st.C;
			K = st.K;

			st.cancel_count++;
			ss = (float).35 / M;
			ss_1 = 1 - ss;

			/* Convert data recorded via microphone to floats, normalizing and applying pre-emphasis */
			for (chan = 0; chan < C; chan++)
			{
				/* Apply a notch filter to make sure DC doesn't end up causing problems */
				filter_dc_notch16(recorded, chan, st.notch_radius, st.recorded, chan * st.SamplesPerFrame, st.SamplesPerFrame, st.notch_mem, 2 * chan, C);

				for (i = 0; i < st.SamplesPerFrame; i++)
				{
					float tmp32 = st.recorded[chan * st.SamplesPerFrame + i] - (st.preemph * st.memD[chan]);
					st.memD[chan] = st.recorded[chan * st.SamplesPerFrame + i];
					st.recorded[chan * st.SamplesPerFrame + i] = tmp32;
				}
			}

			// Convert data submitted to speaker to floats, normalizing and applying pre-emphasis
			for (speak = 0; speak < K; speak++)
			{
				for (i = 0; i < st.SamplesPerFrame; i++)
				{
					st.x[speak * N + i] = st.x[speak * N + i + st.SamplesPerFrame];
					float tmp32 = SUB32(EXTEND32(played[i * K + speak]), EXTEND32(MULT16_16_P15(st.preemph, st.memX[speak])));
					st.x[speak * N + i + st.SamplesPerFrame] = EXTRACT16(tmp32);
					st.memX[speak] = played[i * K + speak];
				}
			}

			for (speak = 0; speak < K; speak++)
			{
				// Move the data in the frequency domain echo cancel buffer to the beginning, to leave room for the new stuff.
				for (j = M - 1; j >= 0; j--)
				{
					for (i = 0; i < N; i++)
					{
						st.X[(j + 1) * N * K + speak * N + i] = st.X[j * N * K + speak * N + i];
					}
				}

				/* Convert x (echo input) to frequency domain and copy it to the frequency domain echo cancel buffer */
				// Original line: spx_fft(st->fft_table, st->x+speak*N, &st->X[speak*N]); 
				// So far as I can tell, no difference between st->x+speak*N vs. &st-X[speak*N].
				st.fft.DoFft(st.x, speak * N, st.X, speak * N);
			}
			#endregion

			// Compute the inner (dot) product of the data submitted to the speaker.
			Sxx = 0;
			for (speak = 0; speak < K; speak++)
			{
				Sxx += mdf_inner_prod(st.x, speak * N + st.SamplesPerFrame, st.x, speak * N + st.SamplesPerFrame, st.SamplesPerFrame);
				power_spectrum_accum(st.X, speak * N, st.Xf, 0, N);
			}

			// ks 10/12/10 - Sff is a measure of what remains after you've done echo cancellation via the coefficients (tap weights) stored in st.e.
			Sff = 0;
			for (chan = 0; chan < C; chan++)
			{
#if TWO_PATH
				/* Compute foreground filter */
				spectral_mul_accum(st.X, 0, st.foreground, chan * N * K * M, st.Y, chan * N, N, M * K);
				st.fft.DoIfft(st.Y, chan * N, st.e, chan * N);
				for (i = 0; i < st.SamplesPerFrame; i++)
				{
					st.e[chan * N + i] = st.recorded[chan * st.SamplesPerFrame + i] - st.e[chan * N + i + st.SamplesPerFrame];
				}
				Sff += mdf_inner_prod(st.e, chan * N, st.e, chan * N, st.SamplesPerFrame);
#endif
			}

			// Debug.WriteLine("st.e={0},{1},{2},{3}", st.e[0], st.e[1], st.e[2], st.e[3]);

			// st.Logger.LogSff(Sff);

			/* Adjust proportional adaption rate */
			/* FIXME: Adjust that for C, K*/
			if (st.adapted)
			{
				mdf_adjust_prop(st.W, N, M, C * K, st.prop);
			}

			/* Compute weight gradient */
			if (!st.saturated)
			{
				for (chan = 0; chan < C; chan++)
				{
					for (speak = 0; speak < K; speak++)
					{
						for (j = M - 1; j >= 0; j--)
						{
							weighted_spectral_mul_conj(st.power_1, 0, st.prop[j], st.X, (j + 1) * N * K + speak * N, st.E, chan * N, st.PHI, 0, N);
							for (i = 0; i < N; i++)
							{
								st.W[chan * N * K * M + j * N * K + speak * N + i] += st.PHI[i];
							}
						}
					}
				}
			}
			else
			{
				st.saturated = false;
			}

			/* FIXME: MC conversion required */
			/* Update weight to prevent circular convolution (MDF / AUMDF) */
			for (chan = 0; chan < C; chan++)
			{
				for (speak = 0; speak < K; speak++)
				{
					for (j = 0; j < M; j++)
					{
						/* This is a variant of the Alternatively Updated MDF (AUMDF) */
						/* Remove the "if" to make this an MDF filter */
						if (j == 0 || st.cancel_count % (M - 1) == j - 1)
						{
							st.fft.DoIfft(st.W, chan * N * K * M + j * N * K + speak * N, st.wtmp, 0);
							Array.Clear(st.wtmp, st.SamplesPerFrame, N - st.SamplesPerFrame);
							st.fft.DoFft(st.wtmp, 0, st.W, chan * N * K * M + j * N * K + speak * N);
						}
					}
				}
			}

			/* So we can use power_spectrum_accum */
			for (i = 0; i <= st.SamplesPerFrame; i++)
			{
				st.Rf[i] = st.Yf[i] = st.Xf[i] = 0;
			}

			Dbf = 0;
			See = 0;
#if TWO_PATH
			/* Difference in response, this is used to estimate the variance of our residual power estimate */
			for (chan = 0; chan < C; chan++)
			{
				spectral_mul_accum(st.X, 0, st.W, chan * N * K * M, st.Y, chan * N, N, M * K);
				st.fft.DoIfft(st.Y, chan * N, st.y, chan * N);
				for (i = 0; i < st.SamplesPerFrame; i++)
				{
					st.e[chan * N + i] = SUB16(st.e[chan * N + i + st.SamplesPerFrame], st.y[chan * N + i + st.SamplesPerFrame]);
				}
				Dbf += 10 + mdf_inner_prod(st.e, chan * N, st.e, chan * N, st.SamplesPerFrame);
				for (i = 0; i < st.SamplesPerFrame; i++)
				{
					st.e[chan * N + i] = st.recorded[chan * st.SamplesPerFrame + i] - st.y[chan * N + i + st.SamplesPerFrame];
				}
				See += mdf_inner_prod(st.e, chan * N, st.e, chan * N, st.SamplesPerFrame);
			}
#endif

#if !TWO_PATH
			Sff = See;
#endif

#if TWO_PATH
			/* Logic for updating the foreground filter */

			/* For two time windows, compute the mean of the energy difference, as well as the variance */
			st.Davg1 = ADD32(MULT16_32_Q15(QCONST16(.6f, 15), st.Davg1), MULT16_32_Q15(QCONST16(.4f, 15), SUB32(Sff, See)));
			st.Davg2 = ADD32(MULT16_32_Q15(QCONST16(.85f, 15), st.Davg2), MULT16_32_Q15(QCONST16(.15f, 15), SUB32(Sff, See)));
			st.Dvar1 = FLOAT_ADD(FLOAT_MULT(VAR1_SMOOTH, st.Dvar1), FLOAT_MUL32U(MULT16_32_Q15(QCONST16(.4f, 15), Sff), MULT16_32_Q15(QCONST16(.4f, 15), Dbf)));
			st.Dvar2 = FLOAT_ADD(FLOAT_MULT(VAR2_SMOOTH, st.Dvar2), FLOAT_MUL32U(MULT16_32_Q15(QCONST16(.15f, 15), Sff), MULT16_32_Q15(QCONST16(.15f, 15), Dbf)));

			/* Equivalent float code:
			st.Davg1 = .6*st.Davg1 + .4*(Sff-See);
			st.Davg2 = .85*st.Davg2 + .15*(Sff-See);
			st.Dvar1 = .36*st.Dvar1 + .16*Sff*Dbf;
			st.Dvar2 = .7225*st.Dvar2 + .0225*Sff*Dbf;
			*/

			update_foreground = false;
			/* Check if we have a statistically significant reduction in the residual echo */
			/* Note that this is *not* Gaussian, so we need to be careful about the longer tail */
			if ((Sff - See) * Math.Abs(Sff - See) > Sff * Dbf)
				update_foreground = true;
			else if (st.Davg1 * Math.Abs(st.Davg1) > VAR1_UPDATE * st.Dvar1)
				update_foreground = true;
			else if (st.Davg2 * Math.Abs(st.Davg2) > VAR2_UPDATE * st.Dvar2)
				update_foreground = true;

			/* Do we update? */
			if (update_foreground)
			{
				st.Davg1 = st.Davg2 = 0;
				st.Dvar1 = st.Dvar2 = FLOAT_ZERO;
				/* Copy background filter to foreground filter */
				for (i = 0; i < N * M * C * K; i++)
					st.foreground[i] = st.W[i];
				/* Apply a smooth transition so as to not introduce blocking artifacts */
				for (chan = 0; chan < C; chan++)
					for (i = 0; i < st.SamplesPerFrame; i++)
						st.e[chan * N + i + st.SamplesPerFrame] = MULT16_16(st.window[i + st.SamplesPerFrame], st.e[chan * N + i + st.SamplesPerFrame]) + MULT16_16(st.window[i], st.y[chan * N + i + st.SamplesPerFrame]);
			}
			else
			{
				bool reset_background = false;
				/* Otherwise, check if the background filter is significantly worse */
				if (FLOAT_GT(FLOAT_MUL32U(NEG32(SUB32(Sff, See)), ABS32(SUB32(Sff, See))), FLOAT_MULT(VAR_BACKTRACK, FLOAT_MUL32U(Sff, Dbf))))
					reset_background = true;
				if (FLOAT_GT(FLOAT_MUL32U(NEG32(st.Davg1), ABS32(st.Davg1)), FLOAT_MULT(VAR_BACKTRACK, st.Dvar1)))
					reset_background = true;
				if (FLOAT_GT(FLOAT_MUL32U(NEG32(st.Davg2), ABS32(st.Davg2)), FLOAT_MULT(VAR_BACKTRACK, st.Dvar2)))
					reset_background = true;
				if (reset_background)
				{
					/* Copy foreground filter to background filter */
					//for (i = 0; i < N * M * C * K; i++)
					//    st.W[i] = SHL32(EXTEND32(st.foreground[i]), 16);
					Buffer.BlockCopy(st.foreground, 0, st.W, 0, N * M * C * K);
					/* We also need to copy the output so as to get correct adaptation */
					for (chan = 0; chan < C; chan++)
					{
						//for (i = 0; i < st.SamplesPerFrame; i++)
						//    st.y[chan * N + i + st.SamplesPerFrame] = st.e[chan * N + i + st.SamplesPerFrame];
						Buffer.BlockCopy(st.e, chan * N + st.SamplesPerFrame, st.y, chan * N + st.SamplesPerFrame, st.SamplesPerFrame);
						for (i = 0; i < st.SamplesPerFrame; i++)
							st.e[chan * N + i] = st.recorded[chan * st.SamplesPerFrame + i] - st.y[chan * N + i + st.SamplesPerFrame];
					}
					See = Sff;
					st.Davg1 = st.Davg2 = 0;
					st.Dvar1 = st.Dvar2 = FLOAT_ZERO;
				}
			}
#endif

			// Subtract the modeled echo path from the microphone signal.
			Sey = Syy = Sdd = 0;
			for (chan = 0; chan < C; chan++)
			{
				/* Compute error signal (for the output with de-emphasis) */
				for (i = 0; i < st.SamplesPerFrame; i++)
				{
					float tmp_out;
#if TWO_PATH
					tmp_out = st.recorded[chan * st.SamplesPerFrame + i] - st.e[chan * N + i + st.SamplesPerFrame];
#else
					tmp_out = st.input[chan*st.frameSize+i] - st.y[chan*N+i+st.frameSize];
#endif
					tmp_out += st.preemph * st.memE[chan];
					/* This is an arbitrary test for saturation in the microphone signal */
					if (recorded[i * C + chan] <= -32000 || recorded[i * C + chan] >= 32000)
					{
						if (!st.saturated)
							st.saturated = true;
					}
					outFrame[i * C + chan] = FLOAT_TO_SHORT(tmp_out);
					st.memE[chan] = tmp_out;
				}

#if DUMP_ECHO_CANCEL_DATA
				dump_audio(in, far_end, out, st.frameSize);
#endif

				/* Compute error signal (filter update version) */
				for (i = 0; i < st.SamplesPerFrame; i++)
				{
					st.e[chan * N + i + st.SamplesPerFrame] = st.e[chan * N + i];
					st.e[chan * N + i] = 0;
				}

				/* Compute a bunch of correlations */
				/* FIXME: bad merge */
				Sey += mdf_inner_prod(st.e, chan * N + st.SamplesPerFrame, st.y, chan * N + st.SamplesPerFrame, st.SamplesPerFrame);
				Syy += mdf_inner_prod(st.y, chan * N + st.SamplesPerFrame, st.y, chan * N + st.SamplesPerFrame, st.SamplesPerFrame);
				Sdd += mdf_inner_prod(st.recorded, chan * st.SamplesPerFrame, st.recorded, chan * st.SamplesPerFrame, st.SamplesPerFrame);

				/* Convert error to frequency domain */
				st.fft.DoFft(st.e, chan * N, st.E, chan * N);
				Array.Clear(st.y, chan * N, st.SamplesPerFrame);
				//for (i = 0; i < st.SamplesPerFrame; i++)
				//    st.y[i + chan * N] = 0;
				st.fft.DoFft(st.y, chan * N, st.Y, chan * N);

				/* Compute power spectrum of echo (X), error (E) and filter response (Y) */
				power_spectrum_accum(st.E, chan * N, st.Rf, 0, N);
				power_spectrum_accum(st.Y, chan * N, st.Yf, 0, N);
			}

			// Debug.WriteLine("st.E={0},{1},{2},{3}", st.E[0], st.E[1], st.E[2], st.E[3]);

			/*printf ("%f %f %f %f\n", Sff, See, Syy, Sdd, st.update_cond);*/

			/* Do some sanity checks */
			if (!(Syy >= 0 && Sxx >= 0 && See >= 0) || !(Sff < N * 1e9 && Syy < N * 1e9 && Sxx < N * 1e9))
			{
				/* Things have gone really bad */
				st.screwed_up += 50;
				//for (i = 0; i < st.SamplesPerFrame * C; i++)
				//    output[i] = 0;
				Array.Clear(outFrame, 0, st.SamplesPerFrame);
			}
			else if (Sff > Sdd + N * 10000) // (SHR32(Sff, 2) > ADD32(Sdd, SHR32(MULT16_16(N, 10000), 6)))
			{
				/* AEC seems to add lots of echo instead of removing it, let's see if it will improve */
				st.screwed_up++;
			}
			else
			{
				/* Everything's fine */
				st.screwed_up = 0;
			}
			if (st.screwed_up >= 50)
			{
				Debug.WriteLine("The echo canceller started acting funny and got slapped (reset). It swears it will behave now.");
				ResetEchoState();
				return;
			}

			/* Add a small noise floor to make sure not to have problems when dividing */
			See = Math.Max(See, N * 100);

			for (speak = 0; speak < K; speak++)
			{
				Sxx += mdf_inner_prod(st.x, speak * N + st.SamplesPerFrame, st.x, speak * N + st.SamplesPerFrame, st.SamplesPerFrame);
				power_spectrum_accum(st.X, speak * N, st.Xf, 0, N);
			}

			/* Smooth far end energy estimate over time */
			for (j = 0; j <= st.SamplesPerFrame; j++)
				st.power[j] = MULT16_32_Q15(ss_1, st.power[j]) + 1 + MULT16_32_Q15(ss, st.Xf[j]);

			/* Compute filtered spectra and (cross-)correlations */
			for (j = st.SamplesPerFrame; j >= 0; j--)
			{
				float Eh, Yh;
				Eh = PSEUDOFLOAT(st.Rf[j] - st.Eh[j]);
				Yh = PSEUDOFLOAT(st.Yf[j] - st.Yh[j]);
				Pey = FLOAT_ADD(Pey, FLOAT_MULT(Eh, Yh));
				Pyy = FLOAT_ADD(Pyy, FLOAT_MULT(Yh, Yh));
				st.Eh[j] = (1 - st.spec_average) * st.Eh[j] + st.spec_average * st.Rf[j];
				st.Yh[j] = (1 - st.spec_average) * st.Yh[j] + st.spec_average * st.Yf[j];
			}

			Pyy = (float)Math.Sqrt(Pyy);
			Pey = FLOAT_DIVU(Pey, Pyy);

			/* Compute correlation updatete rate */
			float tmp = MULT16_32_Q15(st.beta0, Syy);
			if (tmp > MULT16_32_Q15(st.beta_max, See))
				tmp = MULT16_32_Q15(st.beta_max, See);
			alpha = FLOAT_DIV32(tmp, See);
			alpha_1 = FLOAT_SUB(FLOAT_ONE, alpha);
			/* Update correlations (recursive average) */
			st.Pey = FLOAT_ADD(FLOAT_MULT(alpha_1, st.Pey), FLOAT_MULT(alpha, Pey));
			st.Pyy = FLOAT_ADD(FLOAT_MULT(alpha_1, st.Pyy), FLOAT_MULT(alpha, Pyy));
			if (FLOAT_LT(st.Pyy, FLOAT_ONE))
				st.Pyy = FLOAT_ONE;
			/* We don't really hope to get better than 33 dB (MIN_LEAK-3dB) attenuation anyway */
			if (FLOAT_LT(st.Pey, FLOAT_MULT(MIN_LEAK, st.Pyy)))
				st.Pey = FLOAT_MULT(MIN_LEAK, st.Pyy);
			if (FLOAT_GT(st.Pey, st.Pyy))
				st.Pey = st.Pyy;
			/* leak_estimate is the linear regression result */
			st.leak_estimate = FLOAT_EXTRACT16(FLOAT_SHL(FLOAT_DIVU(st.Pey, st.Pyy), 14));
			/* This looks like a stupid bug, but it's right (because we convert from Q14 to Q15) */
			if (st.leak_estimate > 16383)
				st.leak_estimate = 32767;
			else
				st.leak_estimate = SHL16(st.leak_estimate, 1);
			/*printf ("%f\n", st.leak_estimate);*/

			/* Compute Residual to Error Ratio */
			RER = (.0001f * Sxx + 3.0f * MULT16_32_Q15(st.leak_estimate, Syy)) / See;
			/* Check for y in e (lower bound on RER) */
			if (RER < Sey * Sey / (1 + See * Syy))
				RER = Sey * Sey / (1 + See * Syy);
			if (RER > .5f)
				RER = .5f;

			/* We consider that the filter has had minimal adaptation if the following is true*/
			if (!st.adapted && st.sum_adapt > M && st.leak_estimate * Syy > .03f * Syy)
			{
				st.adapted = true;
			}

			if (st.adapted)
			{
				/* Normal learning rate calculation once we're past the minimal adaptation phase */
				for (i = 0; i <= st.SamplesPerFrame; i++)
				{
					float r, e;
					/* Compute frequency-domain adaptation mask */
					r = MULT16_32_Q15(st.leak_estimate, SHL32(st.Yf[i], 3));
					e = SHL32(st.Rf[i], 3) + 1;
					if (r > .5f * e)
						r = .5f * e;
					r = MULT16_32_Q15(QCONST16(.7f, 15), r) + MULT16_32_Q15(QCONST16(.3f, 15), (float)(MULT16_32_Q15(RER, e)));
					/*st.power_1[i] = adapt_rate*r/(e*(1+st.power[i]));*/
					st.power_1[i] = FLOAT_SHL(FLOAT_DIV32_FLOAT(r, FLOAT_MUL32U(e, st.power[i] + 10)), WEIGHT_SHIFT + 16);
				}
			}
			else
			{
				/* Temporary adaption rate if filter is not yet adapted enough */
				float adapt_rate = 0;

				if (Sxx > N * 1000)
				{
					float tmp2 = .25f * Sxx;
					if (tmp2 > .25f * See)
						tmp2 = .25f * See;
					adapt_rate = tmp2 / See;
				}
				for (i = 0; i <= st.SamplesPerFrame; i++)
				{
					st.power_1[i] = adapt_rate / (st.power[i] + 10);
				}

				/* How much have we adapted so far? */
				st.sum_adapt += adapt_rate;
			}

			/* FIXME: MC conversion required */
			for (i = 0; i < st.SamplesPerFrame; i++)
				st.last_y[i] = st.last_y[st.SamplesPerFrame + i];
			if (st.adapted)
			{
				/* If the filter is adapted, take the filtered echo */
				for (i = 0; i < st.SamplesPerFrame; i++)
					st.last_y[st.SamplesPerFrame + i] = recorded[i] - outFrame[i];
			}
			else
			{
				/* If filter isn't adapted yet, all we can do is take the far end signal directly */
				/* moved earlier: for (i=0;i<N;i++)
				st.last_y[i] = st.x[i];*/
			}

			st.framesCancelled++;
			// Debug.WriteLine("");
		}

		public override bool Read(Array outBuffer, out bool moreFrames)
		{
			bool success = base.Read(outBuffer, out moreFrames);
			if (success)
			{
				preprocessFilter.Filter((short[])outBuffer);
			}
			return success;
		}

		/// <summary>
		/// Compute spectrum of estimated echo for use in an echo post-filter
		/// </summary>
		/// <param name="residual_echo">Buffer into which the residual echo calculation should be copied.</param>
		public void GetResidual(float[] residual_echo)
		{
			var st = this;
			int i;
			float leak2;
			int N;

			N = st.window_size;

			/* Apply hanning window (should pre-compute it)*/
			for (i = 0; i < N; i++)
				st.y[i] = MULT16_16(st.window[i], st.last_y[i]);

			/* Compute power spectrum of the echo */
			st.fft.DoFft(st.y, 0, st.Y, 0);
			power_spectrum(st.Y, residual_echo, N);

			if (st.leak_estimate > .5)
				leak2 = 1;
			else
				leak2 = 2 * st.leak_estimate;

			/* Estimate residual echo */
			for (i = 0; i <= st.SamplesPerFrame; i++)
				residual_echo[i] = (int)MULT16_32_Q15(leak2, residual_echo[i]);
		}

		#endregion

		#region Private Methods
		/// <summary>
		/// Resets echo canceller state
		/// </summary>
		private void ResetEchoState()
		{
			var st = this;
			int i, M, N, C, K;
			st.cancel_count = 0;
			st.screwed_up = 0;
			N = st.window_size;
			M = st.M;
			C = st.C;
			K = st.K;
			Array.Clear(st.W, 0, st.W.Length);
#if TWO_PATH
			Array.Clear(st.foreground, 0, st.foreground.Length);
#endif
			Array.Clear(st.X, 0, st.X.Length);
			Array.Clear(st.power, 0, st.power.Length);
			Array.Clear(st.Eh, 0, st.Eh.Length);
			Array.Clear(st.Yh, 0, st.Yh.Length);
			for (i = 0; i <= st.SamplesPerFrame; i++)
			{
				st.power_1[i] = FLOAT_ONE;
			}

			Array.Clear(st.last_y, 0, st.last_y.Length);
			Array.Clear(st.E, 0, st.E.Length);
			Array.Clear(st.x, 0, st.x.Length);
			Array.Clear(st.notch_mem, 0, st.notch_mem.Length);
			Array.Clear(st.memD, 0, st.memD.Length);
			Array.Clear(st.memE, 0, st.memE.Length);
			Array.Clear(st.memX, 0, st.memX.Length);

			st.saturated = false;
			st.adapted = false;
			st.sum_adapt = 0;
			st.Pey = st.Pyy = FLOAT_ONE;
#if TWO_PATH
			st.Davg1 = st.Davg2 = 0;
			st.Dvar1 = st.Dvar2 = FLOAT_ZERO;
#endif
			//Array.Clear(st.play_buf, 0, st.play_buf.Length);
			//st.play_buf_pos = PLAYBACK_DELAY * st.frameSize;
			//st.play_buf_started = false;
		}

		private static void filter_dc_notch16(short[] inSamples, int inoffset, float radius, float[] outSamples, int outoffset, int len, float[] mem, int memoffset, int stride)
		{
			int i;
			float den2;
			den2 = radius * radius + .7f * (1 - radius) * (1 - radius);
			/*printf ("%d %d %d %d %d %d\n", num[0], num[1], num[2], den[0], den[1], den[2]);*/
			for (i = 0; i < len; i++)
			{
				float vin = inSamples[inoffset + i * stride];
				float vout = mem[memoffset + 0] + vin;
				mem[memoffset + 0] = mem[memoffset + 1] + 2 * (-vin + radius * vout);
				mem[memoffset + 1] = vin - (den2 * vout);
				outSamples[outoffset + i] = (radius * vout);
			}
		}

		/// <summary>
		/// Computer the inner product of two arrays.
		/// </summary>
		private static float mdf_inner_prod(float[] x, int xoffset, float[] y, int yoffset, int len)
		{
			float sum = 0;
			len >>= 1; // Fast way of dividing len by 2.
			while (len-- > 0)
			{
				sum += x[xoffset++] * y[yoffset++]; // MAC16_16(part,*x++,*y++);
				sum += x[xoffset++] * y[yoffset++]; // MAC16_16(part,*x++,*y++);
			}
			return sum;
		}

		/// <summary>
		/// Compute power spectrum of a half-complex (packed) vector
		/// </summary>
		private static void power_spectrum(float[] X, float[] ps, int N)
		{
			int i, j;
			ps[0] = X[0] * X[0]; // MULT16_16(X[0],X[0]);
			for (i = 1, j = 1; i < N - 1; i += 2, j++)
			{
				ps[j] = X[i] * X[i] + X[i + 1] * X[i + 1]; //  MULT16_16(X[i],X[i]) + MULT16_16(X[i+1],X[i+1]);
			}
			ps[j] = X[i] * X[i]; // MULT16_16(X[i],X[i]);
		}

		/// <summary>
		/// Compute power spectrum of a half-complex (packed) vector and accumulate
		/// </summary>
		private static void power_spectrum_accum(float[] x, int xoffset, float[] ps, int psoffset, int N)
		{
			int i, j;
			ps[psoffset] += x[xoffset] * x[xoffset]; // MULT16_16(X[0],X[0]);
			for (i = 1, j = 1; i < N - 1; i += 2, j++)
			{
				ps[psoffset + j] += x[xoffset + i] * x[xoffset + i] + x[xoffset + i + 1] * x[xoffset + i + 1]; // MULT16_16(X[i],X[i]) + MULT16_16(X[i+1],X[i+1]);
			}
			ps[psoffset + j] += x[xoffset + i] * x[xoffset + i]; // MULT16_16(X[i],X[i]);

		}

		/// <summary>
		/// Compute cross-power spectrum of a half-complex (packed) vectors and add to acc
		/// </summary>
		private static void spectral_mul_accum(float[] X, int xoffset, float[] Y, int yoffset, float[] acc, int accoffset, int N, int M)
		{
			int i, j;
			for (i = 0; i < N; i++)
				acc[i] = 0;
			for (j = 0; j < M; j++)
			{
				acc[0] += X[xoffset + 0] * Y[yoffset + 0];
				for (i = 1; i < N - 1; i += 2)
				{
					acc[i] += (X[xoffset + i] * Y[yoffset + i] - X[xoffset + i + 1] * Y[yoffset + i + 1]);
					acc[i + 1] += (X[xoffset + i + 1] * Y[yoffset + i] + X[xoffset + i] * Y[yoffset + i + 1]);
				}
				acc[i] += X[xoffset + i] * Y[yoffset + i];

				xoffset += N;
				yoffset += N;
			}
		}

		/// <summary>
		/// Compute weighted cross-power spectrum of a half-complex (packed) vector with conjugate
		/// </summary>
		static void weighted_spectral_mul_conj(float[] w, int woffset, float p, float[] X, int xoffset, float[] Y, int yoffset, float[] prod, int prodoffset, int N)
		{
			int i, j;
			float W;
			W = p * w[woffset]; // FLOAT_AMULT(p, w[0]);
			prod[prodoffset + 0] = W * X[xoffset + 0] * Y[yoffset + 0]; // FLOAT_MUL32(W,MULT16_16(X[xoffset+0],Y[yoffset+0]));
			for (i = 1, j = 1; i < N - 1; i += 2, j++)
			{
				W = p * w[woffset + j]; // FLOAT_AMULT(p, w[j]);
				prod[prodoffset + i] = FLOAT_MUL32(W, MAC16_16(MULT16_16(X[xoffset + i], Y[yoffset + i]), X[xoffset + i + 1], Y[yoffset + i + 1]));
				prod[prodoffset + i + 1] = FLOAT_MUL32(W, MAC16_16(MULT16_16(-X[xoffset + i + 1], Y[yoffset + i]), X[xoffset + i], Y[yoffset + i + 1]));
			}
			W = FLOAT_AMULT(p, w[woffset + j]);
			prod[prodoffset + i] = FLOAT_MUL32(W, MULT16_16(X[xoffset + i], Y[yoffset + i]));
		}

		static void mdf_adjust_prop(float[] W, int N, int M, int P, float[] prop)
		{
			int i, j, p;
			float max_sum = 1;
			float prop_sum = 1;
			for (i = 0; i < M; i++)
			{
				float tmp = 1.0f;
				for (p = 0; p < P; p++)
				{
					for (j = 0; j < N; j++)
					{
						int x = p * N * M + i * N + j;
						tmp += W[x] * W[x];
					}
				}
				prop[i] = (float)Math.Sqrt(tmp);
				if (prop[i] > max_sum)
				{
					max_sum = prop[i];
				}
			}
			for (i = 0; i < M; i++)
			{
				prop[i] += .1f * max_sum;
				prop_sum += prop[i];
			}
			for (i = 0; i < M; i++)
			{
				prop[i] = .99f * prop[i] / prop_sum;
			}
		}
		#endregion

	}
}
