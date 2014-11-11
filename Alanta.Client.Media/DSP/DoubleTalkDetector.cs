using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alanta.Client.Media.Dsp
{
    class DoubleTalkDetector
    {
        // Geigel DTD (Double Talk Detector)
        private float maxAbsSpeakerSample;
        private int hangover;

        // optimize: less calculations for max()
        private float[] maxIntervalAbsSpeakerSample = new float[Constants.NLMS_LEN / Constants.DTD_LEN];
        private int dtdCnt;
        private int dtdNdx;

        public DoubleTalkDetector()
        {
            maxAbsSpeakerSample = 0.0f;
            hangover = 0;

            Array.Clear(maxIntervalAbsSpeakerSample, 0, Constants.NLMS_LEN / Constants.DTD_LEN);

            dtdCnt = 0;
            dtdNdx = 0;
        }

        /* Geigel Double-Talk Detector */
        public bool detect(float microphoneSample, float speakerSample)
        {
            bool returnValue = false;

            float absMicrophoneSample = Math.Abs(microphoneSample);
            float absSpeakerSample = Math.Abs(speakerSample);

            // optimized implementation of max(|x[0]|, |x[1]|, .., |x[L-1]|):
            // calculate max of block (DTD_LEN values)
            if (absSpeakerSample > maxIntervalAbsSpeakerSample[dtdNdx])
            {
                maxIntervalAbsSpeakerSample[dtdNdx] = absSpeakerSample;

                if (absSpeakerSample > maxAbsSpeakerSample)
                {
                    maxAbsSpeakerSample = absSpeakerSample;
                }
            }

            if (++dtdCnt >= Constants.DTD_LEN)
            {
                dtdCnt = 0;

                // calculate maxAbsSpeakerSample
                maxAbsSpeakerSample = 0.0f;

                for (int i = 0; i < Constants.NLMS_LEN / Constants.DTD_LEN; ++i)
                {
                    if (maxIntervalAbsSpeakerSample[i] > maxAbsSpeakerSample)
                    {
                        maxAbsSpeakerSample = maxIntervalAbsSpeakerSample[i];
                    }
                }

                // rotate Ndx
                if (++dtdNdx >= Constants.NLMS_LEN / Constants.DTD_LEN)
                {
                    dtdNdx = 0;
                }

                maxIntervalAbsSpeakerSample[dtdNdx] = 0.0f;
            }

            // The Geigel DTD algorithm with Hangover timer Thold
            if (absMicrophoneSample >= Constants.GEIGEL_THRESHOLD * maxAbsSpeakerSample)
            {
                hangover = Constants.DTD_DEFAULT_HANGOVER;
            }

            if (hangover != 0)
            {
                --hangover;

                returnValue = true;
            }

            return returnValue;
        }
    }
}
