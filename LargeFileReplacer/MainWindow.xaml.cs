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
using System.IO;
using System.Threading;

namespace LargeFileReplacer
{
    public static class Extensions
    {
        public static UIElement Set(this UIElement u, int row, int column)
        {
            Grid.SetRow(u, row);
            Grid.SetColumn(u, column);
            return u;
        }
        public static UIElement SetSpan(this UIElement u, int rowSpan, int columnSpan)
        {
            Grid.SetRowSpan(u, rowSpan);
            Grid.SetColumnSpan(u, columnSpan);
            return u;
        }
        public static Button Set(this Button button, Action<Button> action)
        {
            button.FontSize = 15;
            button.Margin = new Thickness(2, 0, 2, 0);
            button.Click += delegate { action(button); };
            return button;
        }
        public static CheckBox Set(this CheckBox chb, Action<bool> action)
        {
            chb.FontSize = 15;
            chb.Margin = new Thickness(2, 0, 2, 0);
            chb.Checked += delegate { action(true); };
            chb.Unchecked += delegate { action(false); };
            return chb;
        }
        public static TextBox Set(this TextBox txb, Action<string> action)
        {
            txb.FontSize = 30;
            txb.TextChanged += delegate { action(txb.Text); };
            return txb;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static class Targets
        {
            public static bool exclude = false;
            public static bool empty = false, space = false, t = false, r = false, n = false
                , az = false, AZ = false, digit = false, chinese = false, ds = false, symbol = false;
            private static bool IsChinese(char c)
            {
                return '\u4e00' <= c && c <= '\u9fff';
            }
            public static string replaceTo="";
            public static HashSet<char> dict = new HashSet<char>();
            static HashSet<char> symbolDict = new HashSet<char>("~`!@#$%^&*()_-+={[}]|\\:;\"'<,>.?/");
            static bool IsInclude(char c)
            {
                if (empty && char.IsWhiteSpace(c)) return true;
                if ((space && c == ' ') || (t && c == '\t') || (r && c == '\r') || (n && c == '\n')) return true;
                if ((az && char.IsLower(c)) || (AZ && char.IsUpper(c)) || (digit && char.IsDigit(c))) return true;
                if ((chinese && IsChinese(c)) || (symbol && symbolDict.Contains(c))) return true;
                return dict.Contains(c);
            }
            public static bool IsMatch(char c)
            {
                if (ds) return DoubleSpaced(c);//must run first
                return IsInclude(c) ^ exclude;
            }
            static bool spaced = false;
            public static void Init() { spaced = false; }
            public static bool DoubleSpaced(char c)
            {
                bool ans = spaced && c == ' ';
                spaced = (c == ' ');
                return ans;
            }
        }

        async Task Replace(StreamReader reader,StreamWriter writer)
        {
            Targets.Init();
            DateTime lastUpdateTime = DateTime.Now;
            long cnt = 0;
            const int chunkSize = 100000;
            while(true)
            {
                var buffer = new char[chunkSize];
                var n =await reader.ReadAsync(buffer, 0, buffer.Length);
                if (n == 0) break;
                cnt += n;
                Array.Resize(ref buffer, n);
                await writer.WriteAsync(await Task.Run(() => Replace_SubMethod(buffer)));
                var time = DateTime.Now;
                if((time-lastUpdateTime).TotalSeconds>0.1)
                {
                    lastUpdateTime = time;
                    this.Title = $"Processing {cnt} / {reader.BaseStream.Length} ({(100.0 * cnt / reader.BaseStream.Length).ToString("F3")}%)";
                }
            }
            this.Title = $"OK - {DateTime.Now}";
        }

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
        string Replace_SubMethod(char[]buffer)
        {
            StringBuilder ans = new StringBuilder();
            foreach(var c in buffer)
            {
                if (Targets.IsMatch((char)c)) ans.Append(Targets.replaceTo);
                else ans.Append(c);
            }
            return ans.ToString();
        }
        async Task ReplaceMultiThread(StreamReader reader, StreamWriter writer)
        {
            await Task.Run(() =>
            {
                Targets.Init();
                DateTime lastUpdateTime = DateTime.Now;
                long cnt = 0;
                const int chunkSize = 100000;
                ThreadPool.SetMaxThreads(50, 50);
                var runningCount = 1;
                AutoResetEvent done = new AutoResetEvent(false);
                Dictionary<int, string> results = new Dictionary<int, string>();
                int writeIndex = 0;
                for (int readIndex=0; ;readIndex++)
                {
                    var buffer = new char[chunkSize];
                    var n = reader.ReadBlock(buffer, 0, buffer.Length);
                    if (n == 0) break;
                    cnt += n;
                    Array.Resize(ref buffer, n);
                    var time = DateTime.Now;
                    if ((time - lastUpdateTime).TotalSeconds > 0.1)
                    {
                        lastUpdateTime = time;
                        Dispatcher.Invoke(() => this.Title = $"Processing {cnt} / {reader.BaseStream.Length} ({(100.0 * cnt / reader.BaseStream.Length).ToString("F3")}%)");
                    }
                    Interlocked.Increment(ref runningCount);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(o=>
                    {
                        var result = Replace_SubMethod((char[])((object[])o)[1]);
                        lock(results)
                        {
                            results.Add((int)((object[])o)[0], result);
                            while(results.ContainsKey(writeIndex))
                            {
                                var str = results[writeIndex];
                                results.Remove(writeIndex);
                                writer.Write(str);
                                writeIndex++;
                            }
                        }
                        if (0 == Interlocked.Decrement(ref runningCount)) done.Set();
                    }), new object[] { readIndex, buffer });
                }
                if (0 == Interlocked.Decrement(ref runningCount)) done.Set();
                done.WaitOne();
                
                Dispatcher.Invoke(() => this.Title = $"OK - {DateTime.Now}");
            });
        }
        async Task RunMultiThread()
        {
            using (var readStream = OpenFileRead())
            {
                if (readStream == null) return;
                using (var writeStream = OpenFileWrite())
                {
                    if (writeStream == null) return;
                    using (var reader = new StreamReader(readStream, Encoding.UTF8))
                    {
                        using (var writer = new StreamWriter(writeStream, Encoding.UTF8))
                        {
                            await ReplaceMultiThread(reader, writer);
                        }
                    }
                }
            }
        }
        async Task Run()
        {
            using (var readStream = OpenFileRead())
            {
                if (readStream == null) return;
                using (var writeStream = OpenFileWrite())
                {
                    if (writeStream == null) return;
                    using (var reader = new StreamReader(readStream, Encoding.UTF8))
                    {
                        using (var writer = new StreamWriter(writeStream, Encoding.UTF8))
                        {
                            await Replace(reader, writer);
                        }
                    }
                }
            }
        }

