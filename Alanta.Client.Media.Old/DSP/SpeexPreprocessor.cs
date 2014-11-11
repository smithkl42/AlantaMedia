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


namespace Alanta.Client.Media.Dsp
{
    /** Speex pre-processor state. */
    public class SpeexPreprocessState
    {
        /* Basic info */
        public int frame_size;        /**< Number of samples processed each time */
        public int ps_size;           /**< Number of points in the power spectrum */
        public int sampling_rate;     /**< Sampling rate of the input/output */
        public int nbands;
        public SpeexFilterBank bank;

        /* Parameters */
        public int denoise_enabled;
        public int vad_enabled;
        public int dereverb_enabled;
        public float reverb_decay;
        public float reverb_level;
        public float speech_prob_start;
        public float speech_prob_continue;
        public int noise_suppress;
        public int echo_suppress;
        public int echo_suppress_active;
        public SpeexEchoState echo_state;

        public float speech_prob;  /**< Probability last frame was speech */

        /* DSP-related arrays */
        public float[] frame;      /**< Processing frame (2*ps_size) */
        public float[] ft;         /**< Processing frame in freq domain (2*ps_size) */
        public float[] ps;         /**< Current power spectrum */
        public float[] gain2;      /**< Adjusted gains */
        public float[] gain_floor; /**< Minimum gain allowed */
        public float[] window;     /**< Analysis/Synthesis window */
        public float[] noise;      /**< Noise estimate */
        public float[] reverb_estimate; /**< Estimate of reverb energy */
        public float[] old_ps;     /**< Power spectrum for last frame */
        public float[] gain;       /**< Ephraim Malah gain */
        public float[] prior;      /**< A-priori SNR */
        public float[] post;       /**< A-posteriori SNR */

        public float[] S;          /**< Smoothed power spectrum */
        public float[] Smin;       /**< See Cohen paper */
        public float[] Stmp;       /**< See Cohen paper */
        public int[] update_prob;         /**< Probability of speech presence for noise update */

        public float[] zeta;       /**< Smoothed a priori SNR */
        public float[] echo_noise;
        public float[] residual_echo;

        /* Misc */
        public float[] inbuf;      /**< Input buffer (overlapped analysis) */
        public float[] outbuf;     /**< Output buffer (for overlap and add) */

        /* AGC stuff, only for floating point for now */
        public int agc_enabled;
        public float agc_level;
        public float loudness_accum;
        public float[] loudness_weight;   /**< Perceptual loudness curve */
        public float loudness;          /**< Loudness estimate */
        public float agc_gain;          /**< Current AGC gain */
        public float max_gain;          /**< Maximum gain allowed */
        public float max_increase_step; /**< Maximum increase in gain from one frame to another */
        public float max_decrease_step; /**< Maximum decrease in gain from one frame to another */
        public float prev_loudness;     /**< Loudness of previous frame */
        public float init_max;          /**< Current gain limit during initialisation */
        public int nb_adapt;          /**< Number of frames used for adaptation so far */
        public int was_speech;
        public int min_count;         /**< Number of frames processed so far */
        public SpeexFft fft_lookup;        /**< Lookup table for the FFT */

        // ks - According to preprocess.c, this should only be here when FIXED_POINT is defined, but it's used in some floating point code.
        // Not sure what's up with that, or why it's not throwing an error.
        public int frame_shift;
    };

    public enum SpeexPreprocessorCommand
    {
        /** Set preprocessor denoiser state */
        SPEEX_PREPROCESS_SET_DENOISE = 0,
        /** Get preprocessor denoiser state */
        SPEEX_PREPROCESS_GET_DENOISE = 1,

        /** Set preprocessor Automatic Gain Control state */
        SPEEX_PREPROCESS_SET_AGC = 2,
        /** Get preprocessor Automatic Gain Control state */
        SPEEX_PREPROCESS_GET_AGC = 3,

        /** Set preprocessor Voice Activity Detection state */
        SPEEX_PREPROCESS_SET_VAD = 4,
        /** Get preprocessor Voice Activity Detection state */
        SPEEX_PREPROCESS_GET_VAD = 5,

        /** Set preprocessor Automatic Gain Control level (float) */
        SPEEX_PREPROCESS_SET_AGC_LEVEL = 6,
        /** Get preprocessor Automatic Gain Control level (float) */
        SPEEX_PREPROCESS_GET_AGC_LEVEL = 7,

        /** Set preprocessor dereverb state */
        SPEEX_PREPROCESS_SET_DEREVERB = 8,
        /** Get preprocessor dereverb state */
        SPEEX_PREPROCESS_GET_DEREVERB = 9,

        /** Set preprocessor dereverb level */
        SPEEX_PREPROCESS_SET_DEREVERB_LEVEL = 10,

        /** Get preprocessor dereverb level */
        SPEEX_PREPROCESS_GET_DEREVERB_LEVEL = 11,

        /** Set preprocessor dereverb decay */
        SPEEX_PREPROCESS_SET_DEREVERB_DECAY = 12,
        /** Get preprocessor dereverb decay */
        SPEEX_PREPROCESS_GET_DEREVERB_DECAY = 13,

