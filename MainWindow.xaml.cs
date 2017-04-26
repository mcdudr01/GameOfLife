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
    /// 
    /// 4/26 made into immutable struct and HashSet-friendly
    /// <summary>
    public struct CellPos : IEquatable<CellPos>
    {
        private readonly UInt16 x, y;

        public UInt16 X { get { return x; } }
        public UInt16 Y { get { return y; } }

        public CellPos(UInt16 x, UInt16 y)
        { 
            this.x = x;
            this.y = y;
        }

        public override int GetHashCode()
        {
            return 2*x + 3*y;
        }

        public bool Equals(CellPos c)
        {
            return x == c.x && y == c.y;
        }
    }

    /// <summary>
    /// This structure was created to store both the grid for the game of life
    /// as well as the list of known living cells in the grid.
    /// <summary>
    public class Life
    {
        public List<CellPos> Cells { get; set; }
        public bool[,] Mundo { get; set; }
        public Life(List<CellPos> p, bool[,] m)
        {
            Cells = p;
            Mundo = m;
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
            bool[,] mundo = new bool[1000, 1000];
            //list of cells marked for life/birth
            List<CellPos> blessed = new List<CellPos>();

            //populate 15 period cycle thing
            mundo[502, 500] = true; mundo[501, 501] = true; mundo[502, 501] = true; mundo[503, 501] = true; mundo[500, 502] = true; mundo[501, 502] = true;
            mundo[502, 502] = true; mundo[503, 502] = true; mundo[504, 502] = true; mundo[500, 509] = true; mundo[501, 509] = true; mundo[502, 509] = true;
            mundo[503, 509] = true; mundo[504, 509] = true; mundo[501, 510] = true; mundo[502, 510] = true; mundo[503, 510] = true; mundo[502, 511] = true;

            //gliders
            //mundo[500, 500] = true; mundo[501, 500] = true; mundo[501, 502] = true; mundo[502, 500] = true; mundo[502, 501] = true;
            //mundo[990, 990] = true; mundo[991, 990] = true; mundo[991, 992] = true; mundo[992, 990] = true; mundo[992, 991] = true;

            //populate blessed
            for (UInt16 i = 0; i < 1000; i++)
            {
                for(UInt16 j = 0; j < 1000; j++)
                {
                    if (mundo[i, j])
                    {
                        blessed.Add(new CellPos(i,j));
                    }
                }
            }

            Life life = new Life(blessed, mundo);
            
            DispatcherTimer dt = new DispatcherTimer();
            dt.Tick += (sender, e) => { LifeThread(sender, e, ref life); };
            dt.Interval = new TimeSpan(0,0,0,0,100);// update UI every tenth of a second
            dt.Start();
        }
        
        // 4/17 main life loop
        private void LifeThread(object o, EventArgs e, ref Life l)
        {
            redraw(l.Cells);
            calcNewGen(ref l);
        }

        // 4/17: old params: ref bool[,] mundos, ref List<Pair> blessed
        public void calcNewGen(ref Life L)
        {
            // Any live cell with fewer than two live neighbours dies, as if caused by underpopulation.
            // Any live cell with two or three live neighbours lives on to the next generation.
            // Any live cell with more than three live neighbours dies, as if by overpopulation.
            // 4/26 Account for toroidal wrap around and use HashSet to filter duplicates
            // HashSet for checking which unique empty cells should become alive
            var potentials = new HashSet<CellPos>();
            List<CellPos> nuBlessd = new List<CellPos>();
            List<CellPos> doomed = new List<CellPos>();
            foreach (CellPos p in L.Cells)
            {
                // check around cell p in Mundos to see if p should stay alive.
                // 4/18 Accounts for toroidal wrap around (for our 1000x1000 grid)
                int tmp = 0;
                if (L.Mundo[(p.X - 1) % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundo[(p.X - 1) % 1000, p.Y % 1000]) { tmp++; }
                if (L.Mundo[(p.X - 1) % 1000, (p.Y + 1) % 1000]) { tmp++; }
                if (L.Mundo[p.X % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundo[p.X % 1000, (p.Y + 1) % 1000]) { tmp++; }
                if (L.Mundo[(p.X + 1) % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundo[(p.X + 1) % 1000, p.Y % 1000]) { tmp++; }
                if (L.Mundo[(p.X + 1) % 1000, (p.Y + 1) % 1000]) { tmp++; }

                if (tmp == 2 || tmp == 3)
                {
                    nuBlessd.Add(p);
                }
                else
                {
                    doomed.Add(p);
                }

                //check a 3x3 grid around each cell around p to build the HashSet of empty cells
                int iwrap = 0;
                for (int i = (p.X - 1) % 1000; iwrap < 3; i++)
                {
                    int jwrap = 0;
                    for (int j = (p.Y - 1) % 1000; jwrap < 3; j++)
                    {
                        //check around empty/false cell
                        if (!L.Mundo[i % 1000, j % 1000])
                        {
                            // add empty cell to potentials HashSet
                            potentials.Add(new CellPos((UInt16)(i % 1000), (UInt16)(j % 1000)));
                        }
                        jwrap++;
                    }
                    iwrap++;
                }
            }
            
            //for each CellPos in potentials, 
            foreach (CellPos p in potentials)
            {
                UInt16 tmp = 0;
                if (L.Mundo[(p.X - 1) % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundo[(p.X - 1) % 1000, p.Y % 1000]) { tmp++; }
                if (L.Mundo[(p.X - 1) % 1000, (p.Y + 1) % 1000]) { tmp++; }
                if (L.Mundo[p.X % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundo[p.X % 1000, (p.Y + 1) % 1000]) { tmp++; }
                if (L.Mundo[(p.X + 1) % 1000, (p.Y - 1) % 1000]) { tmp++; }
                if (L.Mundo[(p.X + 1) % 1000, p.Y % 1000]) { tmp++; }
                if (L.Mundo[(p.X + 1) % 1000, (p.Y + 1) % 1000]) { tmp++; }

                if (tmp == 3)
                {
                    nuBlessd.Add(p);
                }
            }
            
            // add new cells
            L.Cells.Clear();
            foreach (CellPos p in nuBlessd)
            {
                L.Cells.Add(p);
                L.Mundo[p.X, p.Y] = true;
            }

            // kill off cells
            foreach (CellPos p in doomed)
            {
                L.Mundo[p.X, p.Y] = false;
            }
        }

        // method to refresh the UI
        public void redraw(List<CellPos> blessed)
        {
            canvasMundos.Children.Clear();
            foreach (CellPos p in blessed)
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
