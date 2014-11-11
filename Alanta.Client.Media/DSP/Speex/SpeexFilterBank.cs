using System;

namespace Alanta.Client.Media.Dsp.Speex
{
    public class SpeexFilterBank
    {
        public int[] bank_left;
        public int[] bank_right;
        public float[] filter_left;
        public float[] filter_right;
        public float[] scaling;
        public int nb_banks;
        public int len;

        static float toBARK(float n) { return (float)(13.1f * Math.Atan(.00074f * (n)) + 2.24f * Math.Atan((n) * (n) * 1.85e-8f) + 1e-4f * (n)); }
        static float toMEL(float n) { return (float)(2595.0f * Math.Log10(1.0f + (n) / 700.0f)); }

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
        static float ADD16(float a, float b) { return a + b; }
        static float ADD32(float a, float b) { return a + b; }
        static short ADD16(short a, short b) { return (short)(a + b); }
        static int ADD32(short a, short b) { return a + b; }
        static float SUB16(float a, float b) { return a - b; }
        static float SUB32(float a, float b) { return a - b; }
        static short SUB16(short a, short b) { return (short)(a - b); }
        static int SUB32(short a, short b) { return a - b; }
        static float DIV32(float a, float b) { return a / b; }
        static float DIV32_16(float a, float b) { return a / b; }
        static float FLOAT_MUL32(float a, float b) { return a * b; }
        static float MAC16_16(float c, float a, float b) { return c + a * b; }
        static float MULT16_16(float a, float b) { return a * b; }
        static float MULT16_32_P15(float a, float b) { return a * b; }
        static float PDIV32(float a, float b) { return a / b; }
        const float Q15ONE = 1.0f;
        const float Q15_ONE = 1.0f;

        public static SpeexFilterBank filterbank_new(int banks, float sampling, int len, int type)
        {
        	int i;
        	float df = DIV32(SHL32(sampling, 15), MULT16_16(2, len));
            float maxMel = toBARK(EXTRACT16(sampling / 2));
            float melInterval = PDIV32(maxMel, banks - 1);

            var bank = new SpeexFilterBank();
            bank.nb_banks = banks;
            bank.len = len;
            bank.bank_left = new int[len]; // (int*)speex_alloc(len*sizeof(int));
            bank.bank_right = new int[len]; // (int*)speex_alloc(len*sizeof(int));
            bank.filter_left = new float[len]; // (float*)speex_alloc(len*sizeof(float));
            bank.filter_right = new float[len]; // (float*)speex_alloc(len*sizeof(float));
            /* Think I can safely disable normalisation that for fixed-point (and probably float as well) */
            bank.scaling = new float[banks]; // (float*)speex_alloc(banks*sizeof(float));
            for (i = 0; i < len; i++)
            {
            	float val;
                float currFreq = EXTRACT16(MULT16_32_P15(i, df));
                float mel = toBARK(currFreq);
                if (mel > maxMel)
                    break;
                int id1 = (int)(Math.Floor(mel / melInterval));
                if (id1 > banks - 2)
                {
                    id1 = banks - 2;
                    val = Q15_ONE;
                }
                else
                {
                    val = DIV32_16(mel - id1 * melInterval, EXTRACT16(PSHR32(melInterval, 15)));
                }
                int id2 = id1 + 1;
                bank.bank_left[i] = id1;
                bank.filter_left[i] = SUB16(Q15_ONE, val);
                bank.bank_right[i] = id2;
                bank.filter_right[i] = val;
            }

            /* Think I can safely disable normalisation for fixed-point (and probably float as well) */
            for (i = 0; i < bank.nb_banks; i++)
                bank.scaling[i] = 0;
            for (i = 0; i < bank.len; i++)
            {
                int id = bank.bank_left[i];
                bank.scaling[id] += bank.filter_left[i];
                id = bank.bank_right[i];
                bank.scaling[id] += bank.filter_right[i];
            }
            for (i = 0; i < bank.nb_banks; i++)
                bank.scaling[i] = Q15_ONE / (bank.scaling[i]);
            return bank;
        }

        public static void filterbank_destroy(SpeexFilterBank bank)
        {
            // No-op due to GC.
        }

        public static void filterbank_compute_bank32(SpeexFilterBank bank, float[] ps, float[] mel, int meloffset)
        {
            int i;
            for (i = 0; i < bank.nb_banks; i++)
                mel[meloffset + i] = 0;

            for (i = 0; i < bank.len; i++)
            {
            	int id = bank.bank_left[i];
                mel[meloffset + id] += MULT16_32_P15(bank.filter_left[i], ps[i]);
                id = bank.bank_right[i];
                mel[meloffset + id] += MULT16_32_P15(bank.filter_right[i], ps[i]);
            }
            /* Think I can safely disable normalisation that for fixed-point (and probably float as well) */
            /*for (i=0;i<bank.nb_banks;i++)
               mel[i] = MULT16_32_P15(Q15(bank.scaling[i]),mel[i]);
            */
        }

        public static void filterbank_compute_psd16(SpeexFilterBank bank, float[] mel, int meloffset, float[] ps)
        {
            int i;
            for (i = 0; i < bank.len; i++)
            {
            	int id1 = bank.bank_left[i];
                int id2 = bank.bank_right[i];
                float tmp = MULT16_16(mel[meloffset + id1], bank.filter_left[i]);
                tmp += MULT16_16(mel[meloffset + id2], bank.filter_right[i]);
                ps[i] = EXTRACT16(PSHR32(tmp, 15));
            }
        }

        public static void filterbank_compute_bank(SpeexFilterBank bank, float[] ps, float[] mel)
        {
            int i;
            for (i = 0; i < bank.nb_banks; i++)
                mel[i] = 0;

            for (i = 0; i < bank.len; i++)
            {
                int id = bank.bank_left[i];
                mel[id] += bank.filter_left[i] * ps[i];
                id = bank.bank_right[i];
                mel[id] += bank.filter_right[i] * ps[i];
            }
            for (i = 0; i < bank.nb_banks; i++)
                mel[i] *= bank.scaling[i];
        }

        public static void filterbank_compute_psd(SpeexFilterBank bank, float[] mel, float[] ps)
        {
            int i;
            for (i = 0; i < bank.len; i++)
            {
                int id = bank.bank_left[i];
                ps[i] = mel[id] * bank.filter_left[i];
                id = bank.bank_right[i];
                ps[i] += mel[id] * bank.filter_right[i];
            }
        }

        public static void filterbank_psy_smooth(SpeexFilterBank bank, float[] ps, float[] mask)
        {
            /* Low freq slope: 14 dB/Bark*/
            /* High freq slope: 9 dB/Bark*/
            /* Noise vs tone: 5 dB difference */
            /* FIXME: Temporary kludge */
            var bark = new float[100];
            int i;
            /* Assumes 1/3 Bark resolution */
            float decay_low = 0.34145f;
            float decay_high = 0.50119f;
            filterbank_compute_bank(bank, ps, bark);
            for (i = 1; i < bank.nb_banks; i++)
            {
                /*float decay_high = 13-1.6*log10(bark[i-1]);
                decay_high = pow(10,(-decay_high/30.f));*/
                bark[i] = bark[i] + decay_high * bark[i - 1];
            }
            for (i = bank.nb_banks - 2; i >= 0; i--)
            {
                bark[i] = bark[i] + decay_low * bark[i + 1];
            }
            filterbank_compute_psd(bank, bark, mask);
        }
    }
}