        /** Set probability required for the VAD to go from silence to voice */
        SPEEX_PREPROCESS_SET_PROB_START = 14,
        /** Get probability required for the VAD to go from silence to voice */
        SPEEX_PREPROCESS_GET_PROB_START = 15,

        /** Set probability required for the VAD to stay in the voice state (integer percent) */
        SPEEX_PREPROCESS_SET_PROB_CONTINUE = 16,
        /** Get probability required for the VAD to stay in the voice state (integer percent) */
        SPEEX_PREPROCESS_GET_PROB_CONTINUE = 17,

        /** Set maximum attenuation of the noise in dB (negative number) */
        SPEEX_PREPROCESS_SET_NOISE_SUPPRESS = 18,
        /** Get maximum attenuation of the noise in dB (negative number) */
        SPEEX_PREPROCESS_GET_NOISE_SUPPRESS = 19,

        /** Set maximum attenuation of the residual echo in dB (negative number) */
        SPEEX_PREPROCESS_SET_ECHO_SUPPRESS = 20,
        /** Get maximum attenuation of the residual echo in dB (negative number) */
        SPEEX_PREPROCESS_GET_ECHO_SUPPRESS = 21,

        /** Set maximum attenuation of the residual echo in dB when near end is active (negative number) */
        SPEEX_PREPROCESS_SET_ECHO_SUPPRESS_ACTIVE = 22,
        /** Get maximum attenuation of the residual echo in dB when near end is active (negative number) */
        SPEEX_PREPROCESS_GET_ECHO_SUPPRESS_ACTIVE = 23,

        /** Set the corresponding echo canceller state so that residual echo suppression can be performed (NULL for no residual echo suppression) */
        SPEEX_PREPROCESS_SET_ECHO_STATE = 24,
        /** Get the corresponding echo canceller state */
        SPEEX_PREPROCESS_GET_ECHO_STATE = 25,

        /** Set maximal gain increase in dB/second (int32) */
        SPEEX_PREPROCESS_SET_AGC_INCREMENT = 26,

        /** Get maximal gain increase in dB/second (int32) */
        SPEEX_PREPROCESS_GET_AGC_INCREMENT = 27,

        /** Set maximal gain decrease in dB/second (int32) */
        SPEEX_PREPROCESS_SET_AGC_DECREMENT = 28,

        /** Get maximal gain decrease in dB/second (int32) */
        SPEEX_PREPROCESS_GET_AGC_DECREMENT = 29,

        /** Set maximal gain in dB (int32) */
        SPEEX_PREPROCESS_SET_AGC_MAX_GAIN = 30,

        /** Get maximal gain in dB (int32) */
        SPEEX_PREPROCESS_GET_AGC_MAX_GAIN = 31,

        /*  Can't set loudness */
        /** Get loudness */
        SPEEX_PREPROCESS_GET_AGC_LOUDNESS = 33,

        /*  Can't set gain */
        /** Get current gain (int32 percent) */
        SPEEX_PREPROCESS_GET_AGC_GAIN = 35,

        /*  Can't set spectrum size */
        /** Get spectrum size for power spectrum (int32) */
        SPEEX_PREPROCESS_GET_PSD_SIZE = 37,

        /*  Can't set power spectrum */
        /** Get power spectrum (int32[] of squared values) */
        SPEEX_PREPROCESS_GET_PSD = 39,

        /*  Can't set noise size */
        /** Get spectrum size for noise estimate (int32)  */
        SPEEX_PREPROCESS_GET_NOISE_PSD_SIZE = 41,

        /*  Can't set noise estimate */
        /** Get noise estimate (int32[] of squared values) */
        SPEEX_PREPROCESS_GET_NOISE_PSD = 43,

        /* Can't set speech probability */
        /** Get speech probability in last frame (int32).  */
        SPEEX_PREPROCESS_GET_PROB = 45,

        /** Set preprocessor Automatic Gain Control level (int32) */
        SPEEX_PREPROCESS_SET_AGC_TARGET = 46,
        /** Get preprocessor Automatic Gain Control level (int32) */
        SPEEX_PREPROCESS_GET_AGC_TARGET = 47,
    }

    public class SpeexPreprocessor
    {
        const float Q15_ONE = 1.0f;
        const float Q15ONE = 1.0f;
        const float FLOAT_ONE = 1.0f;
        const float FLOAT_ZERO = 0.0f;

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
        static float DIV32_16_Q8(float a, float b) { return a / b; }
        static float DIV32_16_Q15(float a, float b) { return a / b; }
        static float PDIV32_16(float a, float b) { return a / b; }
        static float FLOAT_MUL32(float a, float b) { return a * b; }
        static float MAC16_16(float c, float a, float b) { return c + a * b; }
        static float MULT16_16(float a, float b) { return a * b; }
        static float MULT16_32_P15(float a, float b) { return a * b; }
        static float MULT16_16_Q14(float a, float b) { return a * b; }
        static float MULT16_16_Q15(float a, float b) { return a * b; }
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
        static float SQR16_Q15(float a) { return a * a; }
        static float spx_cos_norm(float x) { return (float)(Math.Cos((.5f * Math.PI) * (x))); }


