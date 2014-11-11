using System;

namespace Alanta.Client.Media.Jpeg
{
    /// <summary>
    /// Provides a quick (if somewhat confusing) way to range limit typical int values to byte ranges (0-255).
    /// </summary>
    public static class ByteRangeLimiter
    {
        static ByteRangeLimiter()
        {
            BuildRangeLimitTable();
        }

        /// <summary>
        /// Range limit table for quickly normalizing values.
        /// </summary>
        public static byte[] Table;

        /// <summary>
        /// Allows for negative indices into the range limit table.
        /// </summary>
        public const int TableOffset = JpegConstants.MAXJSAMPLE + 1;

        /// <summary>
        /// Allocate and fill in the sample_range_limit table.
        /// 
        /// Several decompression processes need to range-limit values to the range
        /// 0..MAXJSAMPLE; the input value may fall somewhat outside this range
        /// due to noise introduced by quantization, roundoff error, etc. These
        /// processes are inner loops and need to be as fast as possible. On most
        /// machines, particularly CPUs with pipelines or instruction prefetch,
        /// a (subscript-check-less) C table lookup
        ///     x = sample_range_limit[x];
        /// is faster than explicit tests
        /// <c>
        ///     if (x &amp; 0)
        ///        x = 0;
        ///     else if (x > MAXJSAMPLE)
        ///        x = MAXJSAMPLE;
        /// </c>
        /// These processes all use a common table prepared by the routine below.
        /// 
        /// For most steps we can mathematically guarantee that the initial value
        /// of x is within MAXJSAMPLE + 1 of the legal range, so a table running from
        /// -(MAXJSAMPLE + 1) to 2 * MAXJSAMPLE + 1 is sufficient.  But for the initial
        /// limiting step (just after the IDCT), a wildly out-of-range value is 
        /// possible if the input data is corrupt.  To avoid any chance of indexing
        /// off the end of memory and getting a bad-pointer trap, we perform the
        /// post-IDCT limiting thus: <c>x = range_limit[x &amp; MASK];</c>
        /// where MASK is 2 bits wider than legal sample data, ie 10 bits for 8-bit
        /// samples.  Under normal circumstances this is more than enough range and
        /// a correct output will be generated; with bogus input data the mask will
        /// cause wraparound, and we will safely generate a bogus-but-in-range output.
        /// For the post-IDCT step, we want to convert the data from signed to unsigned
        /// representation by adding CENTERJSAMPLE at the same time that we limit it.
        /// So the post-IDCT limiting table ends up looking like this:
        /// <pre>
        ///     CENTERJSAMPLE, CENTERJSAMPLE + 1, ..., MAXJSAMPLE,
        ///     MAXJSAMPLE (repeat 2 * (MAXJSAMPLE + 1) - CENTERJSAMPLE times),
        ///     0          (repeat 2 * (MAXJSAMPLE + 1) - CENTERJSAMPLE times),
        ///     0, 1, ..., CENTERJSAMPLE - 1
        /// </pre>
        /// Negative inputs select values from the upper half of the table after
        /// masking.
        /// 
        /// We can save some space by overlapping the start of the post-IDCT table
        /// with the simpler range limiting table.  The post-IDCT table begins at
        /// sample_range_limit + CENTERJSAMPLE.
        /// 
        /// Note that the table is allocated in near data space on PCs; it's small
        /// enough and used often enough to justify this.
        /// </summary>
        private static void BuildRangeLimitTable()
        {
            Table = new byte[5 * (JpegConstants.MAXJSAMPLE + 1) + JpegConstants.CENTERJSAMPLE];
            int tableOffset = TableOffset;

            /* First segment of "simple" table: limit[x] = 0 for x < 0 */
            Array.Clear(Table, 0, JpegConstants.MAXJSAMPLE + 1);

            /* Main part of "simple" table: limit[x] = x */
            for (int i = 0; i <= JpegConstants.MAXJSAMPLE; i++)
            {
                Table[tableOffset + i] = (byte)i;
            }

            tableOffset += JpegConstants.CENTERJSAMPLE; /* Point to where post-IDCT table starts */

            /* End of simple table, rest of first half of post-IDCT table */
            for (int i = JpegConstants.CENTERJSAMPLE; i < 2 * (JpegConstants.MAXJSAMPLE + 1); i++)
                Table[tableOffset + i] = JpegConstants.MAXJSAMPLE;

            /* Second half of post-IDCT table */
            Array.Clear(Table, tableOffset + 2 * (JpegConstants.MAXJSAMPLE + 1), 2 * (JpegConstants.MAXJSAMPLE + 1) - JpegConstants.CENTERJSAMPLE);
            Array.Copy(Table, 0, Table, tableOffset + 4 * (JpegConstants.MAXJSAMPLE + 1) - JpegConstants.CENTERJSAMPLE, JpegConstants.CENTERJSAMPLE);
        }


    }
}
