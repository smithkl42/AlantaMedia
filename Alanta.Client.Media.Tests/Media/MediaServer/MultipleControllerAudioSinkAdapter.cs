using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using AudioFormat = Alanta.Client.Media.AudioFormat;
using wmAudioFormat = System.Windows.Media.AudioFormat;

namespace Alanta.Client.Test.Media.MediaServer
{
	public class MultipleControllerAudioSinkAdapter : AudioSink
	{
		#region Constructors

		public MultipleControllerAudioSinkAdapter(MediaConfig mediaConfig, CaptureSource captureSource, int frequency)
		{
			ClientLogger.Debug("MultipleControllerAudioSinkAdapter created");
			this.mediaConfig = mediaConfig;
			AudioControllers = new List<IAudioController>();
			AudioContexts = new List<AudioContext>();
			CaptureSource = captureSource;
			RawAudioFormat = new AudioFormat(CaptureSource.AudioCaptureDevice.DesiredFormat.SamplesPerSecond);
			oscillator = new Oscillator();
			oscillator.Frequency = frequency;
		}

		#endregion

		#region Fields and Properties

		public List<IAudioController> AudioControllers { get; private set; }
		public List<AudioContext> AudioContexts { get; private set; }
		public bool UseGeneratedTone { get; set; }
		protected wmAudioFormat wmAudioFormat;
		public AudioFormat RawAudioFormat { get; private set; }
		public AudioContext RootAudioContext { get; set; }
		protected ushort outputBytesPerSample = (AudioConstants.BitsPerSample * AudioConstants.Channels) / 8;
		protected bool dataReceived;
		public event EventHandler CaptureSuccessful;
		private readonly Oscillator oscillator;
		private readonly MediaConfig mediaConfig;
		// private TestAudioContextFactory audioContextFactory;
		public int Rooms { get; set; }
		public int ConnectionsPerRoom { get; set; }

		#endregion

		protected override void OnCaptureStarted()
		{
			// Open an RTP stream.
			ClientLogger.Debug("AudioSinkAdapter: capture started.");
		}

		protected override void OnCaptureStopped()
		{
			ClientLogger.Debug("Audio capture stopped.");
		}

		protected override void OnFormatChange(wmAudioFormat audioFormat)
		{
			// We may need to do more with this.
			ClientLogger.Debug("The audio format was changed: BitsPerSample = {0}, Channels = {1}, SamplesPerSecond = {2}",
				AudioConstants.BitsPerSample, AudioConstants.Channels, audioFormat.SamplesPerSecond);
			if (audioFormat.WaveFormat != WaveFormatType.Pcm ||
				audioFormat.BitsPerSample != AudioConstants.BitsPerSample)
			{
				throw new ArgumentException("The audio format was not supported.");
			}
			wmAudioFormat = audioFormat;
			RawAudioFormat = new AudioFormat(audioFormat.SamplesPerSecond);
		}

		protected override void OnSamples(long sampleTime, long sampleDuration, byte[] sampleData)
		{
			try
			{
				// Raise an event if we managed to successfully capture data.
				if (!dataReceived)
				{
					dataReceived = true;
					if (CaptureSuccessful != null)
					{
						CaptureSuccessful(this, new EventArgs());
					}
				}

				RootAudioContext.Resampler.Write(sampleData);

				bool moreFrames;
				do
				{
					if (RootAudioContext.Resampler.Read(RootAudioContext.ResampleBuffer, out moreFrames))
					{
						// Hack to create single-tone signal - I'm sure there are better places to do this. So sue me.
						if (UseGeneratedTone)
						{
							for (int i = 0; i < RootAudioContext.ResampleBuffer.Length; i++)
							{
								short nextSample = oscillator.GetNextSample();
								RootAudioContext.ResampleBuffer[i] = (byte)nextSample;
								RootAudioContext.ResampleBuffer[++i] = (byte)(nextSample >> 8);
							}
						}

						var resetEvents = new AutoResetEvent[AudioControllers.Count];
						for (int i = 0; i < AudioControllers.Count; i++)
						{
							// Throw the actual encoding and sending out onto a separate thread, so we can take advantage of multiple processors.
							resetEvents[i] = new AutoResetEvent(false);
							ThreadPool.QueueUserWorkItem(obj =>
							{
								var index = (int)obj;
								try
								{
									var copy = new byte[RootAudioContext.ResampleBuffer.Length];
									Buffer.BlockCopy(RootAudioContext.ResampleBuffer, 0, copy, 0, RootAudioContext.ResampleBuffer.Length);
									var submitCtx = AudioContexts[index];
									AudioControllers[index].SubmitRecordedFrame(submitCtx, copy);
								}
								catch (Exception ex)
								{
									ClientLogger.Debug(ex.ToString);
								}
								finally
								{
									resetEvents[index].Set();
								}
							}, i);
						}
						WaitHandle.WaitAll(resetEvents);
					}
				} while (moreFrames);
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.Message);
			}
		}

	}
}
