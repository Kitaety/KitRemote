using System.Drawing;
using System.IO.Compression;
using KitRemote.Logic;
using SharpDX;

namespace KitRemote
{
	public partial class Form1 : Form
	{
		private readonly ScreenStateLogger _screenStateLogger;
		private readonly DisplayInfo[] _displays;
		public Form1()
		{
			InitializeComponent();
			_screenStateLogger = new ScreenStateLogger();

			_screenStateLogger.ScreenRefreshed += ScreenRefreshed;
			_screenStateLogger.FpsChange += OnFpsChange;

            _displays = _screenStateLogger.GetDisplays();


            comboBox1.Items.Clear();
			comboBox1.Items.AddRange(_displays.Select(display => display.DisplayName).ToArray());

            if (_displays.Any())
            {
                comboBox1.SelectedIndex = 0;
            }
        }
        private Image _img;

        private void ScreenRefreshed(object sender, byte[] data)
		{

            lock (pictureBox1)
            {
                _img = Image.FromStream(new MemoryStream(data));;
                pictureBox1.Image = _img;
            }
        }

        private void OnFpsChange(int fps)
		{
            lock (label1)
            {
                label1.Invoke(() =>
                {
                    if (label1 is null || label2 is null)
                    {
                        return;
                    }

                    using var ms = new MemoryStream();
                    _img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    var msb = ms.ToArray();
                    label2.Text = $"{msb.Length / 1024} KB";

                    using var gms = new MemoryStream();

                    using var zipStream = new GZipStream(gms, CompressionLevel.SmallestSize);
                    zipStream.Write(msb, 0, msb.Length);

                    var result = Convert.ToBase64String(gms.ToArray());

                    var byteCode = System.Text.Encoding.Default.GetByteCount(result);

                    label3.Text = $"{byteCode / 1024} KB";

                    label1.Text = fps.ToString();
                });
            }
			
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

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _screenStateLogger.SetDisplay(_displays[comboBox1.SelectedIndex]);
        }
    }
}