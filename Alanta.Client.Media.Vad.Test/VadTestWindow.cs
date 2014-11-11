using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ASV;
using Alanta.Client.Media.Dsp;

namespace Alanta.Client.Media.Vad.Test
{
	public partial class VadTestWindow : Form
	{
		const int sampleRate = 16000;//22050;
		readonly ArrayViewerHorizontalZoomScale scale;
		public VadTestWindow()
		{
			InitializeComponent();

			scale = new ArrayViewerHorizontalZoomScale(ArrayViewerHorizontalZoomScale.MouseInputMode.syncMoveEntireArea);
			arrayViewer.Create(scale);


			bytes = File.ReadAllBytes(@"E:\ASV\Prog\My Projects\110511 Alanta\Ken Smith AGC Test 2011-06-02.raw");
			OnLoadedRawFile();

		}

		byte[] bytes;
		private void buttonLoadRaw_Click(object sender, EventArgs e)
		{
			var dlg = new OpenFileDialog();
			if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				bytes = File.ReadAllBytes(dlg.FileName);
				OnLoadedRawFile();
			}
		}
		short[] Samples
		{
			get
			{
				var samples = new short[bytes.Length / 2];
				Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
				return samples;
			}
		}
		void OnLoadedRawFile()
		{
			short[] samples = Samples;
			arrayViewer.DetachAllArrays();

			var values = new ArrayViewer.ScalarValues<short>(samples);
			var valuesA = new ArrayViewerContinousArray1D(new ArrayViewer.LinearScalarTransformScale(values), values);
			ArrayViewerScalarTransformScaleDisplay1D scaleDisplay = new ArrayViewerScalarTransformScaleDisplay1D(valuesA.TransformScale as ArrayViewer.LinearScalarTransformScale);
			scaleDisplay.getHorisontalLabelText = (position) =>
			{
				return String.Format("{0:f3}s", position * samples.Length / sampleRate);
			};
			arrayViewer.AttachArray(scaleDisplay);
			arrayViewer.AttachArray(valuesA);
			SimulateVadAndAgc();
		}
		short[] SimulateVadAndAgc()
		{
			short[] samples = Samples;
			short[] agcOutput = new short[samples.Length];
			int samplesperFrame = 20 * sampleRate / 1000;
			VoiceActivityDetector vad = new VoiceActivityDetector(VoiceActivityDetector.Aggressiveness.Normal);
			int framesCount = samples.Length / samplesperFrame;

			var voiceDetected = new double[framesCount];

			//WebRtc.VAD webRtcVad = new WebRtc.VAD(0);// unmanaged code


			for (int frame = 0; frame < framesCount; frame++)
			{
				short[] f = new short[samplesperFrame];
				for (int i = 0; i < samplesperFrame; i++) f[i] = samples[frame * samplesperFrame + i];

				var decision = vad.WebRtcVad_CalcVad16khz(f, samplesperFrame);
				voiceDetected[frame] = decision != 0 ? 0.9 : 0;

			}

			var ts = new ArrayViewer.LinearScalarTransformScale(0, 1);

			arrayViewer.AttachArray(new ArrayViewerContinousArray1D(ts, new ArrayViewer.ScalarValues<double>(voiceDetected))
			{
				ArrayPen = new Pen(Color.Red)
			});


			return agcOutput;
		}

		private void buttonSaveToWav_Click(object sender, EventArgs e)
		{
			if (bytes == null)
			{
				MessageBox.Show("no RAW data is loaded");
				return;
			}

			SaveFileDialog dlg = new SaveFileDialog() { Filter = "WAV files|*.wav" };
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				FileStream wavFile = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
				var writer = new BinaryWriter(wavFile, Encoding.ASCII);
				writer.Write('R');
				writer.Write('I');
				writer.Write('F');
				writer.Write('F');
				writer.Write((int)40); // wave data size + 40 is to be written at this position

				writer.Write('W');
				writer.Write('A');
				writer.Write('V');
				writer.Write('E');
				writer.Write('f');
				writer.Write('m');
				writer.Write('t');
				writer.Write(' ');
				writer.Write((int)16); //format_chunk_length			
				writer.Write((short)1); // wFormatTag
				writer.Write((short)1); // nChannels
				writer.Write((int)sampleRate); // nSamplesPerSec
				writer.Write((int)sampleRate * 2); // nAvgBytesPerSec
				writer.Write((short)2); // nBlockAlign
				writer.Write((short)16); // wBitsPerSample

				writer.Write('d');
				writer.Write('a');
				writer.Write('t');
				writer.Write('a');
				writer.Write((int)0); // wave data size is to be written at this position	
				writer.Flush();

				writer.Write(bytes, 0, bytes.Length);

				// finalize wav header
				wavFile.Seek(40, SeekOrigin.Begin);
				writer.Write(bytes.Length);
				writer.Flush();
				wavFile.Seek(4, SeekOrigin.Begin);
				writer.Write(bytes.Length + 40);
				writer.Flush();

				//writer.Close();
				wavFile.Flush();
				wavFile.Close();
			}
		}

		private void buttonSaveAgc2Wav_Click(object sender, EventArgs e)
		{
			if (bytes == null)
			{
				MessageBox.Show("no RAW data is loaded");
				return;
			}


			SaveFileDialog dlg = new SaveFileDialog() { Filter = "WAV files|*.wav" };
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				FileStream wavFile = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
				var writer = new BinaryWriter(wavFile, Encoding.ASCII);
				writer.Write('R');
				writer.Write('I');
				writer.Write('F');
				writer.Write('F');
				writer.Write((int)40); // wave data size + 40 is to be written at this position

				writer.Write('W');
				writer.Write('A');
				writer.Write('V');
				writer.Write('E');
				writer.Write('f');
				writer.Write('m');
				writer.Write('t');
				writer.Write(' ');
				writer.Write((int)16); //format_chunk_length			
				writer.Write((short)1); // wFormatTag
				writer.Write((short)1); // nChannels
				writer.Write((int)sampleRate); // nSamplesPerSec
				writer.Write((int)sampleRate * 2); // nAvgBytesPerSec
				writer.Write((short)2); // nBlockAlign
				writer.Write((short)16); // wBitsPerSample

				writer.Write('d');
				writer.Write('a');
				writer.Write('t');
				writer.Write('a');
				writer.Write((int)0); // wave data size is to be written at this position	
				writer.Flush();

				var agcOutput = SimulateVadAndAgc();
				var agcOutputBytes = new byte[agcOutput.Length * 2];
				Buffer.BlockCopy(agcOutput, 0, agcOutputBytes, 0, agcOutputBytes.Length);
				writer.Write(agcOutputBytes, 0, agcOutputBytes.Length);

				// finalize wav header
				wavFile.Seek(40, SeekOrigin.Begin);
				writer.Write(agcOutputBytes.Length);
				writer.Flush();
				wavFile.Seek(4, SeekOrigin.Begin);
				writer.Write(agcOutputBytes.Length + 40);
				writer.Flush();

				//writer.Close();
				wavFile.Flush();
				wavFile.Close();
			}

		}
	}
}
