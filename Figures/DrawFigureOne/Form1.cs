﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Messaging;
using System.Linq;

namespace DrawFigureOne
{
    public partial class Form1 : Form
    {
        FigureList list = new FigureList();
        Color penColor = Color.FromName("Black");
        string path = Directory.GetCurrentDirectory();

        Figure checkFig = null;
        Figure drawFigure = null;
        List<Type> importedPlug = new List<Type>();
        bool draw = false;
        private static Point checkPoint;

        //for translating figure
        private int xCursorStart = 0;
        private int yCursorStart = 0;
        public Bitmap bmp;

        public static Point middleStart, middleEnd;

        //points for initial polygon
        private static List<Point> points;
        private static Point onePoint, twoPoint, threePoint, fourPoint, fivePoint, sixPoint;

        public Form1()
        {
            InitializeComponent();
            bmp = new Bitmap(HolstPanel.Width, HolstPanel.Height);
            path = path.Substring(0, path.Length - 10);
            checkPoint = new Point(-1, -1);
        }


        private void HolstPanel_Paint(object sender, PaintEventArgs e)
        {
            if (!colorDialog.FullOpen)
            {
                Figure fig;
                for (int i = 0; i < list.getSize; i++)
                {
                    fig = list.getFigure(i);
                    DisplayBMP(bmp, fig);
                }
                ReturnBMP(bmp, list);
            }
        }

        private void ChangePenColor_Click(object sender, EventArgs e)
        {
            colorDialog.FullOpen = true;
            colorDialog.Color = penColor;
            if (colorDialog.ShowDialog() == DialogResult.OK)
                penColor = colorDialog.Color;
            colorDialog.FullOpen = false;
        }

        private void HolstPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (checkFig == null)
            {
                //change view of cursor for displacement
                if (list.CheckCursorSizeAll(e.Location) != null)
                {
                    HolstPanel.Cursor = System.Windows.Forms.Cursors.SizeAll;
                }
                else
                {
                    //change view of cursor for rotate
                    if (list.CheckCursorHand(e.Location).Item1 != null)
                    {
                        HolstPanel.Cursor = System.Windows.Forms.Cursors.Hand;
                    }
                    else
                    {
                        HolstPanel.Cursor = System.Windows.Forms.Cursors.Default;
                    }
                }
            }
            else
            {
                if (HolstPanel.Cursor == System.Windows.Forms.Cursors.SizeAll)
                {
                    //transform figure
                    checkFig.RewriteFigure(e.X - xCursorStart, e.Y - yCursorStart);
                }
                if (HolstPanel.Cursor == System.Windows.Forms.Cursors.Hand)
                {
                    //transform figure
                    checkFig.Rotate(e.X - xCursorStart, e.Y - yCursorStart, checkPoint);
                }
                xCursorStart = e.X;
                yCursorStart = e.Y;
            }

            if (draw)
            {
                drawFigure.Change(e.Location);
            }
        }

