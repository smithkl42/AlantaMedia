namespace ASV
{
	partial class ArrayViewerDialog
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
			this.arrayViewer = new ASV.ArrayViewer();
			this.SuspendLayout();
			// 
			// arrayViewer
			// 
			this.arrayViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.arrayViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.arrayViewer.Location = new System.Drawing.Point(0, 0);
			this.arrayViewer.Name = "arrayViewer";
			this.arrayViewer.Size = new System.Drawing.Size(718, 381);
			this.arrayViewer.TabIndex = 0;
			// 
			// ArrayViewerDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(718, 381);
			this.Controls.Add(this.arrayViewer);
			this.Name = "ArrayViewerDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "View array";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ArrayViewerDialog_KeyDown);
			this.ResumeLayout(false);

		}

		#endregion

		private ArrayViewer arrayViewer;
	}
}