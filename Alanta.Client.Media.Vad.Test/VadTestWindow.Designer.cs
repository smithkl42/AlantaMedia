namespace Alanta.Client.Media.Vad.Test
{
	partial class VadTestWindow
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.buttonLoadRaw = new System.Windows.Forms.Button();
			this.arrayViewer = new ASV.ArrayViewer();
			this.buttonSaveToWav = new System.Windows.Forms.Button();
			this.buttonSaveAgc2Wav = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonLoadRaw
			// 
			this.buttonLoadRaw.Location = new System.Drawing.Point(12, 9);
			this.buttonLoadRaw.Name = "buttonLoadRaw";
			this.buttonLoadRaw.Size = new System.Drawing.Size(84, 21);
			this.buttonLoadRaw.TabIndex = 1;
			this.buttonLoadRaw.Text = "Load RAW";
			this.buttonLoadRaw.UseVisualStyleBackColor = true;
			this.buttonLoadRaw.Click += new System.EventHandler(this.buttonLoadRaw_Click);
			// 
			// arrayViewer
			// 
			this.arrayViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.arrayViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.arrayViewer.Location = new System.Drawing.Point(12, 36);
			this.arrayViewer.Name = "arrayViewer";
			this.arrayViewer.Size = new System.Drawing.Size(1268, 555);
			this.arrayViewer.TabIndex = 0;
			// 
			// buttonSaveToWav
			// 
			this.buttonSaveToWav.Location = new System.Drawing.Point(102, 9);
			this.buttonSaveToWav.Name = "buttonSaveToWav";
			this.buttonSaveToWav.Size = new System.Drawing.Size(119, 21);
			this.buttonSaveToWav.TabIndex = 2;
			this.buttonSaveToWav.Text = "Save to WAV";
			this.buttonSaveToWav.UseVisualStyleBackColor = true;
			this.buttonSaveToWav.Click += new System.EventHandler(this.buttonSaveToWav_Click);
			// 
			// buttonSaveAgc2Wav
			// 
			this.buttonSaveAgc2Wav.Location = new System.Drawing.Point(227, 9);
			this.buttonSaveAgc2Wav.Name = "buttonSaveAgc2Wav";
			this.buttonSaveAgc2Wav.Size = new System.Drawing.Size(179, 21);
			this.buttonSaveAgc2Wav.TabIndex = 3;
			this.buttonSaveAgc2Wav.Text = "Save AGC to WAV";
			this.buttonSaveAgc2Wav.UseVisualStyleBackColor = true;
			this.buttonSaveAgc2Wav.Click += new System.EventHandler(this.buttonSaveAgc2Wav_Click);
			// 
			// VadTestWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1292, 603);
			this.Controls.Add(this.buttonSaveAgc2Wav);
			this.Controls.Add(this.buttonSaveToWav);
			this.Controls.Add(this.buttonLoadRaw);
			this.Controls.Add(this.arrayViewer);
			this.Name = "VadTestWindow";
			this.Text = "VAD debugger";
			this.ResumeLayout(false);

		}

		#endregion

		private ASV.ArrayViewer arrayViewer;
		private System.Windows.Forms.Button buttonLoadRaw;
		private System.Windows.Forms.Button buttonSaveToWav;
		private System.Windows.Forms.Button buttonSaveAgc2Wav;
	}
}

