using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CustomControls;

namespace LocalVideoPlayer
{
    public class LayoutController
    {
        public bool onMainForm = true;
        private bool onTvForm = false;
        private bool onSeasonForm = false;
        private bool onPlayerForm = false;
        private bool onMovieForm = false;

        public CustomScrollbar mainScrollbar = null;
        public CustomScrollbar tvScrollbar = null;
        public CustomScrollbar seasonScrollbar = null;
        public Panel mainFormMainPanel = null;
        public Control mainFormClose = null;
        public Panel tvFormMainPanel = null;
        public Control tvFormClose = null;
        public PictureBox movieBackropBox = null;
        public Control movieFormClose = null;
        public Control playerFormClose = null;
        public Control playButton = null;
        public Panel seasonFormMainPanel = null;
        public int seasonFormIndex = 0;

        private int movieCount;
        private int tvShowCount;
        private List<int[]> mainFormGrid = new List<int[]>();
        private List<Control[]> mainFormControlGrid = new List<Control[]>();
        public List<PictureBox> tvBoxes = new List<PictureBox>();
        public List<PictureBox> movieBoxes = new List<PictureBox>();
        public List<Control> mainFormControlList = new List<Control>();
        public List<Control> tvFormControlList = new List<Control>();
        private int tvFormControlIndex = 0;
        private List<int[]> seasonFormGrid = new List<int[]>();
        private List<Control[]> seasonFormControlGrid = new List<Control[]>();
        public List<Control> seasonFormControlList = new List<Control>();
        private Control currentControl = null;
        private (int x, int y) currentPoint = (0, 0);
        private (int x, int y) returnPointA = (0, 0);
        private (int x, int y) returnPointB = (0, 0);
        public (int x, int y) up = (-1, 0);
        public (int x, int y) down = (1, 0);
        public (int x, int y) left = (0, -1);
        public (int x, int y) right = (0, 1);

        public LayoutController((int m, int t) mediaCount)
        {
            movieCount = mediaCount.m;
            tvShowCount = mediaCount.t;
            BuildMainGrid(false);
            BuildMainGrid(true);
        }

        public void Initialize()
        {
            BuildMainControlGrid();
            if (currentControl != null)
            {
                currentControl = mainFormControlGrid[0][0];
                CenterMouseOverControl(currentControl);
            }
        }

        public void CenterMouseOverControl(Control ctl)
        {
            ctl.Invoke(new MethodInvoker(delegate
            {
                Point target = new Point((ctl.Left + ctl.Right) / 2, (ctl.Top + ctl.Bottom) / 2);
                Point targetPos = ctl.Parent.PointToScreen(target);
                Cursor.Position = targetPos;
            }));
        }

        public void MovePointPosition((int x, int y) movePoint)
        {
            if (onPlayerForm) { return; }
            if (onMainForm) MoveMainGridPoint(movePoint);
            if (onSeasonForm)
            {
                MoveSeasonGridPoint(movePoint);
                return;
            }
            if (onTvForm) MoveTvPoint(movePoint.x);
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
                    currentPoint = (tvFormControlIndex, -1);
                    currentControl = tvFormControlList[currentPoint.x];
                    CenterMouseOverControl(currentControl);
                    return;
                }
            }

            if (onSeasonForm)
            {
                onSeasonForm = false;
                seasonFormControlList.Clear();
                seasonFormGrid.Clear();
                seasonFormControlGrid.Clear();
                currentPoint = returnPointB;
                currentControl = tvFormControlList[currentPoint.x];
                CenterMouseOverControl(currentControl);
                return;
            }

            if (onTvForm)
            {
                returnPointB = currentPoint;
                if (controlName.Equals("seasonButton"))
                {
                    onSeasonForm = true;
                    BuildSeasonGrid();
                    BuildSeasonControlGrid();
                    currentPoint = GetCurrentSeasonPoint(seasonFormIndex);
                    currentControl = seasonFormControlGrid[currentPoint.x][currentPoint.y];
                    if (seasonFormMainPanel.InvokeRequired)
                    {
                        seasonFormMainPanel.Invoke(new MethodInvoker(delegate
                        {
                            seasonFormMainPanel.ScrollControlIntoView(currentControl);
                            AdjustScrollBar();
                        }));
                    }
                    else
                    {
                        seasonFormMainPanel.ScrollControlIntoView(currentControl);
                        AdjustScrollBar();
                    }
                    CenterMouseOverControl(currentControl);
                }

                if (controlName.Equals("playerForm"))
                {
                    onPlayerForm = true;
                }
            }

