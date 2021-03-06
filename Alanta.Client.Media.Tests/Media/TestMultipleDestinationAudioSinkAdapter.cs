﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using Alanta.Client.Test.Media;

namespace Alanta.Client.Media.Tests.Media
{
	public class TestMultipleDestinationAudioSinkAdapter : AudioSinkAdapter
	{
		public TestMultipleDestinationAudioSinkAdapter(
			CaptureSource captureSource, 
			SourceMediaController mediaController, 
			Dictionary<Guid, DestinationMediaController> mediaControllers, 
			MediaConfig mediaConfig)
			: base(captureSource, mediaController, mediaConfig, new TestMediaEnvironment(), AudioFormat.Default)
		{
			_mediaControllers = mediaControllers;
		}

	    private readonly Dictionary<Guid, DestinationMediaController> _mediaControllers;

		protected override void SubmitFrame(AudioContext ctx, byte[] frame)
		{
			base.SubmitFrame(ctx, frame);
			foreach (DestinationMediaController testMediaController in _mediaControllers.Values)
			{
				// Make a copy so we don't run into threading issues.
				var copy = new byte[frame.Length];
				Buffer.BlockCopy(frame, 0, copy, 0, frame.Length);
				testMediaController.SubmitRecordedFrame(ctx, copy);
			}
		}
	}
}
