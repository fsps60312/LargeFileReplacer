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
using System.Threading;

namespace LargeFileReplacer2
{
    public class SymbolsPicker:ContentControl
    {
        const string symbols_string = "~`!@#$%^&*()_-+={[}]|\\:;\"'<,>.?/";
        //readonly static string empty_string = GetEmptyString();
        //static string GetEmptyString()
        //{
        //    var ans = "";
        //    char c = '\0';
        //    do
        //    {
        //        if (char.IsWhiteSpace(c)) ans += c;
        //        c++;
        //    } while (c != '\0');
        //    return ans;
        //}
        class TargetsDef
        {
            public bool exclude = false;
            public delegate void MatchStringChangedEventHandler();
            public event MatchStringChangedEventHandler MatchStringChanged;
            public class Picker
            {
                SortedSet<char> symbols;
                TargetsDef parent;
                public Picker(TargetsDef parent, string symbols)
                {
                    this.parent = parent;
                    this.symbols = new SortedSet<char>(symbols);
                }
                public void Toggle(bool v)
                {
                    if (v) Select(ref parent.matchString);
                    else Deselect(ref parent.matchString);
                    parent.matchDict = new HashSet<char>(parent.matchString);
                    parent.MatchStringChanged?.Invoke();
                }
                void Select(ref string s)
                {
                    var h = new SortedSet<char>(s);
                    foreach (char c in symbols)
                    {
                        if (!h.Contains(c)) s += c;
                    }
                }
                void Deselect(ref string s)
                {
                    string ans = "";
                    foreach (char c in s) if (!symbols.Contains(c)) ans += c;
                    s = ans;
                }
            }
            public TargetsDef()
            {
                space = new Picker(this, " ");
                t = new Picker(this, "\t");
                r = new Picker(this, "\r");
                n = new Picker(this, "\n");
                az = new Picker(this, "abcdefghijklmnopqrstuvwxyz");
                AZ = new Picker(this, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                digit = new Picker(this, "0123456789");
                symbol = new Picker(this, symbols_string);
            }
            public Picker space, t, r, n, az, AZ, digit, symbol;
            public bool empty = false;
            public bool chinese = false;
            private static bool IsChinese(char c)
            {
                return '\u4e00' <= c && c <= '\u9fff';
            }
            public string MatchString
            {
                get { return matchString; }
                set
                {
                    matchString = value;
                    matchDict = new HashSet<char>(matchString);
                }
            }
            private string matchString = "";
            private HashSet<char> matchDict = new HashSet<char>();
            bool IsInclude(char c)
            {
                if (empty && char.IsWhiteSpace(c)) return true;
                if (chinese && IsChinese(c)) return true;
                return matchDict.Contains(c);
            }
            public bool IsMatch(char c)
            {
                return IsInclude(c) ^ exclude;
            }
        }
        TargetsDef Targets = new TargetsDef();
        void InitializeViews()
        {
            var margin = new Thickness(0, 0, 0, 0);
            var gridEmpty = new Grid
            {
                Margin = margin,
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//␣
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//\t
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//\n
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) }//\r
                },
                Children =
                {
                    new CheckBox{Content="␣", IsChecked=false }.Set(new Action<bool>(v=>Targets.space.Toggle(v))).Set(0,0),
                    new CheckBox{Content="\\t", IsChecked=false }.Set(new Action<bool>(v=>Targets.t.Toggle(v))).Set(0,1),
                    new CheckBox{Content="\\r", IsChecked=false}.Set(new Action<bool>(v=>Targets.r.Toggle(v))).Set(0,2),
                    new CheckBox{Content="\\n", IsChecked=false }.Set(new Action<bool>(v=>Targets.n.Toggle(v))).Set(0,3)
                }
            };
            var gridSelect = new Grid
            {
                Margin = margin,
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//Empty
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star) },//grieEmpty
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//a~z
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//A~Z
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//0~9
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//Symbols
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//中文
                },
                Children =
                {
                    new CheckBox{Content="Empty", IsChecked=Targets.empty }.Set(new Action<bool>(v=>gridEmpty.IsEnabled=!( Targets.empty=v))).Set(0,0),
                    gridEmpty.Set(0,1),
                    new CheckBox{Content="a", IsChecked= false}.Set(new Action<bool>(v=>Targets.az.Toggle(v))).Set(0,2),
                    new CheckBox{Content="A", IsChecked= false}.Set(new Action<bool>(v=>Targets.AZ.Toggle(v))).Set(0,3),
                    new CheckBox{Content="0~9", IsChecked= false}.Set(new Action<bool>(v=>Targets.digit.Toggle(v))).Set(0,4),
                    new CheckBox{Content="💡",IsChecked= false,ToolTip=symbols_string }.Set(new Action<bool>(v=>Targets.symbol.Toggle(v))).Set(0,5),
                    new CheckBox{Content="中", IsChecked=false,ToolTip="[\\u4e00-\\u9fff]" }.Set(new Action<bool>(v=>Targets.chinese=v)).Set(0,6)
                }
            };
            var gridPick = new Grid
            {
                Margin = margin,
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//Exclude
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star) },//gridPick
                },
                Children =
                {
                    new CheckBox{Content="Exclude", IsChecked=Targets.exclude }.Set(new Action<bool>(v=>gridSelect.Background=new SolidColorBrush((Targets.exclude=v)?Colors.PaleVioletRed:Colors.White))).Set(0,0),
                    gridSelect.Set(0,1),
                }
            };
            var textBox = new TextBox {TextWrapping=TextWrapping.Wrap };
            Targets.MatchStringChanged += delegate
            {
                textBox.Text = Targets.MatchString;
                gridSelect.Opacity = 1;
            };
            this.Content = new Grid
            {
                Margin = margin,
                RowDefinitions =
                {
                    new RowDefinition{ Height=new GridLength(1,GridUnitType.Auto) },
                    new RowDefinition{ Height=new GridLength(1,GridUnitType.Star) }
                },
                Children =
                {
                    gridPick.Set(0,0),
                    textBox.Set(new Action<string>(s=>{Targets.MatchString=s; gridSelect.Opacity = 0.5; })).Set(1,0)
                },
            };
        }
        public SymbolsPicker()
        {
            InitializeViews();
        }
    }
    public partial class MainWindow : Window
    {
        void InitializeViews()
        {
            this.Width = 950;
            this.Content = new SymbolsPicker();
        }
        public MainWindow()
        {
            InitializeComponent();
            InitializeViews();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
