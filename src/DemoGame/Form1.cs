using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoGame
{
    public partial class Form1 : Form
    {
        DirectoryInfo SelectedGame;
        DopaScript.Interpreter Interpreter;

        Image BackBuffer;
        Graphics Graphic;

        List<Image> Images;
        HashSet<int> CurrentKeyPressed;
        bool GameStarted = false;

        public Form1()
        {
            InitializeComponent();


            DirectoryInfo gamesDir = new DirectoryInfo("Games");
            foreach (DirectoryInfo gameDir in gamesDir.GetDirectories())
            {
                comboBoxGames.Items.Add(gameDir);
            }

            InitData();
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            SelectedGame = comboBoxGames.SelectedItem as DirectoryInfo;
            if (SelectedGame == null)
            {
                return;
            }

            comboBoxGames.Enabled = false;
            buttonPlay.Enabled = false;

            this.Focus();
            GameStarted = true;

            string source = File.ReadAllText(Path.Combine(SelectedGame.FullName, "script.txt"));
            
            Interpreter = new DopaScript.Interpreter();
            Interpreter.Parse(source);

            Interpreter.AddFunction("flip", Flip);
            Interpreter.AddFunction("loadImage", LoadImage);
            Interpreter.AddFunction("drawImage", DrawImage);
            Interpreter.AddFunction("isKeyPressed", IsKeyPressed);
            Interpreter.AddFunction("fillRectangle", FillRectangle);

            Thread thread = new Thread(ExecGame);
            thread.IsBackground = true;
            thread.Start();
        }

        public void ExecGame()
        {
            Interpreter.Execute();
        }

        public void InitData()
        {
            BackBuffer = new Bitmap(320, 240);
            Graphic = Graphics.FromImage(BackBuffer);
            Graphic.FillRectangle(Brushes.SkyBlue, new Rectangle(0, 0, 320, 240));

            Images = new List<Image>();

            CurrentKeyPressed = new HashSet<int>();
        }

        public DopaScript.Value Flip(DopaScript.FunctionCallArgs parameters)
        {
            this.Invoke(new Action(() =>
            {
                pictureBoxScreen.Image = BackBuffer;
                pictureBoxScreen.Refresh();
            }));
            return null;
        }

        public DopaScript.Value LoadImage(DopaScript.FunctionCallArgs parameters)
        {
            int imageId = 0;

            this.Invoke(new Action(() =>
            {
                imageId = Images.Count;
                Images.Add(Bitmap.FromFile(Path.Combine(SelectedGame.FullName, parameters.Values[0].StringValue)));
            }));

            return new DopaScript.Value()
            {
                Type = DopaScript.Value.DataType.Numeric,
                NumericValue = imageId
            };
        }

        public DopaScript.Value DrawImage(DopaScript.FunctionCallArgs parameters)
        {
            this.Invoke(new Action(() =>
            {
                Image img = Images[(int)parameters.Values[0].NumericValue];
                if(parameters.Values.Count == 3)
                {
                    Graphic.DrawImage(img, new Point((int)parameters.Values[1].NumericValue, (int)parameters.Values[2].NumericValue));
                }
                else if(parameters.Values.Count == 7)
                {
                    Rectangle source = new Rectangle();
                    source.X = (int)parameters.Values[1].NumericValue;
                    source.Y = (int)parameters.Values[2].NumericValue;
                    source.Width = (int)parameters.Values[3].NumericValue;
                    source.Height = (int)parameters.Values[4].NumericValue;

                    Rectangle destination = new Rectangle();
                    destination.X = (int)parameters.Values[5].NumericValue;
                    destination.Y = (int)parameters.Values[6].NumericValue;
                    destination.Width = (int)parameters.Values[3].NumericValue;
                    destination.Height = (int)parameters.Values[4].NumericValue;

                    Graphic.DrawImage(img, source, destination, GraphicsUnit.Pixel);
                }
            }));
            return null;
        }

        public DopaScript.Value FillRectangle(DopaScript.FunctionCallArgs parameters)
        {
            this.Invoke(new Action(() =>
            {
                Brush brush = new SolidBrush(Color.FromName(parameters.Values[0].StringValue));

                Rectangle rect = new Rectangle();
                rect.X = (int)parameters.Values[1].NumericValue;
                rect.Y = (int)parameters.Values[2].NumericValue;
                rect.Width = (int)parameters.Values[3].NumericValue;
                rect.Height = (int)parameters.Values[4].NumericValue;

                Graphic.FillRectangle(brush, rect);
            }));
            return null;
        }

        public DopaScript.Value IsKeyPressed(DopaScript.FunctionCallArgs parameters)
        {
            return new DopaScript.Value()
            {
                Type = DopaScript.Value.DataType.Boolean,
                BoolValue = CurrentKeyPressed.Contains((int)parameters.Values[0].NumericValue)
            };
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentKeyPressed.Add(e.KeyValue);
            e.Handled = GameStarted;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            CurrentKeyPressed.Remove(e.KeyValue);
            e.Handled = GameStarted;
        }

        private void pictureBoxScreen_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            base.OnPaint(e);
        }
    }
}
