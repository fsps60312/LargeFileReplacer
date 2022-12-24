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
using System.IO;

namespace LargeFileReplacer2
{
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
    class SymbolsPicker:ContentControl
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
        TargetsDef Targets;
        TargetsDef.Picker space, t, r, n, az, AZ, digit, symbol;
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
                    new CheckBox{Content="␣", IsChecked=false }.Set(new Action<bool>(v=>space.Toggle(v))).Set(0,0),
                    new CheckBox{Content="\\t", IsChecked=false }.Set(new Action<bool>(v=>t.Toggle(v))).Set(0,1),
                    new CheckBox{Content="\\r", IsChecked=false}.Set(new Action<bool>(v=>r.Toggle(v))).Set(0,2),
                    new CheckBox{Content="\\n", IsChecked=false }.Set(new Action<bool>(v=>n.Toggle(v))).Set(0,3)
                }
            };
            var gridToggle = new Grid
            {
                Margin = margin,
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star) },//grieEmpty
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//a~z
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//A~Z
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//0~9
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//Symbols
                },
                Children =
                {
                    gridEmpty.Set(0,0),
                    new CheckBox{Content="a", IsChecked= false}.Set(new Action<bool>(v=>az.Toggle(v))).Set(0,1),
                    new CheckBox{Content="A", IsChecked= false}.Set(new Action<bool>(v=>AZ.Toggle(v))).Set(0,2),
                    new CheckBox{Content="0~9", IsChecked= false}.Set(new Action<bool>(v=>digit.Toggle(v))).Set(0,3),
                    new CheckBox{Content="💡",IsChecked= false,ToolTip=symbols_string }.Set(new Action<bool>(v=>symbol.Toggle(v))).Set(0,4)
                }
            };
            var gridSelect = new Grid
            {
                Margin = margin,
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//Empty
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//中文
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//gridToggle
                },
                Children =
                {
                    new CheckBox{Content="Empty", IsChecked=Targets.empty ,ToolTip="char.IsWhiteSpace(c)"}.Set(new Action<bool>(v=>gridEmpty.IsEnabled=!( Targets.empty=v))).Set(0,0),
                    new CheckBox{Content="中", IsChecked=false,ToolTip="[\\u4e00-\\u9fff]" }.Set(new Action<bool>(v=>Targets.chinese=v)).Set(0,1),
                    gridToggle.Set(0,2)
                }
            };
            var textBox = new TextBox { TextWrapping = TextWrapping.Wrap ,ToolTip="Charactors to be selected.\nDuplicated charactors are acceptable so that you can directly paste an article here."};
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
                    new CheckBox{Content="Inverse", IsChecked=Targets.exclude,ToolTip="Selected charactors will be treated as unselected, and vice versa." }.Set(new Action<bool>(
                        v=>gridSelect.Background=textBox.Background=new SolidColorBrush((Targets.exclude=v)?Colors.LightGray:Colors.White))).Set(0,0),
                    gridSelect.Set(0,1),
                }
            };
            Targets.MatchStringChanged += delegate
            {
                textBox.Text = Targets.MatchString;
                gridToggle.Opacity = 1;
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
                    textBox.Set(new Action<string>(s=>{Targets.MatchString=s; gridToggle.Opacity = 0.5; })).Set(1,0)
                },
            };
        }
        public SymbolsPicker(TargetsDef targetsDef)
        {
            this.Targets = targetsDef;
            InitializeViews();
            space = new TargetsDef.Picker(Targets, " ");
            t = new TargetsDef.Picker(Targets, "\t");
            r = new TargetsDef.Picker(Targets, "\r");
            n = new TargetsDef.Picker(Targets, "\n");
            az = new TargetsDef.Picker(Targets, "abcdefghijklmnopqrstuvwxyz");
            AZ = new TargetsDef.Picker(Targets, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            digit = new TargetsDef.Picker(Targets, "0123456789");
            symbol = new TargetsDef.Picker(Targets, symbols_string);
        }
    }
    public partial class MainWindow : Window
    {
        Stream OpenFileRead()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true) return dialog.OpenFile();
            return null;
        }
        Stream OpenFileWrite()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            if (dialog.ShowDialog() == true) return dialog.OpenFile();
            return null;
        }
        void InitializeViews()
        {
            this.Width = 950;
            var btn = new Button { Content="Start"};
            btn.Click +=async delegate
              {
                  var t = new TargetsDef();
                  t.MatchString = " ";
                  StreamReadPipe pipe1 = new StreamReadPipe(OpenFileRead());
                  var pipe2 = new ReplacePipe(pipe1.ClientHandleString, t) { replaceTo = "-" };
                  StreamWritePipe pipe3 = new StreamWritePipe(pipe2.ClientHandleString, OpenFileWrite());
                  new Thread(() => pipe1.Start()).Start();
                  new Thread(() => pipe2.Start()).Start();
                  new Thread(() => pipe3.Start()).Start();
                  while (true)
                  {
                      await Task.Delay(500);
                      this.Title = $"{pipe1}, {pipe2}, {pipe3}";
                  }
              };
            this.Content = btn;
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
