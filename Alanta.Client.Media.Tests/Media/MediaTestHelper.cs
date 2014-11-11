using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Alanta.Client.Test.Media
{
	public static class MediaTestHelper
	{
		public static void Load(this ICollection<byte[]> buffer, string filter, int bytesPerFrame)
		{
			try
			{
				var dlg = new OpenFileDialog { Filter = filter, FilterIndex = 1 };
				bool? userClickedOk = dlg.ShowDialog();
				if (userClickedOk == true)
				{
					using (Stream fs = dlg.File.OpenRead())
					{
						buffer.Clear();
						while (fs.Position < fs.Length - bytesPerFrame)
						{
							var frame = new byte[bytesPerFrame];
							fs.Read(frame, 0, frame.Length);
							buffer.Add(frame);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		public static void Save(this IEnumerable<byte[]> buffer, string filter)
		{
			try
			{
				var dlg = new SaveFileDialog { Filter = filter, FilterIndex = 1 };
				bool? userClickedOk = dlg.ShowDialog();
				if (userClickedOk == true)
				{
					using (Stream fs = dlg.OpenFile())
					{
						foreach (var frame in buffer)
						{
							fs.Write(frame, 0, frame.Length);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		public static void SaveToCsv<T>(this IEnumerable<T> values, string header, Func<T, string> formatter)
		{
			var dlg = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*", FilterIndex = 1 };
			bool? userClickedOk = dlg.ShowDialog();
			if (userClickedOk == true)
			{
				using (Stream fs = dlg.OpenFile())
				{
					byte[] bheader = Encoding.UTF8.GetBytes(header);
					fs.Write(bheader, 0, bheader.Length);
					foreach (var result in values)
					{
						string line = formatter(result);
						byte[] bline = Encoding.UTF8.GetBytes(line);
						fs.Write(bline, 0, bline.Length);
					}
				}
			}
		}
	}
}