        private void HolstPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (HolstPanel.Cursor == System.Windows.Forms.Cursors.SizeAll)
            {
                checkFig = list.CheckCursorSizeAll(e.Location);
                if (e.Button == MouseButtons.Left)
                {
                    xCursorStart = e.X;
                    yCursorStart = e.Y;
                }
            }
            if (HolstPanel.Cursor == System.Windows.Forms.Cursors.Hand)
            {
                checkFig = list.CheckCursorHand(e.Location).Item1;
                checkPoint = list.CheckCursorHand(e.Location).Item2;
                xCursorStart = e.X;
                yCursorStart = e.Y;
            }
        }

        private void HolstPanel_MouseUp(object sender, MouseEventArgs e)
        {
            checkFig = null;
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = "c:\\";
            saveFileDialog.DefaultExt = "lin";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create);
                BinaryFormatter bfser = new BinaryFormatter();
                bfser.Serialize(fileStream, list);
                fileStream.Close();
            }
        }

        private void ButtonOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы .lin | *.lin";
            openFileDialog.InitialDirectory = "c:\\";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (File.Exists(openFileDialog.FileName))
                    {
                        FileStream fileStream = new FileStream(openFileDialog.FileName, FileMode.Open);
                        BinaryFormatter bfser = new BinaryFormatter();
                        list = (FigureList)bfser.Deserialize(fileStream);
                    }
                }
                catch
                {
                    MessageBox.Show("Unable to open file");
                }
            }
        }

        private void HolstPanel_Click(object sender, EventArgs e)
        {
            if (draw)
            {
                list.Add(drawFigure);
                draw = false;
                drawFigure = null;
                buttonSegment.Image = Image.FromFile(path + "\\assets\\segment.png");
                buttonRectangle.Image = Image.FromFile(path + "\\assets\\rectangle.png");
                buttonEllipse.Image = Image.FromFile(path + "\\assets\\ellipse.png");
                buttonPolygon.Image = Image.FromFile(path + "\\assets\\polygon.png");
                HolstPanel.Refresh();
            }
        }

        private void LoadPlugins_Click(object sender, EventArgs e)
        {
            listPlugins.Items.Clear();
            string[] pathFiles = Directory.GetFiles(path + "\\Plugins", "*.dll");
            foreach (string file in pathFiles)
            {
                try
                {
                    Assembly asm = Assembly.LoadFrom(file);
                    foreach (Type type in asm.GetTypes())
                    {
                        importedPlug.Add(type);
                        listPlugins.Items.Add(type.Name);
                    }
                } 
                catch
                {
                }
            }
        }

        private void drawPlugins_Click(object sender, EventArgs e)
        {
            Graphics graph = Graphics.FromImage(bmp);
            String currFigure = listPlugins.Text;
            
            importedPlug.ForEach(delegate (Type obj)
            {
                if (obj.Name == currFigure)
                {
                    Figure newInstance = Activator.CreateInstance(obj) as Figure;
                    drawFigure = newInstance;
                    draw = true;
                }
            });
        }

        private void DisplayBMP(Bitmap bmp, Figure fig)
        {
            Graphics graph = Graphics.FromImage(bmp);
            fig.Display(graph);
            HolstPanel.Image = bmp;
            graph.Dispose();
        }

        private void ReturnBMP(Bitmap bmp, FigureList list)
        {
            Graphics graph = Graphics.FromImage(bmp);
            graph.Clear(HolstPanel.BackColor);
            list.Display(graph);
            HolstPanel.Image = bmp;
        }

        private void ButtonSegment_MouseUp(object sender, MouseEventArgs e)
        {
            //change background
            if (e.Button == MouseButtons.Left)
            {
                buttonSegment.Image = Image.FromFile(path + "\\assets\\segmentActive.png");
                buttonRectangle.Image = Image.FromFile(path + "\\assets\\rectangle.png");
                buttonEllipse.Image = Image.FromFile(path + "\\assets\\ellipse.png");
                buttonPolygon.Image = Image.FromFile(path + "\\assets\\polygon.png");
            }
            else if (e.Button == MouseButtons.Right)
            {
                buttonSegment.Image = Image.FromFile(path + "\\assets\\segment.png");
            }

            //add base figure
            middleStart = new Point(HolstPanel.Width / 2 - 30, HolstPanel.Height / 2 - 30);
            middleEnd = new Point(HolstPanel.Width / 2 + 30, HolstPanel.Height / 2 + 30);

            points = new List<Point>();
            points.Add(middleStart);
            points.Add(middleEnd);

            draw = true;
            Segment segment = new Segment(points, penColor);
            drawFigure = segment;
        }

        private void ButtonRectangle_MouseUp(object sender, MouseEventArgs e)
        {
            //change background
            if (e.Button == MouseButtons.Left)
            {
                buttonSegment.Image = Image.FromFile(path + "\\assets\\segment.png");
                buttonRectangle.Image = Image.FromFile(path + "\\assets\\rectangleActive.png");
                buttonEllipse.Image = Image.FromFile(path + "\\assets\\ellipse.png");
                buttonPolygon.Image = Image.FromFile(path + "\\assets\\polygon.png");
            }
            else if (e.Button == MouseButtons.Right)
            {
                buttonRectangle.Image = Image.FromFile(path + "\\assets\\rectangle.png");
            }

            //add base figure
            middleStart = new Point(HolstPanel.Width / 2 - 30, HolstPanel.Height / 2 - 30);
            middleEnd = new Point(HolstPanel.Width / 2 + 30, HolstPanel.Height / 2 + 30);

            points = new List<Point>();
            points.Add(middleStart);
            points.Add(middleEnd);

            draw = true;
            Rectangle rectangle = new Rectangle(points, penColor);
            drawFigure = rectangle;
        }

        private void ButtonEllipse_MouseUp(object sender, MouseEventArgs e)
        {
            //change background
            if (e.Button == MouseButtons.Left)
            {
                buttonSegment.Image = Image.FromFile(path + "\\assets\\segment.png");
                buttonRectangle.Image = Image.FromFile(path + "\\assets\\rectangle.png");
                buttonEllipse.Image = Image.FromFile(path + "\\assets\\ellipseActive.png");
                buttonPolygon.Image = Image.FromFile(path + "\\assets\\polygon.png");
            }
            else if (e.Button == MouseButtons.Right)
            {
                buttonEllipse.Image = Image.FromFile(path + "\\assets\\ellipse.png");
            }

            //add base figure
            middleStart = new Point(HolstPanel.Width / 2 - 30, HolstPanel.Height / 2 - 30);
            middleEnd = new Point(HolstPanel.Width / 2 + 30, HolstPanel.Height / 2 + 30);

            points = new List<Point>();
            points.Add(middleStart);
            points.Add(middleEnd);

            draw = true;
            Ellipse ellipse = new Ellipse(points, penColor);
            drawFigure = ellipse;
        }

        private void ButtonPolygon_MouseUp(object sender, MouseEventArgs e)
        {
            //change background
            if (e.Button == MouseButtons.Left)
            {
                buttonSegment.Image = Image.FromFile(path + "\\assets\\segment.png");
                buttonRectangle.Image = Image.FromFile(path + "\\assets\\rectangle.png");
                buttonEllipse.Image = Image.FromFile(path + "\\assets\\ellipse.png");
                buttonPolygon.Image = Image.FromFile(path + "\\assets\\polygonActive.png");
            }
            else if (e.Button == MouseButtons.Right)
            {
                buttonPolygon.Image = Image.FromFile(path + "\\assets\\polygon.png");
            }

            //add base figure
            //initialize start points
            onePoint = new Point(HolstPanel.Width / 2, HolstPanel.Height / 2 + 30);
            twoPoint = new Point(HolstPanel.Width / 2 + 26, HolstPanel.Height / 2 + 15);
            threePoint = new Point(HolstPanel.Width / 2 + 26, HolstPanel.Height / 2 - 15);
            fourPoint = new Point(HolstPanel.Width / 2, HolstPanel.Height / 2 - 30);
            fivePoint = new Point(HolstPanel.Width / 2 - 26, HolstPanel.Height / 2 - 15);
            sixPoint = new Point(HolstPanel.Width / 2 - 26, HolstPanel.Height / 2 + 15);

            points = new List<Point>();
            points.Add(onePoint);
            points.Add(twoPoint);
            points.Add(threePoint);
            points.Add(fourPoint);
            points.Add(fivePoint);
            points.Add(sixPoint);

            draw = true;
            Polygon polygon = new Polygon(points, penColor);
            drawFigure = polygon;
        }
    }
}
