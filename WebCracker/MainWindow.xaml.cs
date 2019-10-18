using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using System.Resources;
using System.Text.RegularExpressions;
using System.Collections;
using System.Runtime.Serialization.Json;
using System.Web;

namespace WebCracker
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public Boolean stop = true;//是否停止
        public Boolean pause = false;//是否暂停
        public string result = "NOT FOUND!";//测试结果
        public int count = 0;//已测数量
        public string testing_pass = "";//正在测试的值
        public string status = "";//当前测试状态
        public MainWindow()
        {
            InitializeComponent();
            ProgressBar.Visibility = Visibility.Hidden;
            // DONE: 1-多线程字典分隔
            // DONE: 1-暂停、加载、结束
            // DONE: 2-关于页面
            // DONE: 5-延迟时间设置
            // DONE: 3-根据状态码、响应长度判断登录成功
            // DONE: 1-网络错误处理
            // DONE: 1-更新readme、注释及关于页面

            // UNDONE: 4-发布程序  Advanced Installer、inno setup、rar自解压

            // TODO: 5-多用户
            // TODO: 6-自动解析用户/密码关键字
            // TODO: 7-账号密码支持加密算法
            // TODO: 8-多种认证方式
            // TODO: 9-可选项折叠
            // TODO: 9-集成dirmap
            // TODO: 9-请求方式添加GET
            // TODO: 9-添加代理
        }
        private void init_state()
        {
            //开始时初始化状态
            stop = false;//是否停止
            pause = false;//是否暂停
            result = "NOT FOUND!";//爆破结果
            status = "";//当前状态
            count = 0;//已测数量
        }
        private Boolean Check_Config()
        {
            //开始前参数校验
            if (!stop && !pause)
            {
                return false;//已经开始时不能点击开始
            }
            TextBox[] NotEmptyBox = { TextURL, TextPassDict, TextUsername, TextUserKey, TextPassKey };
            foreach (TextBox box in NotEmptyBox)
            {
                if (box.Text.Length == 0)
                {
                    TextStatus.Text = Properties.Resources.StringTextStatus2;
                    return false;//必填项校验
                }
            }
            //线程、超时及延迟格式校验
            if (TextThreads.Text.Length > 0)
            {
                Regex re = new Regex("^[1-9][0-9]?$");
                if (!re.IsMatch(TextThreads.Text))
                {
                    TextStatus.Text = Properties.Resources.StringTextStatus3;
                    return false;
                }
            }
            if (TextTimeout.Text.Length > 0)
            {
                Regex re = new Regex("^[1-9][0-9]{0,2}$");
                if (!re.IsMatch(TextTimeout.Text)) {
                    TextStatus.Text = Properties.Resources.StringTextStatus3;
                    return false;
                }
            }
            if (TextDelay.Text.Length > 0)
            {
                Regex re = new Regex("^[1-9][0-9]{0,2}$");
                if (!re.IsMatch(TextDelay.Text))
                {
                    TextStatus.Text = Properties.Resources.StringTextStatus3;
                    return false;
                }
            }
            if (!File.Exists(TextPassDict.Text))
            {
                TextStatus.Text = Properties.Resources.StringTextStatus4;
                return false;//校验密码字典是否存在
            }
            //关键字非自动模式时不能为空，且有个是校验
            if (ComboBoxModel.SelectedIndex!=0)
            {
                if (ComboBoxJudegType.SelectedIndex == 0)
                {
                    Regex re = new Regex("^[1-9][0-9]*$");
                    if (!re.IsMatch(TextKey.Text))
                    {
                        TextStatus.Text = Properties.Resources.StringTextStatus3;
                        return false;
                    }
                }
                else if (ComboBoxJudegType.SelectedIndex == 1 && TextKey.Text.Length==0)
                {
                    TextStatus.Text = Properties.Resources.StringTextStatus3;
                    return false;
                }
                else if (ComboBoxJudegType.SelectedIndex == 2)
                {
                    Regex re = new Regex("^[1-5][0-9]{2}$");
                    if (!re.IsMatch(TextKey.Text))
                    {
                        TextStatus.Text = Properties.Resources.StringTextStatus3;
                        return false;
                    }
                }
            }
            return true;
        }
        private void Set_Underline(TextBox obj, string color)
        {
            BrushConverter brushConverter = new BrushConverter();
            Brush brush = (Brush)brushConverter.ConvertFromString(color);
            obj.BorderBrush = brush;
            obj.CaretBrush = brush;//必填项未填时设置边框及光标为红色
        }
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (!this.stop)//停止测试
            {
                this.stop = true;
                count = 0;
                testing_pass = "";
                status = Properties.Resources.StringTextStatus6;
                TextStatus.Text = status;
                ProgressBar.Value = count;
                ProgressBar.Visibility = Visibility.Hidden;
                //不准备重置开始密码
            }
        }
        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            //暂停的本质是将当前测试值置为开始密码，停止所有线程，保存测试配置，下次测试时重新从开始密码测试
            if (!pause&&!stop)//已经开始时才能暂停
            {
                this.pause = true;
                TextStart.Text = testing_pass;
                status = Properties.Resources.StringTextStatus5;
                TextStatus.Text = status;
            }
        }
        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (stop||pause)//没有开始才能加载
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    string config_filename = openFileDialog.FileName;
                    if (File.Exists(config_filename))
                    {
                        string jsonString = System.IO.File.ReadAllText(config_filename);
                        var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
                        DataContractJsonSerializer serializer1 = new DataContractJsonSerializer(typeof(Hashtable));
                        Hashtable ht = (Hashtable)serializer1.ReadObject(ms);
                        TextURL.Text = (string)ht["url"];
                        TextPassDict.Text = (string)ht["passdict"];
                        TextUsername.Text = (string)ht["username"];
                        TextUserKey.Text = (string)ht["userkey"];
                        TextPassKey.Text = (string)ht["passkey"];
                        TextKey.Text = (string)ht["key"];
                        TextThreads.Text = (string)ht["threads"];
                        TextTimeout.Text = (string)ht["timeout"];
                        TextStart.Text = (string)ht["start"];
                        TextCookies.Text = (string)ht["cookies"];
                        TextHeaders.Text = (string)ht["headers"];
                        TextData.Text = (string)ht["data"];
                        status = config_filename + Properties.Resources.StringTextStatus8;
                        TextStatus.Text = status;
                    }
                }
            }
        }
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Hashtable ht = new Hashtable//将当前测试配置保存为hashtable,并序列化为json保存
            {
                { "url", TextURL.Text },
                { "passdict", TextPassDict.Text },
                { "username", TextUsername.Text },
                { "userkey", TextUserKey.Text },
                { "passkey", TextPassKey.Text },
                { "key", TextKey.Text },
                { "threads", TextThreads.Text },
                { "timeout", TextTimeout.Text },
                { "cookies", TextCookies.Text },
                { "headers", TextHeaders.Text },
                { "data", TextData.Text },
                { "start", pause ? testing_pass: TextStart.Text}
            };
            var ms = new MemoryStream();
            new DataContractJsonSerializer(ht.GetType()).WriteObject(ms, ht);
            string json = Encoding.UTF8.GetString(ms.ToArray());
            var openFileDialog = new Microsoft.Win32.SaveFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(openFileDialog.FileName, json);
                status = openFileDialog.FileName + Properties.Resources.StringTextStatus7;
                TextStatus.Text = status;
            }
        }
        private Tuple<int, string> request(string url, int timeout_int, string cookies, string headers, string data_str)
        {// 发送http请求，返回状态码及响应结果
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            string response;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            if (timeout_int > 0) req.Timeout = timeout_int;
            if (cookies.Length > 0) req.Headers.Add("Cookie", cookies);
            if (headers.Length > 0)
            {
                foreach (string header in headers.Split('\n'))
                {
                    //c#真麻烦,python大法好
                    string header_t = header.Trim();
                    string header_key = header_t.Split(':')[0].Trim(), header_value = header_t.Split(':')[1].Trim();
                    header_key = header_key.Substring(0, 1).ToUpper() + header_key.Substring(1);
                    if (header_key.Equals("User-Agent")) req.UserAgent = header_value;
                    else if (header_key.Equals("Accept")) req.Accept = header_value;
                    else if (header_key.Equals("Connection") && header_value.Equals("keep-alive")) req.KeepAlive = true;
                    else if (header_key.Equals("Connection") && !header_value.Equals("keep-alive")) req.KeepAlive = false;
                    else if (header_key.Equals("Content-Length")) continue;
                    else if (header_key.Equals("Content-Type")) continue;
                    else if (header_key.Equals("Expect")) req.Expect = header_value;
                    else if (header_key.Equals("Host")) req.Host = header_value;
                    else if (header_key.Equals("If-Modified-Since")) continue;//需要格式化日期，但是不能确定日期格式，不处理
                    else if (header_key.Equals("Range")) continue;//range处理起来太麻烦，也不怎么用，不处理
                    else if (header_key.Equals("Referer")) req.Referer = header_value;
                    else if (header_key.Equals("Transfer-Encoding")) req.TransferEncoding = header_value;
                    else req.Headers.Add(header_key, header_value);
                }
            }
            byte[] data = Encoding.UTF8.GetBytes(WebUtility.UrlEncode(data_str));
            req.ContentLength = data.Length;
            try
            {
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
            }
            catch (WebException error)
            {
                this.Dispatcher.Invoke(new Action(() => TextStatus.Text = Properties.Resources.StringTextStatus12 + error.Message));
                return new Tuple<int, string>(0, "");//请求出错时状态码设为0
            }

            int status_code = 0;
            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
                //网页出现404、500等会直接抛出WebException异常,奇葩啊
            }
            catch (WebException error)
            {
                this.Dispatcher.Invoke(new Action(() => TextStatus.Text = Properties.Resources.StringTextStatus12 + error.Message));
                resp = (HttpWebResponse)error.Response;
            }
            if (resp is null)
            {
                return new Tuple<int, string>(0, "");
            }
            status_code = (int)resp.StatusCode;
            Stream stream = resp.GetResponseStream();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                response = reader.ReadToEnd();
                resp.Close();
            }
            return new Tuple<int, string>(status_code, response);
        }
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (!Check_Config()) return;//参数校验未通过不能开始
            init_state();//初始化状态
            string url = TextURL.Text;//登录接口
            string passDict = TextPassDict.Text;//密码字典路径
            string[] passwords = System.IO.File.ReadAllLines(passDict);
            string start = TextStart.Text;//开始密码
            int all_pass_length = passwords.Length;//所有密码的长度
            if (start.Length > 0)
            {
                int index = passwords.ToList().IndexOf(start);
                if (index >= 0)
                {
                    passwords = passwords.Skip(index).Take(passwords.Length-index).ToArray();
                    count += index;
                }
            }
            int need_pass_length = passwords.Length;//本次需要测试的密码长度
            string username = TextUsername.Text;//用户名
            string userKey = TextUserKey.Text;//用户名关键字
            string passKey = TextPassKey.Text;//密码关键字
            int judgeType = ComboBoxJudegType.SelectedIndex;//登录成功判断类型，0-响应长度，1-关键字，2-状态码
            int model = ComboBoxModel.SelectedIndex;//判断模式，0-自动，1-包含，2-排除
            string key = TextKey.Text;//关键字
            int judge_length = -1;//响应长度
            int judge_code = 0;//状态码
            if (judgeType == 0 && model != 0)//根据判断类型转换预期结果
            {
                judge_length = Convert.ToInt32(key);
            }else if (judgeType == 2 && model != 0)
            {
                judge_code = Convert.ToInt32(key);
            }
            string threads = TextThreads.Text;//线程
            int threads_int = 1;
            if (threads.Length > 0) threads_int = Convert.ToInt32(threads);
            string timeout = TextTimeout.Text;//超时时间
            int timeout_int = 0;
            if (timeout.Length > 0) timeout_int = Convert.ToInt32(timeout) * 1000;
            string delay = TextDelay.Text;//延迟时间
            int delay_int = 0;
            if (delay.Length > 0) delay_int = Convert.ToInt32(delay);
            string cookies = TextCookies.Text;//cookies
            string headers = TextHeaders.Text;//headers
            string data_text = TextData.Text;//data
            int thread_pass_length = need_pass_length / threads_int;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                this.Dispatcher.Invoke(new Action(() => ProgressBar.Visibility = Visibility.Visible));//更新进度条
                this.Dispatcher.Invoke(new Action(() => ProgressBar.Maximum = all_pass_length));
            });
            //密码分组，各线程均匀分布，保证排在字典前列的密码优先测试
            List<List<string>> list = new List<List<string>>(threads_int);
            for (int i = 0; i < threads_int; i++)
            {
                List<string> strList = new List<string>();
                list.Add(strList);
            }
            for (int i = 0; i < need_pass_length; i++)
            {
                list[i % threads_int].Add(passwords[i]);
            }
            // 请求两次错误的密码，根据响应结果判断能否使用自动模式
            string data_str1 = userKey + "=" + username + "&" + passKey + "=" + "011fc2994e39d251141540f87a69092b3f22a86767f7283de7eeedb3897bedf6";//abcdefghijklmnopqrstuvwxyz0123456789的sha256
            if (data_text.Length > 0) data_str1 += "&" + data_text;
            Tuple<int, string> tuple1 = request(url, timeout_int, cookies, headers, data_str1);
            int Error_Status_Code1 = tuple1.Item1;
            string Error_Status_Response1 = tuple1.Item2;
            if (Error_Status_Code1 == 0) {
                stop = true;
                return;
            }//请求出错时结束测试
            string data_str2 = userKey + "=" + username + "&" + passKey + "=" + "2d18c15e22115104be36dd6a16fc170a3385e6e879e76fb91bf9fdb4d3f38243";//ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789的sha256
            if (data_text.Length > 0) data_str2 += "&" + data_text;
            Tuple<int, string> tuple2 = request(url, timeout_int, cookies, headers, data_str2);
            int Error_Status_Code2 = tuple2.Item1;
            string Error_Status_Response2 = tuple2.Item2;
            //根据两次错误的密码判断是否可是使用自动模式
            if (model == 0 && judgeType == 0 && Error_Status_Response1.Length != Error_Status_Response2.Length)
            {
                status = Properties.Resources.StringTextStatus9;
            }else if (model == 0 && judgeType == 0 && Error_Status_Response1 != Error_Status_Response2)
            {
                status = Properties.Resources.StringTextStatus10;
            }
            else if (model == 0 && judgeType == 0 && Error_Status_Code1 != Error_Status_Code2)
            {
                status = Properties.Resources.StringTextStatus11;
            }
            if (status.Length > 0)
            {
                TextStatus.Text = status;
                return;
            }
            List<Boolean> Threads_Status = new List<Boolean> { };//线程状态，判断是否结束
            try
            {
                for (int i = 0; i < threads_int; i++)
                {
                    Threads_Status.Add(false);
                    int Thread_Index = i;
                    //另一种密码分组算法，非均匀分布，而是每个线程从前向后取固定长度
                    //string[] password_list = passwords.Skip(i * thread_pass_length).Take(thread_pass_length).ToArray();
                    string[] password_list = list[i].ToArray();
                    ThreadPool.QueueUserWorkItem(_ => {
                        foreach (string password in password_list)
                        {
                            if (!stop && !pause)
                            {
                                count += 1;
                                testing_pass = password;
                                status = "正在尝试: " + Convert.ToString(count) + "/" + Convert.ToString(all_pass_length) + " " + username + "/" + password;
                                this.Dispatcher.Invoke(new Action(() => TextStatus.Text = status));
                                this.Dispatcher.Invoke(new Action(() => ProgressBar.Value = count));//更新状态栏
                                string data_str = userKey + "=" + username + "&" + passKey + "=" + password;
                                if (data_text.Length > 0) data_str += "&" + data_text;
                                Tuple<int, string> tuple = request(url, timeout_int, cookies, headers, data_str);
                                int status_code = tuple.Item1;//状态码
                                string response = tuple.Item2;//响应体
                                Boolean success = false;//判断是否登录成功
                                if (judgeType == 0)
                                {
                                    if (model == 0 && response.Length != Error_Status_Response1.Length) success = true;
                                    else if (model == 1 && response.Length == judge_length) success = true;
                                    else if (model == 2 && response.Length != judge_length) success = true;
                                }
                                else if (judgeType == 1)
                                {
                                    if (model == 0 && response != Error_Status_Response1) success = true;
                                    else if (model == 1 && response.Contains(key)) success = true;
                                    else if (model == 2 && !response.Contains(key)) success = true;
                                }
                                else if (judgeType == 2)
                                {
                                    if (model == 0 && status_code != Error_Status_Code1) success = true;
                                    else if (model == 1 && status_code == judge_code) success = true;
                                    else if (model == 2 && status_code != judge_code) success = true;
                                }
                                // 如果登录成功则修改状态及结果
                                if (success)
                                {
                                    result = "Found: " + username + "/" + password;
                                    stop = true;
                                    break;
                                }
                                if (delay_int != 0) Thread.Sleep(delay_int);//如果有延迟时间则等待
                            }
                            else return;//如果已暂停或已停止则返回
                        }
                        Threads_Status[Thread_Index] = true;//当前线程已结束
                        Boolean all_over = true;//判断是否所有线程均结束,主要用于所有线程都没找到密码的情况
                        foreach (Boolean t in Threads_Status)
                        {
                            all_over &= t;
                        }
                        if (stop || all_over)
                        {//成功或全部失败则更新状态栏
                            stop = true;
                            status = result;
                            this.Dispatcher.Invoke(new Action(() => TextStatus.Text = status));
                            this.Dispatcher.Invoke(new Action(() => ProgressBar.Value = all_pass_length));
                        }
                    });
                }
            }
            catch (Exception error)
            {
                TextStatus.Text = error.Message;
            }
        }
        private void ButtonAbout_Click(object sender, RoutedEventArgs e)
        {
            this.TabItem2.IsSelected = true;//关于页面
        }
        private void ButtonLoadPassDict_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();//加载密码字典
            if (openFileDialog.ShowDialog() == true)
            {
                this.TextPassDict.Text = openFileDialog.FileName;
            }
        }
        private void Text_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox box = (TextBox)sender;//必填项不能为空，为空时置为红色
            if (box.Text.Length == 0) {
                Set_Underline(box, "Red");
                TextStatus.Text = Properties.Resources.StringTextStatus2;
            }else{
                Set_Underline(box, "Blue");
                if (TextStatus!=null) TextStatus.Text = Properties.Resources.StringTextStatus;
            }
            if (box.Name== "TextPassDict" && !File.Exists(box.Text))//密码字典必须存在
            {
                if (TextStatus != null) TextStatus.Text = Properties.Resources.StringTextStatus4;
            }
        }
        private void Number_Check(object sender, TextCompositionEventArgs e)
        {
            TextBox box = (TextBox)sender;//只能输入数字
            Regex re = new Regex("[^0-9]+");
            e.Handled = re.IsMatch(e.Text);
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            this.TabItem1.IsSelected = true;//返回主页面
        }
    }
}
