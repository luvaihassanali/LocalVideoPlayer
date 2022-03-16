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
        private bool onMovieForm = false;

        public Panel mainFormMainPanel = null;
        public Control mainFormClose = null;
        public Panel tvFormMainPanel = null;
        public Control tvFormClose = null;
        public PictureBox movieBackropBox = null;
        public Control movieFormClose = null;
        public PlayerForm playerForm = null;
        public Control playerFormClose = null;
        public Control playButton = null;
        //timeline

        private int movieCount;
        private int tvShowCount;
        private List<int[]> mainFormGrid = new List<int[]>();
        private List<Control[]> mainFormControlGrid = new List<Control[]>();
        public List<Control> mainFormControlList = new List<Control>();
        public List<Control> tvFormControlList = new List<Control>();
        private int tvFormControlIndex = 0;
        private Control currentControl = null;
        private (int x, int y) currentPoint = (0, 0);
        private (int x, int y) returnPointA = (0, 0);
        private (int x, int y) returnPointB = (0, 0);
        private (int x, int y) returnPointC = (0, 0);
        public (int x, int y) up = (-1, 0);
        public (int x, int y) down = (1, 0);
        public (int x, int y) left = (0, -1);
        public (int x, int y) right = (0, 1);

        public LayoutModel((int m, int t) mediaCount)
        {
            movieCount = mediaCount.m;
            tvShowCount = mediaCount.t;
            BuildMainGrid(false);
            BuildMainGrid(true);
        }

        public void Initialize()
        {
            if (onMainForm)
            {
                BuildMainControlGrid();
                currentControl = mainFormControlGrid[0][0];
                CenterMouseOverControl(currentControl);
            }
        }

        public void Select(string controlName)
        {
            if (onMainForm)
            {
                onMainForm = false;
                returnPointA = currentPoint;
                bool isMovie = MainForm.media.IsMovie(controlName);
                if (isMovie)
                {
                    onMovieForm = true;
                    currentControl = movieBackropBox;
                    CenterMouseOverControl(currentControl);
                }
                else
                {
                    onTvForm = true;
                    // collect tv controls into list (only up down)
                    // add move points
                    // do season form
                    // player form...
                    currentPoint = (tvFormControlIndex, -1);
                    currentControl = tvFormControlList[tvFormControlIndex];
                    CenterMouseOverControl(currentControl);
                }
            }
        }

        public void CloseCurrentForm()
        {
            if (onMainForm)
            {
                mainFormMainPanel.ScrollControlIntoView(mainFormClose);
                CenterMouseOverControl(mainFormClose);
                MouseWorker.DoMouseClick();
            }

            if (onMovieForm)
            {
                onMovieForm = false;
                onMainForm = true;
                CenterMouseOverControl(movieFormClose);
                MouseWorker.DoMouseClick();
                currentPoint = returnPointA;
                currentControl = mainFormControlGrid[currentPoint.x][currentPoint.y];
                CenterMouseOverControl(currentControl);
            }

            if (onTvForm)
            {
                tvFormControlList.Clear();
                tvFormControlIndex = 0;
                onTvForm = false;
                onMainForm = true;
                CenterMouseOverControl(tvFormClose);
                MouseWorker.DoMouseClick();
                currentPoint = returnPointA;
                currentControl = mainFormControlGrid[currentPoint.x][currentPoint.y];
                CenterMouseOverControl(currentControl);
            }
        }

        public void MovePointPosition((int x, int y) movePoint)
        {
            if (onMainForm) MoveMainGridPoint(movePoint);
            if (onTvForm) MoveTvPoint(movePoint.x);
        }

        private void MoveTvPoint(int rX)
        {
            int newIndex = currentPoint.x + rX;
            if (newIndex < 0 || newIndex >= tvFormControlList.Count) return;
            currentPoint = (newIndex, currentPoint.y);
            currentControl = tvFormControlList[newIndex]; 
            tvFormMainPanel.ScrollControlIntoView(currentControl);
            CenterMouseOverControl(currentControl);
        }

        public void CenterMouseOverControl(Control ctl)
        {
            Point target = new Point((ctl.Left + ctl.Right) / 2, (ctl.Top + ctl.Bottom) / 2);
            Point targetPos = ctl.Parent.PointToScreen(target);
            Cursor.Position = targetPos;
        }

        #region Main form grid

        public void MoveMainGridPoint((int x, int y) movePoint)
        {
            (int x, int y) newPoint = (currentPoint.x + movePoint.x, currentPoint.y + movePoint.y);
            if (OutOfMainGridRange(newPoint)) return;

            if (mainFormControlGrid[newPoint.x][newPoint.y] == null)
            {
                (int x, int y) candidatePoint = ClosestMainGridPoint(newPoint);
                if (candidatePoint.x != -1)
                {
                    newPoint = candidatePoint;
                }
                else
                {
                    newPoint = NextMainGridPoint(newPoint, movePoint);
                    if (newPoint.x == -1) return;
                }
            }

            mainFormGrid[newPoint.x][newPoint.y] = 2;
            mainFormGrid[currentPoint.x][currentPoint.y] = 1;
            currentPoint = newPoint;

            currentControl = mainFormControlGrid[currentPoint.x][currentPoint.y];
            mainFormMainPanel.ScrollControlIntoView(currentControl);
            CenterMouseOverControl(currentControl);
        }

        public (int x, int y) NextMainGridPoint((int x, int y) currentPoint, (int x, int y) movePoint)
        {
            (int x, int y) nextPoint = (currentPoint.x + movePoint.x, currentPoint.y + movePoint.y);
            if (OutOfMainGridRange(nextPoint)) return (-1, -1);
            if (mainFormControlGrid[nextPoint.x][nextPoint.y] == null)
            {
                NextMainGridPoint(nextPoint, movePoint);
            }
            else
            {
                return nextPoint;
            }
            return (-1, -1);
        }

        private (int x, int y) ClosestMainGridPoint((int x, int y) nextPoint)
        {
            int low = nextPoint.y - 1;
            int high = nextPoint.y + 1;
            while (low >= 0 || high > 6)
            {
                if (low >= 0)
                {
                    if (mainFormControlGrid[nextPoint.x][low] != null) return (nextPoint.x, low);
                }

                if (high < 6)
                {
                    if (mainFormControlGrid[nextPoint.x][high] != null) return (nextPoint.x, high);
                }
                low--;
                high++;
            }
            return (-1, -1);
        }

        private bool OutOfMainGridRange((int x, int y) testPoint)
        {
            if (testPoint.y < 0 || testPoint.x < 0 || testPoint.y >= 6 || testPoint.x >= mainFormGrid.Count) return true;
            return false;
        }

        private void BuildMainControlGrid()
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
                    mainFormControlGrid[rowIndex][count] = mainFormControlList[controlIndex];
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
                    mainFormControlGrid[rowIndex][count] = mainFormControlList[controlIndex];
                    controlIndex++;
                }
                count++;
            }
        }

        private void BuildMainGrid(bool movie)
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

        #endregion

        #region Print functions

        private void PrintMainGrid()
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

        private void PrintMainControlGrid()
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

        #endregion
    }
}
