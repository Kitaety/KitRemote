namespace KitRemote
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			button1 = new Button();
			button2 = new Button();
			pictureBox1 = new PictureBox();
			label1 = new Label();
			label2 = new Label();
			label3 = new Label();
			comboBox1 = new ComboBox();
			comboBox2 = new ComboBox();
			((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
			SuspendLayout();
			// 
			// button1
			// 
			button1.Location = new Point(22, 21);
			button1.Margin = new Padding(3, 2, 3, 2);
			button1.Name = "button1";
			button1.Size = new Size(82, 22);
			button1.TabIndex = 0;
			button1.Text = "button1";
			button1.UseVisualStyleBackColor = true;
			button1.Click += button1_Click;
			// 
			// button2
			// 
			button2.Location = new Point(125, 21);
			button2.Margin = new Padding(3, 2, 3, 2);
			button2.Name = "button2";
			button2.Size = new Size(82, 22);
			button2.TabIndex = 1;
			button2.Text = "button2";
			button2.UseVisualStyleBackColor = true;
			button2.Click += button2_Click;
			// 
			// pictureBox1
			// 
			pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			pictureBox1.Location = new Point(20, 58);
			pictureBox1.Margin = new Padding(3, 2, 3, 2);
			pictureBox1.Name = "pictureBox1";
			pictureBox1.Size = new Size(976, 270);
			pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
			pictureBox1.TabIndex = 2;
			pictureBox1.TabStop = false;
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(795, 23);
			label1.Name = "label1";
			label1.Size = new Size(38, 15);
			label1.TabIndex = 3;
			label1.Text = "label1";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(876, 23);
			label2.Name = "label2";
			label2.Size = new Size(38, 15);
			label2.TabIndex = 4;
			label2.Text = "label2";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(936, 23);
			label3.Name = "label3";
			label3.Size = new Size(38, 15);
			label3.TabIndex = 5;
			label3.Text = "label3";
			// 
			// comboBox1
			// 
			comboBox1.FormattingEnabled = true;
			comboBox1.Location = new Point(228, 20);
			comboBox1.Name = "comboBox1";
			comboBox1.Size = new Size(180, 23);
			comboBox1.TabIndex = 6;
			comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
			// 
			// comboBox2
			// 
			comboBox2.FormattingEnabled = true;
			comboBox2.Location = new Point(426, 20);
			comboBox2.Name = "comboBox2";
			comboBox2.Size = new Size(204, 23);
			comboBox2.TabIndex = 7;
			comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1015, 338);
			Controls.Add(comboBox2);
			Controls.Add(comboBox1);
			Controls.Add(label3);
			Controls.Add(label2);
			Controls.Add(label1);
			Controls.Add(pictureBox1);
			Controls.Add(button2);
			Controls.Add(button1);
			Margin = new Padding(3, 2, 3, 2);
			Name = "Form1";
			Text = "Form1";
			FormClosing += Form1_FormClosing;
			((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button button1;
		private Button button2;
		private PictureBox pictureBox1;
		private Label label1;
		private Label label2;
		private Label label3;
		private ComboBox comboBox1;
		private ComboBox comboBox2;
	}
}