        void InitializeViews()
        {
            this.Width = 900;
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
                Children=
                {
                    new CheckBox{Content="␣", IsChecked=Targets.space }.Set(new Action<bool>(v=>Targets.space=v)).Set(0,0),
                    new CheckBox{Content="\\t", IsChecked=Targets.t }.Set(new Action<bool>(v=>Targets.t=v)).Set(0,1),
                    new CheckBox{Content="\\r", IsChecked=Targets.r }.Set(new Action<bool>(v=>Targets.r=v)).Set(0,2),
                    new CheckBox{Content="\\n", IsChecked=Targets.n }.Set(new Action<bool>(v=>Targets.n=v)).Set(0,3)
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
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//MultiThread
                },
                Children =
                {
                    new CheckBox{Content="Empty", IsChecked=Targets.empty }.Set(new Action<bool>(v=>gridEmpty.IsEnabled=!( Targets.empty=v))).Set(0,0),
                    gridEmpty.Set(0,1),
                    new CheckBox{Content="a~z", IsChecked=Targets.az }.Set(new Action<bool>(v=>Targets.az=v)).Set(0,2),
                    new CheckBox{Content="A~Z", IsChecked=Targets.AZ }.Set(new Action<bool>(v=>Targets.AZ=v)).Set(0,3),
                    new CheckBox{Content="0~9", IsChecked=Targets.digit }.Set(new Action<bool>(v=>Targets.digit=v)).Set(0,4),
                    new CheckBox{Content="Symbols",IsChecked=Targets.symbol}.Set(new Action<bool>(v=>Targets.symbol=v)).Set(0,5),
                    new CheckBox{Content="中文", IsChecked=Targets.chinese }.Set(new Action<bool>(v=>Targets.chinese=v)).Set(0,6),
                    new Button{Content="MultiThread"}.Set(new Action<Button>(async btn=>
                    {
                        btn.IsEnabled=false;
                        try{ await RunMultiThread(); }
                        catch(Exception error){MessageBox.Show(error.ToString()); }
                        finally{btn.IsEnabled=true; }
                    })).Set(0,7),
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
            int columnSpan = 3;
            this.Content = new Grid
            {
                Margin = margin,
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//Open
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star) },//Pick
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto) },//double space
                },
                RowDefinitions =
                {
                    new RowDefinition{ Height=new GridLength(1,GridUnitType.Auto) },
                    new RowDefinition{ Height=new GridLength(1,GridUnitType.Star) },
                    new RowDefinition{ Height=new GridLength(1,GridUnitType.Star) }
                },
                Children =
                {
                    new Button{Content="Open"}.Set(new Action<Button>(async btn=>
                    {
                        btn.IsEnabled=false;
                        try{ await Run(); }
                        catch(Exception error){MessageBox.Show(error.ToString()); }
                        finally{btn.IsEnabled=true; }
                    })).Set(0,0),
                    gridPick.Set(0,1),
                    new CheckBox{Content="Double Space", IsChecked=Targets.ds }.Set(new Action<bool>(v=>gridPick.IsEnabled=!( Targets.ds=v))).Set(0,2),
                    new TextBox{Text=string.Join("", Targets.dict) }.Set(new Action<string>(s=>Targets.dict=new HashSet<char>( s))).Set(1,0).SetSpan(1,columnSpan),
                    new TextBox{ Text=Targets.replaceTo}.Set(new Action<string>(s=>Targets.replaceTo=s)).Set(2,0).SetSpan(1,columnSpan)
                },
            };
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
