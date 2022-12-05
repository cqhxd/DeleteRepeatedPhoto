using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Threading;
// NuGet包地址---https://www.nuget.org/packages/WindowsAPICodePack-Shell/
// WindowsAPICodePack-Shell  NuGet包，可以获取照片的所有信息。
using Microsoft.WindowsAPICodePack.Shell;

// ************************************************************
// 删除重复的照片
// 功能描述：清理一个文件夹下重复的照片，把重复的放在该文件夹的 fordel 目录下， 
// 对比描述：根据照片文件的字节数、拍摄日期对比是否重复
// 代     码：小黄 2022.11.29   wechat:18983625525 
// ************************************************************
namespace 删除重复图片
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = folderBrowserDialog1.SelectedPath;//获取选中文件路径
            }
        }


        private void btn_start_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() == "") { return; }
            if (!Directory.Exists(textBox1.Text)) { return; }

            this.btn_start.Enabled = false;
            this.label1.Visible = true; this.label2.Visible = true; this.label2.Text = "";
            this.label3.Visible = true; this.label4.Visible = true; this.label4.Text = "";

            Thread t = new Thread(do_it);
            t.Start();
        }

        //开始
        private void do_it()
        {
            if (!Directory.Exists(textBox1.Text + "\\fordel"))
            {
                Directory.CreateDirectory(textBox1.Text + "\\fordel");
            }
            var For_del_path = textBox1.Text + "\\fordel"; // 待删除的目录

            DirectoryInfo dir = new DirectoryInfo(textBox1.Text);

            FileInfo[] files = dir.GetFiles();  // 1、找出本目录下所有文件 

            Dictionary<string, FileInfo> Dict = new Dictionary<string, FileInfo>();

            SetProcess St = new SetProcess(SetProcessSub);

            for (var i = 0; i < files.Length; i++)
            {
                // key 用 ‘文件长度’ + ‘拍摄时间’ 作为Key
                // 拍摄时间 和 文件字节数 相同的判定为相同文件
                // 注：拍摄时间 和 文件字节数 相同 而图片内容不一样的几率几乎为 0，如果再严谨点，加上
                //      图片的GPS信息或其它信息联合判断Key。
                var key = files[i].Length.ToString() + "_" + GetMediaTimeLen(files[i].FullName);

                this.BeginInvoke(St, new object[] { i, files.Length });

                try
                {
                    Dict.Add(key, files[i]);
                }
                catch { continue; }
            }
            //=======================================================================

            List<FileInfo> tmp_objs = null;
            FileInfo[] files2 = null;



            for (var i = 0; i < Dict.Count; i++)
            {
                string key_str = Dict.Keys.ToArray()[i]; //根据Dictionary的index取Key值
                string[] tmp_arr = key_str.Split('_');

                FileInfo fi = Dict.Values.ToArray()[i];    //根据Dictionary的index取value值
                long t_long = Convert.ToInt64(tmp_arr[0].ToString()); //字节长度
                string t_ps_data = tmp_arr[1].ToString(); //拍摄时间字符串

                this.BeginInvoke(St, new object[] { i, Dict.Count });

                tmp_objs = null;
                tmp_objs = new List<FileInfo>();
                files2 = null;
                files2 = dir.GetFiles();  // 1、找出本目录下所有文件 

                for (var k = 0; k < files2.Length; k++)
                {
                    if (files2[k].Length == t_long) //先判断 length ,对了，在判断拍摄时间，不能用 if (files2[k].Length == t_long && time == t_ps_data)，太慢
                    {
                        var time = GetMediaTimeLen(files2[k].FullName);//这里来得到文件的拍摄时间
                        if (time == t_ps_data)
                        {
                            tmp_objs.Add(files2[k]);
                        }
                    }
                }
                //处理tmp_objs
                //保留 fi ,把其他的移动到 fordel 文件夹
                if (tmp_objs.Count > 1)
                {
                    for (var x = 0; x < tmp_objs.Count; x++)
                    {
                        if (tmp_objs[x].FullName != fi.FullName)
                        {
                            //剪切到 fordel 文件夹
                            if (File.Exists(tmp_objs[x].FullName))
                            {
                                try
                                {
                                    File.Move(tmp_objs[x].FullName, For_del_path + "\\" + tmp_objs[x].Name);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                            }
                        }
                    }
                    //fordel 完成
                }

            }

            Complete CC = new Complete(IsComplete);
            this.BeginInvoke(CC);
        }





        // ===================== 2022-11-29 =====================  
        // WindowsAPICodePack-Shell  NuGet包，可以获取照片的所有信息。
        // 光圈、快门、镜头、相机、拍摄时间、分辨率、像素、焦距...等等等等......
        // 在 Properties.System 属性里面找，共124项属性
        // ===================== 2022-11-29 =====================  
        private static string GetMediaTimeLen(string path)
        {
            using (ShellObject obt = ShellObject.FromParsingName(path))
            {
                var tokendata = obt.Properties.System.ItemDate.Value; //如果无'拍摄日期'，应该是取的'修改日期'
                return tokendata.ToString();
            }
        }




        public delegate void Complete();

        public void IsComplete()
        {
            this.label1.Visible = false; this.label2.Visible = false;
            this.label3.Visible = false; this.label4.Visible = false;
            this.btn_start.Enabled = true;
        }

        public delegate void SetProcess(Int64 nowInt, Int64 allInt);
        public void SetProcessSub(Int64 nowInt, Int64 allInt)
        {
            this.label2.Text = nowInt.ToString();
            this.label4.Text = allInt.ToString();
        }


    }
}
