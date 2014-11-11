using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ASV
{
	public partial class ArrayViewerDialog : Form
	{
		/// <param name="title">nullable</param>
		public ArrayViewerDialog(string title)
		{
			InitializeComponent();
			if (title != null) this.Text = title;
		}
		public ArrayViewerContinousArray1D CreateAndAttachArray<T>(T[] array)
		{
			ArrayViewer.ScalarValues<T> values = new ArrayViewer.ScalarValues<T>(array);
			ArrayViewerHorizontalZoomScale scale = new ArrayViewerHorizontalZoomScale(ArrayViewerHorizontalZoomScale.MouseInputMode.zoomAndMove);
			arrayViewer.Create(scale);
			ArrayViewerContinousArray1D ret;
			arrayViewer.AttachArray(ret = new ArrayViewerContinousArray1D(new ArrayViewer.LinearScalarTransformScale(values), values));
			return ret;
		}
		public ArrayViewer AV
		{
			get
			{
				return arrayViewer;
			}
		}

		private void ArrayViewerDialog_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Escape: DialogResult = DialogResult.Cancel;
					break;
			}
		}
	}
}