        static void conj_window(float[] w, int len)
        {
            int i;
            for (i = 0; i < len; i++)
            {
                float tmp;
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
                tmp = SQR16_Q15(QCONST16(.5f, 15) - MULT16_16_P15(QCONST16(.5f, 15), spx_cos_norm(SHL32(EXTEND32(x), 2))));
                if (inv != 0)
                    tmp = SUB16(Q15_ONE, tmp);
                w[i] = (float)Math.Sqrt(SHL32(EXTEND32(tmp), 15));
            }
        }


        /* This function approximates the gain function 
           y = gamma(1.25)^2 * M(-.25;1;-x) / sqrt(x)  
           which multiplied by xi/(1+xi) is the optimal gain
           in the loudness domain ( sqrt[amplitude] )
        */
        static float hypergeom_gain(float xx)
        {
            int ind;
            float integer, frac;
            float x;
            float[] table = new float[] {
                  0.82157f, 1.02017f, 1.20461f, 1.37534f, 1.53363f, 1.68092f, 1.81865f,
                  1.94811f, 2.07038f, 2.18638f, 2.29688f, 2.40255f, 2.50391f, 2.60144f,
                  2.69551f, 2.78647f, 2.87458f, 2.96015f, 3.04333f, 3.12431f, 3.20326f};
            x = EXPIN_SCALING_1 * xx;
            integer = (float)Math.Floor(2 * x);
            ind = (int)integer;
            if (ind < 0)
                return FRAC_SCALING;
            if (ind > 19)
                return FRAC_SCALING * (1 + .1296f / x);
            frac = 2 * x - integer;
            return (float)(FRAC_SCALING * ((1 - frac) * table[ind] + frac * table[ind + 1]) / Math.Sqrt(x + .0001f));
        }

        static float qcurve(float x)
        {
            return 1.0f / (1.0f + .15f / (SNR_SCALING_1 * x));
        }

        static void compute_gain_floor(int noise_suppress, int effective_echo_suppress,
            float[] noise, int noiseoffset,
            float[] echo, int echooffset,
            float[] gain_floor, int gain_floor_offset,
            int len)
        {
            int i;
            float echo_floor;
            float noise_floor;

            noise_floor = (float)Math.Exp(.2302585f * noise_suppress);
            echo_floor = (float)Math.Exp(.2302585f * effective_echo_suppress);

            /* Compute the gain floor based on different floors for the background noise and residual echo */
            for (i = 0; i < len; i++)
            {
                gain_floor[gain_floor_offset + i] =
                    (float)(FRAC_SCALING * Math.Sqrt(noise_floor *
                        PSHR32(noise[noiseoffset + i], NOISE_SHIFT) + echo_floor * echo[echooffset + i]) /
                        Math.Sqrt(1 + PSHR32(noise[noiseoffset + i], NOISE_SHIFT) + echo[echooffset + i]));
            }
        }

        public static SpeexPreprocessState speex_preprocess_state_init(int frame_size, int sampling_rate)
        {
            int i;
            int N, N3, N4, M;

            SpeexPreprocessState st = new SpeexPreprocessState(); // (SpeexPreprocessState )speex_alloc(sizeof(SpeexPreprocessState));
            st.frame_size = frame_size;

            /* Round ps_size down to the nearest power of two */
            st.ps_size = st.frame_size;

            N = st.ps_size;
            N3 = 2 * N - st.frame_size;
            N4 = st.frame_size - N3;

            st.sampling_rate = sampling_rate;
            st.denoise_enabled = 1;
            st.vad_enabled = 0;
            st.dereverb_enabled = 0;
            st.reverb_decay = 0;
            st.reverb_level = 0;
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
                st.window[i] = Q15_ONE;

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
                st.update_prob[i] = 1;
            for (i = 0; i < N3; i++)
            {
                st.inbuf[i] = 0;
                st.outbuf[i] = 0;
            }
            st.agc_enabled = 0;
            st.agc_level = 8000;
            st.loudness_weight = new float[N]; // (float*)speex_alloc(N * sizeof(float));
            for (i = 0; i < N; i++)
            {
                float ff = ((float)i) * .5f * sampling_rate / ((float)N);
                /*st.loudness_weight[i] = .5f*(1.0f/(1.0f+ff/8000.0f))+1.0f*Math.Exp(-.5f*(ff-3800.0f)*(ff-3800.0f)/9e5f);*/
                st.loudness_weight[i] = (float)(.35f - .35f * ff / 16000.0f + .73f * Math.Exp(-.5f * (ff - 3800) * (ff - 3800) / 9e5f));
                if (st.loudness_weight[i] < .01f)
                    st.loudness_weight[i] = .01f;
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
            st.was_speech = 0;

            st.fft_lookup = new SpeexFft(2 * N);

            st.nb_adapt = 0;
            st.min_count = 0;
            return st;
        }

        public static void speex_preprocess_state_destroy(SpeexPreprocessState st)
        {
            // No-op due to GC
        }

        /* FIXME: The AGC doesn't work yet with fixed-point*/
        static void speex_compute_agc(SpeexPreprocessState st, float Pframe, float[] ft)
        {
            int i;
            int N = st.ps_size;
            float target_gain;
            float loudness = 1.0f;
            float rate;

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
                rate = .03f * Pframe * Pframe;
                st.loudness = (float)((1 - rate) * st.loudness + (rate) * Math.Pow(AMP_SCALE * loudness, LOUDNESS_EXP));
                st.loudness_accum = (1 - rate) * st.loudness_accum + rate;
                if (st.init_max < st.max_gain && st.nb_adapt > 20)
                    st.init_max *= 1.0f + .1f * Pframe * Pframe;
            }
            /*printf ("%f %f %f %f\n", Pframe, loudness, pow(st.loudness, 1.0f/LOUDNESS_EXP), st.loudness2);*/

            target_gain = AMP_SCALE * st.agc_level * (float)Math.Pow(st.loudness / (1e-4 + st.loudness_accum), -1.0f / LOUDNESS_EXP);

            if ((Pframe > .5 && st.nb_adapt > 20) || target_gain < st.agc_gain)
            {
                if (target_gain > st.max_increase_step * st.agc_gain)
                    target_gain = st.max_increase_step * st.agc_gain;
                if (target_gain < st.max_decrease_step * st.agc_gain && loudness < 10 * st.prev_loudness)
                    target_gain = st.max_decrease_step * st.agc_gain;
                if (target_gain > st.max_gain)
                    target_gain = st.max_gain;
                if (target_gain > st.init_max)
                    target_gain = st.init_max;

                st.agc_gain = target_gain;
            }
            /*fprintf (stderr, "%f %f %f\n", loudness, (float)AMP_SCALE_1*pow(st.loudness, 1.0f/LOUDNESS_EXP), st.agc_gain);*/

            for (i = 0; i < 2 * N; i++)
                ft[i] *= st.agc_gain;
            st.prev_loudness = loudness;
        }

