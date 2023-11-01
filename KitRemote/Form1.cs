using System.Drawing;

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
        }

        private void ScreenRefreshed(object sender, byte[] data)
        {
            //pictureBox1.Image = ScaleImage(Image.FromStream(new MemoryStream(data)), pictureBox1.Height, pictureBox1.Width);
            pictureBox1.Image = Image.FromStream(new MemoryStream(data));
        }

        private void OnFpsChange(int fps)
        {
            label1.Invoke(() =>
            {
                if (label1 is null)
                {
                    return;
                }
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
            //_recorder.Dispose();
        }

        private static Bitmap ScaleImage(Image originalImage, int newHeight, int newWidth)
        {
            var resizedImage = new Bitmap(newWidth, newHeight);

            using var graphics = Graphics.FromImage(resizedImage);
            
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;

            graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);

            return resizedImage;
        }
    }
}