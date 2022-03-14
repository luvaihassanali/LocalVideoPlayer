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
        public Control mainFormClose = null;
        public Control tvFormClose = null;
        public Control playerFormClose = null;

        private int movieCount;
        private int tvShowCount;
        private List<int[]> mainFormGrid = new List<int[]>();
        private List<Control[]> mainFormControlGrid = new List<Control[]>();
        public List<Control> controlList = new List<Control>();

        private (int x, int y) currentPoint = (0, 0);
        private (int x, int y) up = (0, -1);
        private (int x, int y) down = (0, 1);
        private (int x, int y) left = (-1, 0);
        private (int x, int y) right = (1, 0);

        public LayoutModel((int m, int t) mediaCount)
        {
            movieCount = mediaCount.m;
            tvShowCount = mediaCount.t;
            buildGrid(false);
            buildGrid(true);
        }

        public void Initialize()
        {
            buildControlGrid();
            CenterMouseOverControl(mainFormControlGrid[0][0]);
            printGrid();
            printControlGrid();
        }

        private void moveCurrentPoint((int x, int y) movePoint)
        {
            (int, int) newPoint = (currentPoint.x + movePoint.x, currentPoint.y + movePoint.y);
            if (OutOfRange(newPoint)) return;
            mainFormGrid[newPoint.Item2][newPoint.Item1] = 2;
            currentPoint = newPoint;
        }

        private bool OutOfRange((int x, int y) testPoint)
        {
            if (testPoint.x < 0 || testPoint.y < 0 || testPoint.x >= 6 || testPoint.y >= mainFormGrid.Count) return true;
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

        public void CenterMouseOverControl(Control ctl)
        {
            Point target = new Point((ctl.Left + ctl.Right) / 2, (ctl.Top + ctl.Bottom) / 2);
            Point targetPos = ctl.Parent.PointToScreen(target);
            Cursor.Position = targetPos;
        }
    }

    class MainInterface
    {
        int[,] row = new int[2, 2];
        //close
    }

    class TvFormInterface
    {
        //Control mainBox
        //List<Control> episodePictureBoxes
        //close
    }

    class PlayerInterface
    {
        //Control play/pause 
        //Control close
        //TImeline?
    }
}
