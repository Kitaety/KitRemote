using System.Drawing;
using System.IO.Compression;

namespace KitRemote
{
	public partial class Form1 : Form
	{
		private readonly ScreenStateLogger _screenStateLogger;
		public Form1()
		{
			InitializeComponent();
			_screenStateLogger = new ScreenStateLogger();

			_screenStateLogger.ScreenRefreshed += ScreenRefreshed;
			_screenStateLogger.FpsChange += OnFpsChange;

			comboBox1.Items.Clear();
			comboBox1.Items.AddRange(_screenStateLogger.GetAdapters().ToArray());
			comboBox1.SelectedIndex = 0;

			InitOutputsCombobox();
		}

		private void InitOutputsCombobox()
		{
			comboBox2.Items.Clear();
			comboBox2.Text = string.Empty;
			comboBox2.Items.AddRange(_screenStateLogger.GetOutputs().ToArray());

			if (comboBox2.Items.Count > 0)
			{
				comboBox2.SelectedIndex = 0;
			}
		}

		private void ScreenRefreshed(object sender, byte[] data)
		{
			pictureBox1.Image = Image.FromStream(new MemoryStream(data));
		}

		private void OnFpsChange(int fps)
		{
			label1.Invoke(() =>
			{
				if (label1 is null || label2 is null)
				{
					return;
				}

				using var ms = new MemoryStream();
				pictureBox1.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
				var msb = ms.ToArray();
				label2.Text = $"{msb.Length / 1024} KB";

				using var gms = new MemoryStream();

				using GZipStream zipStream = new GZipStream(gms, CompressionLevel.SmallestSize);
				zipStream.Write(msb, 0, msb.Length);

				label3.Text = $"{gms.ToArray().Length / 1024} KB";

				label1.Text = fps.ToString();
			});
		}

		private void button1_Click(object sender, EventArgs e)
		{
			_screenStateLogger.Start();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			_screenStateLogger.Stop();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			_screenStateLogger.Stop();
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			_screenStateLogger.SetAdapter(comboBox1.SelectedIndex);
			InitOutputsCombobox();
		}

		private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			_screenStateLogger.SetOutput(comboBox2.SelectedIndex);
		}
	}
}