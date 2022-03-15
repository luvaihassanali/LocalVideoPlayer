using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    public class LayoutModel
    {
        private bool onMainForm = true;
        private bool onTvForm = false;
        private bool onPlayerForm = false;
        public MainForm mainForm = null;
        private Panel mainFormMainPanel = null;
        public Control mainFormClose = null;
        public TvForm tvForm = null;
        private PictureBox tvBackdropPictureBox = null;
        private Control tvFormClose = null;
        public Form movieForm = null;
        private PictureBox movieBackropPictureBox = null;
        private Control movieFormClose = null;
        public PlayerForm playerForm = null;
        public Control playerFormClose = null;
        public Control playButton = null;
        //timeline

        private int movieCount;
        private int tvShowCount;
        private List<int[]> mainFormGrid = new List<int[]>();
        private List<Control[]> mainFormControlGrid = new List<Control[]>();
        public List<Control> controlList = new List<Control>();

        private (int x, int y) currentPoint = (0, 0);
        public (int x, int y) up = (-1, 0);
        public (int x, int y) down = (1, 0);
        public (int x, int y) left = (0, -1);
        public (int x, int y) right = (0, 1);

        public LayoutModel((int m, int t) mediaCount, MainForm m)
        {
            mainForm = m;
            movieCount = mediaCount.m;
            tvShowCount = mediaCount.t;
            buildGrid(false);
            buildGrid(true);

        }

        public void Initialize()
        {
            foreach (Control c in mainForm.Controls)
            {
                Panel p_ = c as Panel;
                if (p_ != null && p_.Name.Equals("mainFormMainPanel"))
                {
                    mainFormMainPanel = p_;
                }

            }
            buildControlGrid();
            CenterMouseOverControl(mainFormControlGrid[0][0]);
            printGrid();
            printControlGrid();
        }
        
        public void moveCurrentPoint((int x, int y) movePoint)
        {
            (int x, int y) newPoint = (currentPoint.x + movePoint.x, currentPoint.y + movePoint.y);
            if (OutOfRange(newPoint)) return;
            if (mainFormControlGrid[newPoint.x][newPoint.y] == null)
            {
                newPoint = GetNextPoint(newPoint, movePoint);
                if (newPoint.x == -1) return;

            }
            mainFormGrid[newPoint.x][newPoint.y] = 2;
            mainFormGrid[currentPoint.x][currentPoint.y] = 1;
            currentPoint = newPoint;
            printGrid();
            printControlGrid();
            Control currentControl = mainFormControlGrid[currentPoint.x][currentPoint.y];
            mainFormMainPanel.ScrollControlIntoView(currentControl);
            CenterMouseOverControl(currentControl);
        }

        public (int x, int y) GetNextPoint((int x, int y) currentPoint, (int x, int y) movePoint)
        {
            (int x, int y) nextPoint = (currentPoint.x + movePoint.x, currentPoint.y + movePoint.y);
            if (OutOfRange(nextPoint)) return (-1, -1);
            if (mainFormControlGrid[nextPoint.x][nextPoint.y] == null)
            {
                GetNextPoint(nextPoint, movePoint);
            } else
            {
                return nextPoint;
            }
            return (-1, -1);
        }

        private bool OutOfRange((int x, int y) testPoint)
        {
            if (testPoint.y < 0 || testPoint.x < 0 || testPoint.y >= 6 || testPoint.x >= mainFormGrid.Count) return true;
            return false;
        }

        private void buildControlGrid()
        {
            int count = 0;
            int rowIndex = 0;
            int controlIndex = movieCount;

            for (int i = 0; i < tvShowCount; i++)
            {
                if (count == 6)
                {
                    rowIndex++;
                    count = 0;
                }

                if (mainFormGrid[rowIndex][count] == 0)
                {
                    mainFormControlGrid[rowIndex][count] = null;
                } 
                else
                {
                    mainFormControlGrid[rowIndex][count] = controlList[controlIndex];
                    controlIndex++;
                }
                count++;
            }

            count = 0;
            controlIndex = 0;
            rowIndex++;

            for (int i = 0; i < movieCount; i++)
            {
                if (count == 6)
                {
                    rowIndex++;
                    count = 0;
                }

                if (mainFormGrid[rowIndex][count] == 0)
                {
                    mainFormControlGrid[rowIndex][count] = null;
                }
                else
                {
                    mainFormControlGrid[rowIndex][count] = controlList[controlIndex];
                    controlIndex++;
                }
                count++;
            }
        }

        private void buildGrid(bool movie)
        {
            int currentCount = movie ? movieCount : tvShowCount;
            int count = 0;
            int[] currentRow = null;
            Control[] currentControlRow = null;

            for (int i = 0; i < currentCount; i++)
            {
                if (count == 6) count = 0;
                if (count == 0)
                {
                    currentRow = new int[6];
                    currentControlRow = new Control[6];
                    mainFormGrid.Add(currentRow);
                    mainFormControlGrid.Add(currentControlRow);
                    currentRow[count] = 1;
                    currentControlRow[count] = null;
                }

                currentRow[count] = 1;
                currentControlRow[count] = null;
                count++;
            }
        }

        public void CenterMouseOverControl(Control ctl)
        {
            Point target = new Point((ctl.Left + ctl.Right) / 2, (ctl.Top + ctl.Bottom) / 2);
            Point targetPos = ctl.Parent.PointToScreen(target);
            Cursor.Position = targetPos;
        }

        private void printGrid()
        {
            foreach (int[] row in mainFormGrid)
            {
                Console.Write("[ ");
                for (int i = 0; i < row.Length; i++)
                {
                    Console.Write(row[i]);
                    if (i != row.Length - 1)
                    {
                        Console.Write(", ");
                    }
                }
                Console.WriteLine(" ]");
            }
            Console.WriteLine();
        }

        private void printControlGrid()
        {
            foreach (Control[] row in mainFormControlGrid)
            {
                Console.Write("[ ");
                for (int i = 0; i < row.Length; i++)
                {
                    string itemName = row[i] == null ? "null" : row[i].Name;
                    Console.Write(itemName);
                    if (i != row.Length - 1)
                    {
                        Console.Write(", ");
                    }
                }
                Console.WriteLine(" ]");
            }
            Console.WriteLine();
        }
    }
}