        static void preprocess_analysis(SpeexPreprocessState st, short[] x)
        {
            int i;
            int N = st.ps_size;
            int N3 = 2 * N - st.frame_size;
            int N4 = st.frame_size - N3;
            float[] ps = st.ps;

            /* 'Build' input frame */
            for (i = 0; i < N3; i++)
                st.frame[i] = st.inbuf[i];
            for (i = 0; i < st.frame_size; i++)
                st.frame[N3 + i] = x[i];

            /* Update inbuf */
            for (i = 0; i < N3; i++)
                st.inbuf[i] = x[N4 + i];

            /* Windowing */
            for (i = 0; i < 2 * N; i++)
                st.frame[i] = MULT16_16_Q15(st.frame[i], st.window[i]);

            /* Perform FFT */
            st.fft_lookup.DoFft(st.frame, 0, st.ft, 0);

            /* Power spectrum */
            ps[0] = MULT16_16(st.ft[0], st.ft[0]);
            for (i = 1; i < N; i++)
                ps[i] = MULT16_16(st.ft[2 * i - 1], st.ft[2 * i - 1]) + MULT16_16(st.ft[2 * i], st.ft[2 * i]);
            for (i = 0; i < N; i++)
                st.ps[i] = PSHR32(st.ps[i], 2 * st.frame_shift);

            SpeexFilterBank.filterbank_compute_bank32(st.bank, ps, ps, N);
        }

        static void update_noise_prob(SpeexPreprocessState st)
        {
            int i;
            int min_range;
            int N = st.ps_size;

            for (i = 1; i < N - 1; i++)
                st.S[i] = MULT16_32_Q15(QCONST16(.8f, 15), st.S[i]) + MULT16_32_Q15(QCONST16(.05f, 15), st.ps[i - 1])
                                + MULT16_32_Q15(QCONST16(.1f, 15), st.ps[i]) + MULT16_32_Q15(QCONST16(.05f, 15), st.ps[i + 1]);
            st.S[0] = MULT16_32_Q15(QCONST16(.8f, 15), st.S[0]) + MULT16_32_Q15(QCONST16(.2f, 15), st.ps[0]);
            st.S[N - 1] = MULT16_32_Q15(QCONST16(.8f, 15), st.S[N - 1]) + MULT16_32_Q15(QCONST16(.2f, 15), st.ps[N - 1]);

            if (st.nb_adapt == 1)
            {
                for (i = 0; i < N; i++)
                    st.Smin[i] = st.Stmp[i] = 0;
            }

            if (st.nb_adapt < 100)
                min_range = 15;
            else if (st.nb_adapt < 1000)
                min_range = 50;
            else if (st.nb_adapt < 10000)
                min_range = 150;
            else
                min_range = 300;
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
                    st.update_prob[i] = 1;
                else
                    st.update_prob[i] = 0;
                /*fprintf (stderr, "%f ", st.S[i]/st.Smin[i]);*/
                /*fprintf (stderr, "%f ", st.update_prob[i]);*/
            }

        }

        // private static float NOISE_OVERCOMPENS = 1.0f;

        public static int speex_preprocess(SpeexPreprocessState st, short[] x, int[] echo)
        {
            return speex_preprocess_run(st, x);
        }

