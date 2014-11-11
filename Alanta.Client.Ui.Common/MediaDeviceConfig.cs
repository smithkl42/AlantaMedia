using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Ui.Controls
{
	public class MediaDeviceConfig
	{
		private class Device
		{
			public int Priority { get; set; }
			public string FriendlyNameMatch { get; set; }
		}

		public string AudioCaptureDeviceFriendlyName { get; set; }
		public string VideoCaptureDeviceFriendlyName { get; set; }
		public const string DefaultFileName = "MediaDeviceConfig.xml";
		public const string PriorityFileName = "MediaDevicePriority.xml";
		private static IEnumerable<Device> preferredAudioDevices;
		private static IEnumerable<Device> preferredVideoDevices;

		/// <summary>
		/// Handles various housekeeping tasks related to selecting the best audio and video devices.
		/// The GetDefaultXXXCaptureDevice() method doesn't always return the best results on some 
		/// configurations, especially Macintoshes, so we need to add a bit more intelligence
		/// to the webcam/microphone selection process.
		/// </summary>
		public MediaDeviceConfig()
		{
			var uri = new Uri("/Alanta.Client.UI.Common;component/Resources/MediaDevicePriority.xml", UriKind.Relative);
			var sr = Application.GetResourceStream(uri);
			if (sr != null)
			{
				XDocument doc = XDocument.Load(sr.Stream);
				preferredAudioDevices = (from device in doc.Descendants("Devices").Descendants("AudioCaptureDevices").Descendants("Device")
										 let xAttribute = device.Attribute("Priority")
										 where xAttribute != null
										 let attribute = device.Attribute("FriendlyNameMatch")
										 where attribute != null
										 select new Device
										 {
											 Priority = Convert.ToInt32(xAttribute.Value),
											 FriendlyNameMatch = attribute.Value
										 }).OrderByDescending(d => d.Priority).ToList();
				preferredVideoDevices = (from device in doc.Descendants("Devices").Descendants("VideoCaptureDevices").Descendants("Device")
										 let xAttribute1 = device.Attribute("Priority")
										 where xAttribute1 != null
										 let attribute1 = device.Attribute("FriendlyNameMatch")
										 where attribute1 != null
										 select new Device
										 {
											 Priority = Convert.ToInt32(xAttribute1.Value),
											 FriendlyNameMatch = attribute1.Value
										 }).OrderByDescending(d => d.Priority).ToList();
			}
			else
			{
				throw new FileNotFoundException(string.Format("The media device configuration resource {0} was not found.", uri));
			}
		}

		/// <summary>
		/// Returns the best audio capture device based on a variety of heuristics.
		/// </summary>
		/// <returns>The best microphone to use.</returns>
		public AudioCaptureDevice SelectBestAudioCaptureDevice()
		{
			var device = (GetLastAudioDevice() ?? GetPreferredAudioDevice()) ?? CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice();
			return device;
		}

		/// <summary>
		/// Returns the best video capture device based on a variety of heuristics.
		/// </summary>
		/// <returns>The best webcam to use.</returns>
		public VideoCaptureDevice SelectBestVideoCaptureDevice()
		{
			var device = (GetLastVideoDevice() ?? GetPreferredVideoDevice()) ?? CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();
			return device;
		}

		/// <summary>
		/// The idea here is to select a format which is closest to the format we actually want, 
		/// and allows us to do the simplest possible downsampling (or none at all).
		/// </summary>
		/// <param name="device">The selected audio capture device</param>
		public static void SelectBestAudioFormat(AudioCaptureDevice device)
		{
			if (device != null && device.SupportedFormats.Count > 0)
			{
				// Some devices return a "SamplesPerSecond" of 0 at this stage. Damn Microsoft.
				var possibleAudioFormats = device.SupportedFormats.Where(format =>
											format.BitsPerSample == AudioConstants.BitsPerSample &&
												format.WaveFormat == WaveFormatType.Pcm).ToList();

				var formats = new StringBuilder();
				foreach (var format in device.SupportedFormats)
				{
					formats.AppendFormat("BitsPerSample={0}, Channels={1}, SamplesPerSecond={2}\r\n", format.BitsPerSample, format.Channels, format.SamplesPerSecond);
				}
				ClientLogger.Debug("Possible audio formats: " + formats);

				// This will select any format that is an exact match of the desired format.
				var bestAudioFormat = possibleAudioFormats
					.FirstOrDefault(format => format.SamplesPerSecond == AudioConstants.WidebandSamplesPerSecond &&
						format.Channels == AudioConstants.Channels &&
						format.BitsPerSample == AudioConstants.BitsPerSample);

				// This will prefer formats that are exact multiples of the desired format, and which have the same number of channels.
				if (bestAudioFormat == null)
				{
					bestAudioFormat = possibleAudioFormats
						.OrderBy(format =>
							(format.SamplesPerSecond != 0)
							? (format.SamplesPerSecond % AudioConstants.WidebandSamplesPerSecond) + format.Channels - AudioConstants.Channels
							: int.MaxValue)
						.FirstOrDefault();
				}
				Debug.Assert(bestAudioFormat != null, "No appropriate audio format was found; possible formats = \r\n" + formats);
				ClientLogger.Debug("Selected audio format: BitsPerSample={0}, Channels={1}, SamplesPerSecond={2}", bestAudioFormat.BitsPerSample, bestAudioFormat.Channels, bestAudioFormat.SamplesPerSecond);
				device.DesiredFormat = bestAudioFormat;
			}
			else
			{
				ClientLogger.Debug("No audio capture device was found.");
			}
		}

		public static void SelectBestVideoFormat(VideoCaptureDevice device)
		{
			// Handle various error conditions.
			if (device == null)
			{
				ClientLogger.Debug("No video capture device was found.");
				return;
			}
			//if (device.SupportedFormats.Count == 0)
			//{
			//    ClientLogger.Debug("The VideoCaptureDevice.SupportedFormats collection was empty.");
			//    return;
			//}

			var possibleVideoFormats = (from format in device.SupportedFormats
										where // Convert.ToInt16(format.FramesPerSecond) >= VideoConstants.AcceptFramesPerSecond && 
											format.PixelHeight >= VideoConstants.Height &&
											format.PixelWidth >= VideoConstants.Width
										select format).ToList();
			var formats = new StringBuilder();
			formats.Append(string.Format("device={0}, possible formats={1}\r\n", device.FriendlyName, device.SupportedFormats.Count()));
			foreach (var videoFormat in device.SupportedFormats)
			{
				formats.AppendFormat("Frames={0}, Height={1}, Width={2}, Stride={3}\r\n", videoFormat.FramesPerSecond, videoFormat.PixelHeight, videoFormat.PixelWidth, videoFormat.Stride);
			}
			ClientLogger.Debug("Possible video formats: \r\n" + formats);
			VideoFormat bestVideoFormat;
			if (possibleVideoFormats.Any())
			{
				bestVideoFormat = possibleVideoFormats
					.OrderBy(format => Math.Abs(format.PixelHeight - VideoConstants.Height) + Math.Abs(format.PixelWidth - VideoConstants.Width))
					.FirstOrDefault();
				Debug.Assert(bestVideoFormat != null, "No appropriate video format was found; possible formats=\r\n" + formats); // 
			}
			else
			{
				bestVideoFormat = new VideoFormat(PixelFormatType.Unknown, VideoConstants.Width, VideoConstants.Height, 5);
				ClientLogger.Debug("A video capture device was present, but it didn't present itself with any matching formats. This is normal for Macs. Trying to guess at one.");
			}
			device.DesiredFormat = bestVideoFormat;
			ClientLogger.Debug("Selected video format: Frames={0}, Height={1}, Width={2}, Stride={3}",
				bestVideoFormat.FramesPerSecond, bestVideoFormat.PixelHeight, bestVideoFormat.PixelWidth, bestVideoFormat.Stride);
		}

		private AudioCaptureDevice GetLastAudioDevice()
		{
			// return null; // For testing!!!
			return CaptureDeviceConfiguration.GetAvailableAudioCaptureDevices().FirstOrDefault(d => d.FriendlyName == AudioCaptureDeviceFriendlyName);
		}

		private VideoCaptureDevice GetLastVideoDevice()
		{
			// return null; // For testing!!!
			return CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices().FirstOrDefault(d => d.FriendlyName == VideoCaptureDeviceFriendlyName);
		}

		private static AudioCaptureDevice GetPreferredAudioDevice()
		{
			var localDevices = CaptureDeviceConfiguration.GetAvailableAudioCaptureDevices();
			foreach (var preferredDevice in preferredAudioDevices)
			{
				var re = new Regex(preferredDevice.FriendlyNameMatch);
				foreach (var localDevice in localDevices)
				{
					ClientLogger.Debug("Checking preferred audio device {0} against local device {1}", preferredDevice.FriendlyNameMatch, localDevice.FriendlyName);
					if (re.IsMatch(localDevice.FriendlyName))
					{
						ClientLogger.Debug("Preferred audio device {0} matches local device {1}", preferredDevice.FriendlyNameMatch, localDevice.FriendlyName);
						return localDevice;
					}
				}
			}
			return null;
		}

		private static VideoCaptureDevice GetPreferredVideoDevice()
		{
			var localDevices = CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices();
			foreach (var preferredDevice in preferredVideoDevices)
			{
				var re = new Regex(preferredDevice.FriendlyNameMatch);
				foreach (var localDevice in localDevices)
				{
					ClientLogger.Debug("Checking preferred video device {0} against local device {1}", preferredDevice.FriendlyNameMatch, localDevice.FriendlyName);
					if (re.IsMatch(localDevice.FriendlyName))
					{
						ClientLogger.Debug("Preferred video device {0} matches local device {1}", preferredDevice.FriendlyNameMatch, localDevice.FriendlyName);
						return localDevice;
					}
				}
			}
			return null;
		}


	}
}
