using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace EightDigital {
    public sealed partial class MainPage : Page {
        private Random rand = new Random();
        private List<int?> Board { get; set; }

        private bool UseH2Function { get; set; }

        private bool ShowProcedure { get; set; }

        private HashSet<int> ToSearchStates { get; set; }

        private HashSet<int> SearchedStates { get; set; }

        private Dictionary<int, int> GScore { get; set; }

        private Dictionary<int, int> FScore { get; set; }

        private List<int?> Origin = new List<int?> { 1, 2, 3, 8, null, 4, 7, 6, 5 };

        private int CurrentStateNum => (int)Board.Aggregate((x, y) => (x is null ? 0 : x) * 10 + (y is null ? 0 : y));

        //Number of wrong placed digits
        private int CurrentH1 => Board.Count(d => d != Origin[Board.IndexOf(d)]);

        //Block distance between each digits and its objective place
        private int CurrentH2 {
            get {
                int ans = 0;
                for (int i = 0, j, ir, ic, jr, jc; i < 9; i++) {
                    j = Origin.IndexOf(Board[i]);
                    ir = i / 3;
                    ic = i % 3;
                    jr = j / 3;
                    jc = j % 3;
                    ans += Math.Abs(ir - jr) + Math.Abs(ic - jc);
                }
                return ans;
            }
        }

        private int CurrentH => UseH2Function ? CurrentH2 : CurrentH1;

        public MainPage() {
            this.InitializeComponent();
            UseH2Function = true;
            ShowProcedure = true;
            StateTextBox.Text = "";
            RefreshButton_Click(null, null);
        }

        private bool Solvable() {
            int numOfRe = 0;
            for (int i = 0; i < 9; i++) {
                if (Board[i] is null) {
                    continue;
                }
                for (int j = 0; j < i; j++) {
                    if (Board[j] > Board[i]) {
                        numOfRe++;
                    }
                }
            }
            return (numOfRe % 2) == 1;
        }

        private void StateToBoard(int state) {
            int i = 8;
            if (state < 100000000) {
                Board[i--] = null;
            }
            for (; i >= 0; i--) {
                if (state % 10 == 0) {
                    Board[i] = null;
                }
                else {
                    Board[i] = state % 10;
                }
                state /= 10;
            }
        }

        private List<int> GetNeighbors(int current) {
            int DraftToState(int?[,] draft) {
                int state = 0;
                for (int i = 0; i < 3; i++) {
                    for (int j = 0; j < 3; j++) {
                        state += draft[i, j] is null ? 0 : (int)draft[i, j];
                        if ((j + i * 3) < 8) {
                            state *= 10;
                        }
                    }
                }
                return state;
            }

            List<int> ans = new List<int>();
            int?[,] Draft = new int?[3, 3];
            int r = 0, c = 0;
            int? temp;
            StateToBoard(current);
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    Draft[i, j] = Board[j + i * 3];
                    if (Draft[i, j] == null) {
                        r = i;
                        c = j;
                    }
                }
            }

            //up
            if (r > 0) {
                temp = Draft[r - 1, c];
                Draft[r - 1, c] = Draft[r, c];
                Draft[r, c] = temp;
                ans.Add(DraftToState(Draft));
                temp = Draft[r - 1, c];
                Draft[r - 1, c] = Draft[r, c];
                Draft[r, c] = temp;
            }
            //down
            if (r < 2) {
                temp = Draft[r + 1, c];
                Draft[r + 1, c] = Draft[r, c];
                Draft[r, c] = temp;
                ans.Add(DraftToState(Draft));
                temp = Draft[r + 1, c];
                Draft[r + 1, c] = Draft[r, c];
                Draft[r, c] = temp;
            }
            //left
            if (c > 0) {
                temp = Draft[r, c - 1];
                Draft[r, c - 1] = Draft[r, c];
                Draft[r, c] = temp;
                ans.Add(DraftToState(Draft));
                temp = Draft[r, c - 1];
                Draft[r, c - 1] = Draft[r, c];
                Draft[r, c] = temp;
            }
            //right
            if (c < 2) {
                temp = Draft[r, c + 1];
                Draft[r, c + 1] = Draft[r, c];
                Draft[r, c] = temp;
                ans.Add(DraftToState(Draft));
                temp = Draft[r, c + 1];
                Draft[r, c + 1] = Draft[r, c];
                Draft[r, c] = temp;
            }

            return ans;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) {
            ToSearchStates = new HashSet<int>();
            SearchedStates = new HashSet<int>();
            GScore = new Dictionary<int, int>();
            FScore = new Dictionary<int, int>();

            if (StateTextBox.Text != "") {
                StateToBoard(int.Parse(StateTextBox.Text));
                if (Solvable()) {
                    StateTextBox.Text = "";
                }
                else {
                    StateTextBox.Text = "No solution!";
                    return;
                }
            }
            else {
                Board = Origin.OrderBy(d => rand.Next()).ToList();
                while (!Solvable()) {
                    Board = Origin.OrderBy(d => rand.Next()).ToList();
                }
            }
            ToSearchStates.Add(CurrentStateNum);
            GScore[CurrentStateNum] = 0;
            FScore[CurrentStateNum] = CurrentH;
            Bindings.Update();
        }

        private async void NextStepButton_Click(object sender, RoutedEventArgs e) {
            bool? continuous = (e.OriginalSource as Button).Tag as bool?;
        start:
            int current = 0;
            int minFS = int.MaxValue;
            foreach (int s in ToSearchStates) {
                if (FScore[s] < minFS) {
                    minFS = FScore[s];
                    current = s;
                }
            }
            StateToBoard(current);
            Bindings.Update();
            if (ShowProcedure) {
                await Task.Delay(10);
            }
            if (CurrentH == 0) {
                return;
            }

            ToSearchStates.Remove(current);
            SearchedStates.Add(current);

            foreach (int neighbor in GetNeighbors(current)) {
                if (SearchedStates.Contains(neighbor)) {
                    continue;
                }
                else {
                    StateToBoard(neighbor);
                    int tentativeGScore = GScore[current] + 1;
                    if (!GScore.ContainsKey(neighbor)) {
                        GScore[neighbor] = int.MaxValue;
                    }
                    if (tentativeGScore < GScore[neighbor]) {
                        GScore[neighbor] = tentativeGScore;
                        FScore[neighbor] = tentativeGScore + CurrentH;
                        if (!ToSearchStates.Contains(neighbor)) {
                            ToSearchStates.Add(neighbor);
                        }
                    }
                }
            }
            if (continuous is true) {
                goto start;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) {
            (e.OriginalSource as Button).Tag = true;
            NextStepButton_Click(sender, e);
        }

        private void StateTextBox_KeyUp(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter) {
                RefreshButton_Click(sender, null);
            }
        }
    }

    //Utils
    public sealed partial class MainPage : Page {
        private Dictionary<int?, int?> ToOldOrder = new Dictionary<int?, int?> {
            { 1, 1 },
            { 2, 2 },
            { 3, 3 },
            { 8, 4 },
            {-1, 5 },
            {4, 6 },
            {7, 7 },
            {6, 8 },
            {5, -1 }
        };

        private SolidColorBrush SkyBlueBrush = new SolidColorBrush(Windows.UI.Colors.SkyBlue);
        private SolidColorBrush WhiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
        private SolidColorBrush GetColor(int? x) => x is null ? WhiteBrush : SkyBlueBrush;
        private int? b00 => Board[0];

        private int? b01 => Board[1];

        private int? b02 => Board[2];

        private int? b10 => Board[3];

        private int? b11 => Board[4];

        private int? b12 => Board[5];

        private int? b20 => Board[6];

        private int? b21 => Board[7];

        private int? b22 => Board[8];
    }

}