        public static int speex_preprocess_run(SpeexPreprocessState st, short[] x)
        {
            int i;
            int M;
            int N = st.ps_size;
            int N3 = 2 * N - st.frame_size;
            int N4 = st.frame_size - N3;
            float[] ps = st.ps;
            float Zframe;
            float Pframe;
            float beta, beta_1;
            float effective_echo_suppress;

            st.nb_adapt++;
            if (st.nb_adapt > 20000)
                st.nb_adapt = 20000;
            st.min_count++;

            beta = Math.Max(QCONST16(.03f, 15), DIV32_16(Q15_ONE, st.nb_adapt));
            beta_1 = Q15_ONE - beta;
            M = st.nbands;
            /* Deal with residual echo if provided */
            if (st.echo_state != null)
            {
                SpeexEchoCanceller.speex_echo_get_residual(st.echo_state, st.residual_echo, N);
                /* If there are NaNs or ridiculous values, it'll show up in the DC and we just reset everything to zero */
                if (!(st.residual_echo[0] >= 0 && st.residual_echo[0] < N * 1e9f))
                {
                    for (i = 0; i < N; i++)
                        st.residual_echo[i] = 0;
                }
                for (i = 0; i < N; i++)
                    st.echo_noise[i] = Math.Max(MULT16_32_Q15(QCONST16(.6f, 15), st.echo_noise[i]), st.residual_echo[i]);
                SpeexFilterBank.filterbank_compute_bank32(st.bank, st.echo_noise, st.echo_noise, N);
            }
            else
            {
                for (i = 0; i < N + M; i++)
                    st.echo_noise[i] = 0;
            }
            preprocess_analysis(st, x);

            update_noise_prob(st);

            /* Noise estimation always updated for the 10 first frames */
            /*if (st.nb_adapt<10)
            {
               for (i=1;i<N-1;i++)
                  st.update_prob[i] = 0;
            }
            */

            /* Update the noise estimate for the frequencies where it can be */
            for (i = 0; i < N; i++)
            {
                if (st.update_prob[i] == 0 || st.ps[i] < PSHR32(st.noise[i], NOISE_SHIFT))
                    st.noise[i] = Math.Max(EXTEND32(0), MULT16_32_Q15(beta_1, st.noise[i]) + MULT16_32_Q15(beta, SHL32(st.ps[i], NOISE_SHIFT)));
            }
            SpeexFilterBank.filterbank_compute_bank32(st.bank, st.noise, st.noise, N);

            /* Special case for first frame */
            if (st.nb_adapt == 1)
                for (i = 0; i < N + M; i++)
                    st.old_ps[i] = ps[i];

            /* Compute a posteriori SNR */
            for (i = 0; i < N + M; i++)
            {
                float gamma;

                /* Total noise estimate including residual echo and reverberation */
                float tot_noise = ADD32(ADD32(ADD32(EXTEND32(1), PSHR32(st.noise[i], NOISE_SHIFT)), st.echo_noise[i]), st.reverb_estimate[i]);

                /* A posteriori SNR = ps/noise - 1*/
                st.post[i] = SUB16(DIV32_16_Q8(ps[i], tot_noise), QCONST16(1.0f, SNR_SHIFT));
                st.post[i] = Math.Min(st.post[i], QCONST16(100.0f, SNR_SHIFT));

                /* Computing update gamma = .1 + .9*(old/(old+noise))^2 */
                gamma = QCONST16(.1f, 15) + MULT16_16_Q15(QCONST16(.89f, 15), SQR16_Q15(DIV32_16_Q15(st.old_ps[i], ADD32(st.old_ps[i], tot_noise))));

                /* A priori SNR update = gamma*max(0,post) + (1-gamma)*old/noise */
                st.prior[i] = EXTRACT16(PSHR32(ADD32(MULT16_16(gamma, Math.Max(0, st.post[i])), MULT16_16(Q15_ONE - gamma, DIV32_16_Q8(st.old_ps[i], tot_noise))), 15));
                st.prior[i] = Math.Min(st.prior[i], QCONST16(100.0f, SNR_SHIFT));
            }

            /*print_vec(st.post, N+M, "");*/

            /* Recursive average of the a priori SNR. A bit smoothed for the psd components */
            st.zeta[0] = PSHR32(ADD32(MULT16_16(QCONST16(.7f, 15), st.zeta[0]), MULT16_16(QCONST16(.3f, 15), st.prior[0])), 15);
            for (i = 1; i < N - 1; i++)
                st.zeta[i] = PSHR32(ADD32(ADD32(ADD32(MULT16_16(QCONST16(.7f, 15), st.zeta[i]), MULT16_16(QCONST16(.15f, 15), st.prior[i])),
                                     MULT16_16(QCONST16(.075f, 15), st.prior[i - 1])), MULT16_16(QCONST16(.075f, 15), st.prior[i + 1])), 15);
            for (i = N - 1; i < N + M; i++)
                st.zeta[i] = PSHR32(ADD32(MULT16_16(QCONST16(.7f, 15), st.zeta[i]), MULT16_16(QCONST16(.3f, 15), st.prior[i])), 15);

            /* Speech probability of presence for the entire frame is based on the average filterbank a priori SNR */
            Zframe = 0;
            for (i = N; i < N + M; i++)
                Zframe = ADD32(Zframe, EXTEND32(st.zeta[i]));
            Pframe = QCONST16(.1f, 15) + MULT16_16_Q15(QCONST16(.899f, 15), qcurve(DIV32_16(Zframe, st.nbands)));

            effective_echo_suppress = EXTRACT16(PSHR32(ADD32(MULT16_16(SUB16(Q15_ONE, Pframe), st.echo_suppress), MULT16_16(Pframe, st.echo_suppress_active)), 15));

            compute_gain_floor(st.noise_suppress, (int)effective_echo_suppress, st.noise, N, st.echo_noise, N, st.gain_floor, N, M);

            /* Compute Ephraim & Malah gain speech probability of presence for each critical band (Bark scale) 
               Technically this is actually wrong because the EM gaim assumes a slightly different probability 
               distribution */
            for (i = N; i < N + M; i++)
            {
                /* See EM and Cohen papers*/
                float theta;
                /* Gain from hypergeometric function */
                float MM;
                /* Weiner filter gain */
                float prior_ratio;
                /* a priority probability of speech presence based on Bark sub-band alone */
                float P1;
                /* Speech absence a priori probability (considering sub-band and frame) */
                float q;

                prior_ratio = PDIV32_16(SHL32(EXTEND32(st.prior[i]), 15), ADD16(st.prior[i], SHL32(1, SNR_SHIFT)));
                theta = MULT16_32_P15(prior_ratio, QCONST32(1.0f, EXPIN_SHIFT) + SHL32(EXTEND32(st.post[i]), EXPIN_SHIFT - SNR_SHIFT));

                MM = hypergeom_gain(theta);
                /* Gain with bound */
                st.gain[i] = EXTRACT16(Math.Min(Q15_ONE, MULT16_32_Q15(prior_ratio, MM)));
                /* Save old Bark power spectrum */
                st.old_ps[i] = MULT16_32_P15(QCONST16(.2f, 15), st.old_ps[i]) + MULT16_32_P15(MULT16_16_P15(QCONST16(.8f, 15), SQR16_Q15(st.gain[i])), ps[i]);

                P1 = QCONST16(.199f, 15) + MULT16_16_Q15(QCONST16(.8f, 15), qcurve(st.zeta[i]));
                q = Q15_ONE - MULT16_16_Q15(Pframe, P1);
                st.gain2[i] = 1 / (1.0f + (q / (1.0f - q)) * (1 + st.prior[i]) * (float)Math.Exp(-theta));
            }
            /* Convert the EM gains and speech prob to linear frequency */
            SpeexFilterBank.filterbank_compute_psd16(st.bank, st.gain2, N, st.gain2);
            SpeexFilterBank.filterbank_compute_psd16(st.bank, st.gain, N, st.gain);

            /* Use true for linear gain resolution (best) or false for Bark gain resolution (faster) */
            if (true)
            {
                SpeexFilterBank.filterbank_compute_psd16(st.bank, st.gain_floor, N, st.gain_floor);

                /* Compute gain according to the Ephraim-Malah algorithm -- linear frequency */
                for (i = 0; i < N; i++)
                {
                    float MM;
                    float theta;
                    float prior_ratio;
                    float tmp;
                    float p;
                    float g;

                    /* Wiener filter gain */
                    prior_ratio = PDIV32_16(SHL32(EXTEND32(st.prior[i]), 15), ADD16(st.prior[i], SHL32(1, SNR_SHIFT)));
                    theta = MULT16_32_P15(prior_ratio, QCONST32(1.0f, EXPIN_SHIFT) + SHL32(EXTEND32(st.post[i]), EXPIN_SHIFT - SNR_SHIFT));

                    /* Optimal estimator for loudness domain */
                    MM = hypergeom_gain(theta);
                    /* EM gain with bound */
                    g = EXTRACT16(Math.Min(Q15_ONE, MULT16_32_Q15(prior_ratio, MM)));
                    /* Interpolated speech probability of presence */
                    p = st.gain2[i];

                    /* Constrain the gain to be close to the Bark scale gain */
                    if (MULT16_16_Q15(QCONST16(.333f, 15), g) > st.gain[i])
                        g = MULT16_16(3, st.gain[i]);
                    st.gain[i] = g;

                    /* Save old power spectrum */
                    st.old_ps[i] = MULT16_32_P15(QCONST16(.2f, 15), st.old_ps[i]) + MULT16_32_P15(MULT16_16_P15(QCONST16(.8f, 15), SQR16_Q15(st.gain[i])), ps[i]);

                    /* Apply gain floor */
                    if (st.gain[i] < st.gain_floor[i])
                        st.gain[i] = st.gain_floor[i];

                    /* Exponential decay model for reverberation (unused) */
                    /*st.reverb_estimate[i] = st.reverb_decay*st.reverb_estimate[i] + st.reverb_decay*st.reverb_level*st.gain[i]*st.gain[i]*st.ps[i];*/

                    /* Take into account speech probability of presence (loudness domain MMSE estimator) */
                    /* gain2 = [p*sqrt(gain)+(1-p)*sqrt(gain _floor) ]^2 */
                    tmp = MULT16_16_P15(p, (float)Math.Sqrt(SHL32(EXTEND32(st.gain[i]), 15))) + MULT16_16_P15(SUB16(Q15_ONE, p), (float)Math.Sqrt(SHL32(EXTEND32(st.gain_floor[i]), 15)));
                    st.gain2[i] = SQR16_Q15(tmp);

                    /* Use this if you want a log-domain MMSE estimator instead */
                    /*st.gain2[i] = pow(st.gain[i], p) * pow(st.gain_floor[i],1.0f-p);*/
                }
            }
            else
            {
                for (i = N; i < N + M; i++)
                {
                    float tmp;
                    float p = st.gain2[i];
                    st.gain[i] = Math.Max(st.gain[i], st.gain_floor[i]);
                    tmp = MULT16_16_P15(p, (float)Math.Sqrt(SHL32(EXTEND32(st.gain[i]), 15))) + MULT16_16_P15(SUB16(Q15_ONE, p), (float)Math.Sqrt(SHL32(EXTEND32(st.gain_floor[i]), 15)));
                    st.gain2[i] = SQR16_Q15(tmp);
                }
                SpeexFilterBank.filterbank_compute_psd16(st.bank, st.gain2, N, st.gain2);
            }

            /* If noise suppression is off, don't apply the gain (but then why call this in the first place!) */
            if (st.denoise_enabled == 0)
            {
                for (i = 0; i < N + M; i++)
                    st.gain2[i] = Q15_ONE;
            }

            /* Apply computed gain */
            for (i = 1; i < N; i++)
            {
                st.ft[2 * i - 1] = MULT16_16_P15(st.gain2[i], st.ft[2 * i - 1]);
                st.ft[2 * i] = MULT16_16_P15(st.gain2[i], st.ft[2 * i]);
            }
            st.ft[0] = MULT16_16_P15(st.gain2[0], st.ft[0]);
            st.ft[2 * N - 1] = MULT16_16_P15(st.gain2[N - 1], st.ft[2 * N - 1]);

            /*FIXME: This *will* not work for fixed-point */
            if (st.agc_enabled != 0)
                speex_compute_agc(st, Pframe, st.ft);

            /* Inverse FFT with 1/N scaling */
            st.fft_lookup.DoIfft(st.ft, 0, st.frame, 0);
            /* Scale back to original (lower) amplitude */
            // for (i = 0; i < 2 * N; i++)
            // st.frame[i] = PSHR16(st.frame[i], st.frame_shift);

            /*FIXME: This *will* not work for fixed-point */
            if (st.agc_enabled != 0)
            {
                float max_sample = 0;
                for (i = 0; i < 2 * N; i++)
                    if (Math.Abs(st.frame[i]) > max_sample)
                        max_sample = Math.Abs(st.frame[i]);
                if (max_sample > 28000.0f)
                {
                    float damp = 28000.0f / max_sample;
                    for (i = 0; i < 2 * N; i++)
                        st.frame[i] *= damp;
                }
            }

            /* Synthesis window (for WOLA) */
            for (i = 0; i < 2 * N; i++)
                st.frame[i] = MULT16_16_Q15(st.frame[i], st.window[i]);

            /* Perform overlap and add */
            for (i = 0; i < N3; i++)
                x[i] = (short)(st.outbuf[i] + st.frame[i]);
            for (i = 0; i < N4; i++)
                x[N3 + i] = (short)st.frame[N3 + i];

            /* Update outbuf */
            for (i = 0; i < N3; i++)
                st.outbuf[i] = st.frame[st.frame_size + i];

            /* FIXME: This VAD is a kludge */
            st.speech_prob = Pframe;
            if (st.vad_enabled != 0)
            {
                if (st.speech_prob > st.speech_prob_start || (st.was_speech != 0 && st.speech_prob > st.speech_prob_continue))
                {
                    st.was_speech = 1;
                    return 1;
                }
                else
                {
                    st.was_speech = 0;
                    return 0;
                }
            }
            else
            {
                return 1;
            }
        }

