using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;

namespace GameOfLife
{
    /// <summary>
    /// Data structure to store locational info of live cells.
    /// 
    /// While this structure is quite larger than the bit-sized cell is represents,
    /// it aids in the effort to optimize processing the game of life by effectively
    /// reducing the space of consideration from the size of the grid to a localized
    /// area centered around known live cells.
    /// <summary>
    public class CellLoc
    {
        public UInt16 X { get; set; }
        public UInt16 Y { get; set; }
        public CellLoc(UInt16 x, UInt16 y)
        { 
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// This structure was created to store both the grid for the game of life
    /// as well as the list of known living cells in the grid.
    /// <summary>
    public class Life
    {
        public List<CellLoc> Pair { get; set; }
        public bool[,] Mundos { get; set; }
        public Life(List<CellLoc> p, bool[,] m)
        {
            Pair = p;
            Mundos = m;
        }
    }

    /// <summary>
    /// Implementation of Conway's Game of Life, by Michael Dudrey 4/5/2017
    /// 
    /// This program was made for fun to explore & practice different capabilities of C#
    /// </summary>
    public partial class MainWindow : Window
    {
        // Program Init
        public MainWindow()
        {
            InitializeComponent();

            //main grid for life
            bool[,] mundos = new bool[1000, 1000];
            //list of cells marked for life/birth
            List<CellLoc> blessed = new List<CellLoc>();

            //populate 15 period cycle thing
            mundos[502, 500] = true; mundos[501, 501] = true; mundos[502, 501] = true; mundos[503, 501] = true; mundos[500, 502] = true; mundos[501, 502] = true;
            mundos[502, 502] = true; mundos[503, 502] = true; mundos[504, 502] = true; mundos[500, 509] = true; mundos[501, 509] = true; mundos[502, 509] = true;
            mundos[503, 509] = true; mundos[504, 509] = true; mundos[501, 510] = true; mundos[502, 510] = true; mundos[503, 510] = true; mundos[502, 511] = true;

            //gliders
            //mundos[500, 500] = true; mundos[501, 500] = true; mundos[501, 502] = true; mundos[502, 500] = true; mundos[502, 501] = true;
            //mundos[990, 990] = true; mundos[991, 990] = true; mundos[991, 992] = true; mundos[992, 990] = true; mundos[992, 991] = true;

            //populate blessed
            for (UInt16 i = 0; i < 1000; i++)
            {
                for(UInt16 j = 0; j < 1000; j++)
                {
                    if (mundos[i, j])
                    {
                        blessed.Add(new CellLoc(i,j));
                    }
                }
            }

            Life life = new Life(blessed, mundos);
            
            DispatcherTimer dt = new DispatcherTimer();
            dt.Tick += (sender, e) => { LifeThread(sender, e, ref life); };
            dt.Interval = new TimeSpan(0,0,0,0,100);// update UI every tenth of a second
            dt.Start();
        }
        
        // 4/17 main life loop
        private void LifeThread(object o, EventArgs e, ref Life l)
        {
            redraw(l.Pair);
            calcNewGen(ref l);
        }

        // 4/17: old params: ref bool[,] mundos, ref List<Pair> blessed
        public void calcNewGen(ref Life L)
        {
            // Any live cell with fewer than two live neighbours dies, as if caused by underpopulation.
            // Any live cell with two or three live neighbours lives on to the next generation.
            // Any live cell with more than three live neighbours dies, as if by overpopulation.
            List<CellLoc> nuBlessd = new List<CellLoc>();
            List<CellLoc> doomed = new List<CellLoc>();
            foreach (CellLoc p in L.Pair)
            {
                // check around cell p in Mundos to see if p should stay alive.
                // 4/18 Accounts for toroidal wrap around (for our 1000x1000 grid)
                int tmp = 0;
                if (L.Mundos[(p.X - 1) % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundos[(p.X - 1) % 1000, p.Y % 1000]) { tmp++; }
                if (L.Mundos[(p.X - 1) % 1000, (p.Y + 1) % 1000]) { tmp++; }
                if (L.Mundos[p.X % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundos[p.X % 1000, (p.Y + 1) % 1000]) { tmp++; }
                if (L.Mundos[(p.X + 1) % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundos[(p.X + 1) % 1000, p.Y % 1000]) { tmp++; }
                if (L.Mundos[(p.X + 1) % 1000, (p.Y + 1) % 1000]) { tmp++; }

                if (tmp == 2 || tmp == 3)
                {
                    nuBlessd.Add(p);
                }
                else
                {
                    doomed.Add(p);
                }
            }

            // 4/17 Need to account for toroidal wrap around. 
            // The window size would be stupidly large if I had true cells in opposite corners of the grid.
            // Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
            // for every false/dead cell in a NxM grid, where N = blessed.Xmax-min+2 and M = blessed.Ymax-min+2:
            // do same check as above but only add to nuBlessd if tmp == 3
            L.Pair = L.Pair.OrderBy(x => x.X).ToList();
            int xMin = L.Pair[0].X;
            int xMax = L.Pair[L.Pair.Count - 1].X;

            L.Pair = L.Pair.OrderBy(x => x.Y).ToList();
            int yMin = L.Pair[0].Y;
            int yMax = L.Pair[L.Pair.Count - 1].Y;

            for (int i = xMin - 1; i < xMax + 2; i++)
            {
                for (int j = yMin - 1; j < yMax + 2; j++)
                {
                    // if on a live cell, skip.
                    if (L.Mundos[i, j])
                    {
                        continue;
                    }
                    // check around the pair in Mundos to see if p should stay alive.
                    // account for wrap-around later
                    int tmp = 0;
                    for (int k = i - 1; k < i + 2; k++)
                    {
                        for (int l = j - 1; l < j + 2; l++)
                        {
                            if (k == i && l == j)
                            {
                                continue;
                            }
                            if (L.Mundos[k, l])
                            {
                                tmp++;
                            }
                        }
                    }
                    if (tmp == 3)
                    {
                        nuBlessd.Add(new CellLoc((UInt16)i, (UInt16)j));
                    }
                }
            }

            // add new cells
            L.Pair.Clear();
            foreach (CellLoc p in nuBlessd)
            {
                L.Pair.Add(p);
                L.Mundos[p.X, p.Y] = true;
            }

            // kill off cells
            foreach (CellLoc p in doomed)
            {
                L.Mundos[p.X, p.Y] = false;
            }
        }

        // method to refresh the UI
        public void redraw(List<CellLoc> blessed)
        {
            canvasMundos.Children.Clear();
            foreach (CellLoc p in blessed)
            {
                AddPixel(p.X, p.Y);
            }
        }

        // method to draw cells on the UI
        private void AddPixel(int x, int y)
        {
            Rectangle rec = new Rectangle();
            Canvas.SetTop(rec, y);
            Canvas.SetLeft(rec, x);
            rec.Width = 1;
            rec.Height = 1;
            rec.Fill = new SolidColorBrush(Colors.White);
            canvasMundos.Children.Add(rec);
        }
    }
}
