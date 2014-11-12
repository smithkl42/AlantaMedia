using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using Alanta.Client.Common.Logging;
using Alanta.Client.Ui.Controls.AudioVisualizer;

namespace Alanta.Client.Media.Tests.Media.MediaServer
{
	public class MultipleControllerAudioMediaStreamSource : AudioMediaStreamSource
	{
		public MultipleControllerAudioMediaStreamSource(int expectedFrequency)
			: base(null, AudioFormat.Default)
		{
			AudioControllers = new List<AudioControllerEntry>();
			for (int i = 0; i < FourierTransform.MeterFrequencies.Length; i++)
			{
				if (FourierTransform.MeterFrequencies[i] >= expectedFrequency)
				{
					_frequencyIndex = i;
					break;
				}
			}
		}

		public List<AudioControllerEntry> AudioControllers { get; private set; }
		private readonly int _frequencyIndex;
		public bool UseGeneratedTone { get; set; }

		protected override void GetSampleAsync(MediaStreamType mediaStreamType)
		{
			try
			{
				_logger.LogSampleRequested();

				var resetEvents = new AutoResetEvent[AudioControllers.Count];

				// Dequeue the sample from the media controllers, so that each controller's entire audio stack gets exercised.
				for (int i = 0; i < AudioControllers.Count; i++)
				{
					var audioControllerEntry = AudioControllers[i];
					resetEvents[i] = audioControllerEntry.ResetEvent;

					// Throw the decoding work into a thread pool.
					ThreadPool.QueueUserWorkItem(obj =>
					{
						var entry = (AudioControllerEntry)obj;
						entry.MediaController.GetNextAudioFrame(ms =>
						{
							try
							{
								entry.LastMemoryStream = ms;

								// Check to see if the frame has the expected frequency, and if not, update a counter.
								if (!UseGeneratedTone) return;
								if (entry.WriteToBuffer(entry.LastMemoryStream))
								{
									entry.Clear();
									byte[] frequencies = PeakMeter.CalculateFrequencies(entry.SampleBuffer, AudioFormat.Default.SamplesPerSecond);
									bool frequencyMismatch = false;
									if (frequencies[_frequencyIndex] == 0)
									{
										frequencyMismatch = true;
									}
									else
									{
										if (frequencies.Where((t, j) => j != _frequencyIndex && t > frequencies[_frequencyIndex]).Any())
										{
											frequencyMismatch = true;
										}
									}
									entry.LogFrequencyCounterMismatch(frequencyMismatch);
								}
							}
							catch (Exception ex)
							{
								ClientLogger.Debug(ex.ToString);
							}
							finally
							{
								entry.ResetEvent.Set();
							}
						});

					}, audioControllerEntry);

				}

				// Wait for all the decoding jobs to finish
				WaitHandle.WaitAll(resetEvents);

				// Submit only the first sample retrieved to be played.
				var rawSample = AudioControllers[0].LastMemoryStream;

				var sample = new MediaStreamSample(
					_mediaStreamDescription,
					rawSample,
					0,
					rawSample.Length,
					(DateTime.Now - _startTime).Ticks,
					_emptySampleDict);
				ReportGetSampleCompleted(sample);
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString());
			}
		}
	}

	public class AudioControllerEntry
	{
		public AudioControllerEntry(MediaController mediaController)
		{
			SampleBuffer = new short[bufferLength];
			MediaController = mediaController;
			FrequencyMismatchCounter = MediaController.MediaStats.RegisterCounter("Audio:FrequencyMismatch");
			ResetEvent = new AutoResetEvent(false);
		}
		public MediaController MediaController { get; set; }
		public short[] SampleBuffer { get; private set; }
		public int BufferPosition { get; private set; }
		public Counter FrequencyMismatchCounter { get; private set; }
		private const int bufferLength = AudioVisualizerConstants.TransformBufferSize;
		private byte[] byteBuffer = new byte[1024];
		public AutoResetEvent ResetEvent { get; private set; }
		public MemoryStream LastMemoryStream { get; set; }

		public bool WriteToBuffer(MemoryStream memoryStream)
		{
			var lengthInBytes = (int)Math.Min((bufferLength - BufferPosition) * sizeof(short), memoryStream.Length);
			memoryStream.Read(byteBuffer, 0, lengthInBytes);
			memoryStream.Seek(0, SeekOrigin.Begin);
			Buffer.BlockCopy(byteBuffer, 0, SampleBuffer, BufferPosition * sizeof(short), lengthInBytes);
			BufferPosition += lengthInBytes / sizeof(short);
			return BufferPosition >= bufferLength;
		}

		public void Clear()
		{
			BufferPosition = 0;
		}

		private int _recentMismatches;
		private long _frameCount;

		public void LogFrequencyCounterMismatch(bool isMismatched)
		{
			if (isMismatched)
			{
				_recentMismatches++;
			}
			if (++_frameCount % 50 == 0)
			{
				FrequencyMismatchCounter.Update(_recentMismatches);
				_recentMismatches = 0;
			}
		}

	}
}