            if (onMovieForm)
            {
                returnPointB = currentPoint;
                if (controlName.Equals("playerForm"))
                {
                    onPlayerForm = true;
                }
            }
        }

        public void CloseCurrentForm()
        {
            if (onMainForm)
            {
                mainFormMainPanel.Invoke(new MethodInvoker(delegate
                {
                    mainFormMainPanel.AutoScrollPosition = new Point(0, 0);
                    AdjustScrollBar();
                    mainFormClose.Visible = true;
                }));

                CenterMouseOverControl(mainFormClose);
                MouseWorker.DoMouseClick();
            }

            if (onPlayerForm)
            {
                onPlayerForm = false;
                CenterMouseOverControl(playerFormClose);
                MouseWorker.DoMouseClick();

                if (onMovieForm)
                {
                    onMovieForm = false;
                    onMainForm = true;
                    currentPoint = returnPointA;
                    currentControl = mainFormControlGrid[currentPoint.x][currentPoint.y];
                    CenterMouseOverControl(currentControl);
                }
                else if (onTvForm)
                {
                    currentPoint = returnPointB;
                    currentControl = tvFormControlList[currentPoint.x];
                    CenterMouseOverControl(currentControl);
                }
                return;
            }

            if (onSeasonForm)
            {
                MouseWorker.DoMouseClick();
                onSeasonForm = false;
                seasonFormControlList.Clear();
                seasonFormGrid.Clear();
                seasonFormControlGrid.Clear();
                currentPoint = returnPointB;
                currentControl = tvFormControlList[currentPoint.x];
                CenterMouseOverControl(currentControl);
                return;
            }

            if (onTvForm)
            {
                tvFormControlList.Clear();
                tvFormControlIndex = 0;
                onTvForm = false;
                onMainForm = true;

                tvFormMainPanel.Invoke(new MethodInvoker(delegate
                {
                    tvFormMainPanel.AutoScrollPosition = new Point(0, 0);
                    AdjustScrollBar();
                    tvFormClose.Visible = true;
                }));

                CenterMouseOverControl(tvFormClose);
                MouseWorker.DoMouseClick();
                currentPoint = returnPointA;
                currentControl = mainFormControlGrid[currentPoint.x][currentPoint.y];
                CenterMouseOverControl(currentControl);
            }

            if (onMovieForm)
            {
                onMovieForm = false;
                onMainForm = true;
                CenterMouseOverControl(movieFormClose);
                MouseWorker.DoMouseClick();
                currentControl = mainFormControlGrid[currentPoint.x][currentPoint.y];
                CenterMouseOverControl(currentControl);
            }
        }

        public void AdjustScrollBar()
        {
            if (onMainForm)
            {
                if (mainScrollbar != null)
                {
                    int newVal = -mainFormMainPanel.AutoScrollPosition.Y;
                    mainFormClose.Visible = newVal == 0 ? true : false;
                    mainScrollbar.Value = newVal;
                }
            }
            else if (onSeasonForm)
            {
                if (seasonScrollbar != null)
                {
                    int newVal = -seasonFormMainPanel.AutoScrollPosition.Y;
                    seasonScrollbar.Value = newVal;
                }
            }
            else if (onTvForm)
            {
                if (tvScrollbar != null)
                {
                    int newVal = -tvFormMainPanel.AutoScrollPosition.Y;
                    tvFormClose.Visible = newVal == 0 ? true : false;
                    tvScrollbar.Value = newVal;
                }
            }
        }

        public void ClearSeasonBoxBorder()
        {
            foreach (PictureBox p in seasonFormControlList) { p.BorderStyle = BorderStyle.None; }
        }

        public void ClearTvBoxBorder()
        {
            foreach(PictureBox p in tvBoxes) { p.BorderStyle = BorderStyle.None; }
        }

        public void ClearMovieBoxBorder()
        {
            foreach (PictureBox p in movieBoxes) { p.BorderStyle = BorderStyle.None; }
        }

        #region Tv form 

        public void DeactivateTvForm()
        {
            tvFormControlList.Clear();
            tvFormControlIndex = 0;
            onTvForm = false;
            onMainForm = true;
        }

        private void MoveTvPoint(int rX)
        {
            int newIndex;
            if (currentPoint.x == 0 && rX == 1)
            {
                newIndex = 2;
            }
            else if (currentPoint.x == 2 && rX == -1)
            {
                newIndex = 0;
            }
            else
            {
                newIndex = currentPoint.x + rX;
            }

            if (newIndex < 0 || newIndex >= tvFormControlList.Count) return;

            currentPoint = (newIndex, currentPoint.y);
            currentControl = tvFormControlList[newIndex];
            tvFormMainPanel.Invoke(new MethodInvoker(delegate
            {
                tvFormMainPanel.ScrollControlIntoView(currentControl);
                tvFormControlList[2].Location = new Point(tvFormControlList[1].Location.X + 20, tvFormControlList[1].Location.Y + tvFormControlList[1].Height + (int)(tvFormControlList[2].Height * 1.75));
                AdjustScrollBar();
            }));
            CenterMouseOverControl(currentControl);
        }

        #endregion

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

            mainFormMainPanel.Invoke(new MethodInvoker(delegate { 
                mainFormMainPanel.ScrollControlIntoView(currentControl);
                AdjustScrollBar();
            }));
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

        #region Season form grid

        private (int x, int y) GetCurrentSeasonPoint(int seasonFormIndex)
        {
            int count = 0;
            (int x, int y) point = (0, 0);
            while (seasonFormIndex > 0)
            {
                seasonFormIndex--;
                if (count == 2)
                {
                    count = 0;
                    point = (point.x + 1, 0);
                    if (seasonFormIndex == 0) break;
                }
                else
                {
                    point = (point.x, point.y + 1);
                    count++;
                }
            }
            seasonFormGrid[point.x][point.y] = 2;
            return point;
        }

        public void MoveSeasonGridPoint((int x, int y) movePoint)
        {
            (int x, int y) newPoint = (currentPoint.x + movePoint.x, currentPoint.y + movePoint.y);
            if (OutOfSeasonGridRange(newPoint)) return;

            if (seasonFormControlGrid[newPoint.x][newPoint.y] == null)
            {
                (int x, int y) candidatePoint = ClosestSeasonGridPoint(newPoint);
                if (candidatePoint.x != -1)
                {
                    newPoint = candidatePoint;
                }
                else
                {
                    newPoint = NextSeasonGridPoint(newPoint, movePoint);
                    if (newPoint.x == -1) return;
                }
            }

            seasonFormGrid[newPoint.x][newPoint.y] = 2;
            seasonFormGrid[currentPoint.x][currentPoint.y] = 1;
            currentPoint = newPoint;
            currentControl = seasonFormControlGrid[currentPoint.x][currentPoint.y];
            seasonFormMainPanel.Invoke(new MethodInvoker(delegate { 
                seasonFormMainPanel.ScrollControlIntoView(currentControl);
                AdjustScrollBar();
            }));
            CenterMouseOverControl(currentControl);
            PrintGrid(); PrintControlGrid();
        }

        public (int x, int y) NextSeasonGridPoint((int x, int y) currentPoint, (int x, int y) movePoint)
        {
            (int x, int y) nextPoint = (currentPoint.x + movePoint.x, currentPoint.y + movePoint.y);
            if (OutOfSeasonGridRange(nextPoint)) return (-1, -1);
            if (seasonFormControlGrid[nextPoint.x][nextPoint.y] == null)
            {
                NextSeasonGridPoint(nextPoint, movePoint);
            }
            else
            {
                return nextPoint;
            }
            return (-1, -1);
        }

        private (int x, int y) ClosestSeasonGridPoint((int x, int y) nextPoint)
        {
            int low = nextPoint.y - 1;
            int high = nextPoint.y + 1;
            while (low >= 0 || high > 3)
            {
                if (low >= 0)
                {
                    if (seasonFormControlGrid[nextPoint.x][low] != null) return (nextPoint.x, low);
                }

                if (high < 3)
                {
                    if (seasonFormControlGrid[nextPoint.x][high] != null) return (nextPoint.x, high);
                }
                low--;
                high++;
            }
            return (-1, -1);
        }

        private bool OutOfSeasonGridRange((int x, int y) testPoint)
        {
            if (testPoint.y < 0 || testPoint.x < 0 || testPoint.y >= 3 || testPoint.x >= seasonFormGrid.Count) return true;
            return false;
        }

        private void BuildSeasonControlGrid()
        {
            int seasonCount = seasonFormControlList.Count;
            int count = 0;
            int rowIndex = 0;
            int controlIndex = 0;

            for (int i = 0; i < seasonCount; i++)
            {
                if (count == 3)
                {
                    rowIndex++;
                    count = 0;
                }

                if (seasonFormGrid[rowIndex][count] == 0)
                {
                    seasonFormControlGrid[rowIndex][count] = null;
                }
                else
                {
                    seasonFormControlGrid[rowIndex][count] = seasonFormControlList[controlIndex];
                    controlIndex++;
                }
                count++;
            }
        }

        private void BuildSeasonGrid()
        {
            int seasonCount = seasonFormControlList.Count;
            int count = 0;
            int[] currentRow = null;
            Control[] currentControlRow = null;

            for (int i = 0; i < seasonCount; i++)
            {
                if (count == 3) count = 0;
                if (count == 0)
                {
                    currentRow = new int[3];
                    currentControlRow = new Control[3];
                    seasonFormGrid.Add(currentRow);
                    seasonFormControlGrid.Add(currentControlRow);
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

        private void PrintGrid()
        {
            foreach (int[] row in seasonFormGrid)
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

        private void PrintControlGrid()
        {
            foreach (Control[] row in seasonFormControlGrid)
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