        public void speex_preprocess_estimate_update(SpeexPreprocessState st, short[] x)
        {
            int i;
            int N = st.ps_size;
            int N3 = 2 * N - st.frame_size;
            int M;
            float[] ps = st.ps;

            M = st.nbands;
            st.min_count++;

            preprocess_analysis(st, x);

            update_noise_prob(st);

            for (i = 1; i < N - 1; i++)
            {
                if (st.update_prob[i] == 0 || st.ps[i] < PSHR32(st.noise[i], NOISE_SHIFT))
                {
                    st.noise[i] = MULT16_32_Q15(QCONST16(.95f, 15), st.noise[i]) + MULT16_32_Q15(QCONST16(.05f, 15), SHL32(st.ps[i], NOISE_SHIFT));
                }
            }

            for (i = 0; i < N3; i++)
                st.outbuf[i] = MULT16_16_Q15(x[st.frame_size - N3 + i], st.window[st.frame_size + i]);

            /* Save old power spectrum */
            for (i = 0; i < N + M; i++)
                st.old_ps[i] = ps[i];

            for (i = 0; i < N; i++)
                st.reverb_estimate[i] = MULT16_32_Q15(st.reverb_decay, st.reverb_estimate[i]);
        }


        public static int speex_preprocess_ctl(SpeexPreprocessState st, SpeexPreprocessorCommand request, ref object ptr)
        {
            int i;
            switch (request)
            {
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_DENOISE:
                    st.denoise_enabled = (int)ptr;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_DENOISE:
                    ptr = st.denoise_enabled;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_AGC:
                    st.agc_enabled = (int)ptr;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC:
                    ptr = st.agc_enabled;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_AGC_LEVEL:
                    st.agc_level = (float)ptr;
                    if (st.agc_level < 1)
                        st.agc_level = 1;
                    if (st.agc_level > 32768)
                        st.agc_level = 32768;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC_LEVEL:
                    ptr = st.agc_level;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_AGC_INCREMENT:
                    st.max_increase_step = (float)Math.Exp(0.11513f * (int)ptr * st.frame_size / st.sampling_rate);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC_INCREMENT:
                    ptr = (float)Math.Floor(.5 + 8.6858 * Math.Log(st.max_increase_step) * st.sampling_rate / st.frame_size);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_AGC_DECREMENT:
                    st.max_decrease_step = (float)Math.Exp(0.11513f * ((int)ptr) * st.frame_size / st.sampling_rate);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC_DECREMENT:
                    ptr = (float)Math.Floor(.5 + 8.6858 * (float)Math.Log(st.max_decrease_step) * st.sampling_rate / st.frame_size);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_AGC_MAX_GAIN:
                    st.max_gain = (float)Math.Exp(0.11513f * (int)ptr);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC_MAX_GAIN:
                    ptr = (float)Math.Floor(.5 + 8.6858 * (float)Math.Log(st.max_gain));
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_VAD:
                    MediaLogger.LogDebugMessage("The VAD has been replaced by a hack pending a complete rewrite");
                    st.vad_enabled = (int)ptr;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_VAD:
                    ptr = st.vad_enabled;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_DEREVERB:
                    st.dereverb_enabled = (int)ptr;
                    for (i = 0; i < st.ps_size; i++)
                        st.reverb_estimate[i] = 0;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_DEREVERB:
                    ptr = st.dereverb_enabled;
                    break;

                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_DEREVERB_LEVEL:
                    /* FIXME: Re-enable when de-reverberation is actually enabled again */
                    /*st.reverb_level = (*(float*)ptr);*/
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_DEREVERB_LEVEL:
                    /* FIXME: Re-enable when de-reverberation is actually enabled again */
                    /*(*(float*)ptr) = st.reverb_level;*/
                    break;

                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_DEREVERB_DECAY:
                    /* FIXME: Re-enable when de-reverberation is actually enabled again */
                    /*st.reverb_decay = (*(float*)ptr);*/
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_DEREVERB_DECAY:
                    /* FIXME: Re-enable when de-reverberation is actually enabled again */
                    /*(*(float*)ptr) = st.reverb_decay;*/
                    break;

                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_PROB_START:
                    ptr = Math.Min(100, Math.Max(0, (int)ptr));
                    st.speech_prob_start = DIV32_16(MULT16_16(Q15ONE, (int)ptr), 100);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_PROB_START:
                    ptr = MULT16_16_Q15(st.speech_prob_start, 100);
                    break;

                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_PROB_CONTINUE:
                    ptr = Math.Min(100, Math.Max(0, (int)ptr));
                    st.speech_prob_continue = DIV32_16(MULT16_16(Q15ONE, (int)ptr), 100);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_PROB_CONTINUE:
                    ptr = MULT16_16_Q15(st.speech_prob_continue, 100);
                    break;

                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_NOISE_SUPPRESS:
                    st.noise_suppress = -Math.Abs((int)ptr);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_NOISE_SUPPRESS:
                    ptr = st.noise_suppress;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_ECHO_SUPPRESS:
                    st.echo_suppress = -Math.Abs((int)ptr);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_ECHO_SUPPRESS:
                    ptr = st.echo_suppress;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_ECHO_SUPPRESS_ACTIVE:
                    st.echo_suppress_active = -Math.Abs((int)ptr);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_ECHO_SUPPRESS_ACTIVE:
                    ptr = st.echo_suppress_active;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_ECHO_STATE:
                    st.echo_state = (SpeexEchoState)ptr;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_ECHO_STATE:
                    ptr = (SpeexEchoState)st.echo_state;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC_LOUDNESS:
                    ptr = Math.Pow(st.loudness, 1.0 / LOUDNESS_EXP);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC_GAIN:
                    ptr = Math.Floor(.5 + 8.6858 * (float)Math.Log(st.agc_gain));
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_PSD_SIZE:
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_NOISE_PSD_SIZE:
                    ptr = st.ps_size;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_PSD:
                    for (i = 0; i < st.ps_size; i++)
                        ((int[])ptr)[i] = (int)st.ps[i];
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_NOISE_PSD:
                    for (i = 0; i < st.ps_size; i++)
                        ((int[])ptr)[i] = (int)PSHR32(st.noise[i], NOISE_SHIFT);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_PROB:
                    ptr = MULT16_16_Q15(st.speech_prob, 100);
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_SET_AGC_TARGET:
                    st.agc_level = ((int)ptr);
                    if (st.agc_level < 1)
                        st.agc_level = 1;
                    if (st.agc_level > 32768)
                        st.agc_level = 32768;
                    break;
                case SpeexPreprocessorCommand.SPEEX_PREPROCESS_GET_AGC_TARGET:
                    ptr = st.agc_level;
                    break;
                default:
                    MediaLogger.LogDebugMessage("Unknown speex_preprocess_ctl request: {0}", request);
                    return -1;
            }
            return 0;
        }

    }
}
