﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Collections;
using System.Threading;

namespace convert_article_to_voc_list
{
    public struct smart_search_info
    {
        public int search_start;
        public int search_end;
        public ArrayList search_list;
    }

    public struct voc_object : IComparable<voc_object>
    {
        public voc_object(string _voc, string _date, string _context, int _isMark)
        {
            voc = _voc;
            date = _date;
            context = _context;
            isMark = _isMark;
        }
        public string voc;
        public string date;
        public string context;
        public int isMark;

        public int CompareTo(voc_object voc)
        {
            return - String.Compare(voc.voc, this.voc, true);
        }
    }

    public partial class Form1 : Form
    {
        private const string input_txt_folder = "article_to_list_input";
        private const string out_clt_folder = "article_to_list_output";
        private const int default_max_element = 5000;
        private const string sft_ver = "Version 1.1";
        private int max_element = default_max_element;
        private string[] original_text = new string[] { };
        private int max_smart_merge_thread_count = Environment.ProcessorCount * 2;

        private void set_max_element(int max)
        {
            max_element = max;
            textBox3.Text = max_element.ToString();
        }

        public Form1()
        {
            InitializeComponent();
            LinkLabel.Link link = new LinkLabel.Link();
            link.LinkData = "http://pchsu-blog.blogspot.tw/2017/05/blog-post.html";
            linkLabel1.Links.Add(link);

            LinkLabel.Link link2 = new LinkLabel.Link();
            link2.LinkData = "http://pc-hsu.blogspot.tw/2013/03/blog-post.html";
            linkLabel2.Links.Add(link2);
            folder_init();
            textBox3.Text = default_max_element.ToString();

            this.checkBox1.Checked = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folder_init();
            start_do_convert_all();
            MessageBox.Show("OK");
        }

        void send_msg(string msg, bool clean)
        {
            if(!clean)
                textBox1.Text += msg;
            else
                textBox1.Text = msg;

            System.Windows.Forms.Application.DoEvents();
            this.Refresh();
        }

        private void folder_init()
        {
            clean_msg();

            if (false == Directory.Exists(input_txt_folder))
            {
                System.IO.Directory.CreateDirectory(input_txt_folder);
                send_msg(input_txt_folder + " folder , Created !\r\n", false);
            }

            if (false == Directory.Exists(out_clt_folder))
            {
                System.IO.Directory.CreateDirectory(out_clt_folder);
                send_msg(out_clt_folder + " folder , Created !\r\n", false);
            }

            send_msg(sft_ver + " : 初始化完畢 !\r\n", false);
        }

        private int process_count = 0;

        private void clean_msg()
        {
            textBox1.Text = "";
        }

        private void process_init()
        {
            clean_msg();
            process_count = 0;
        }

        private void start_do_convert_all()
        {
            process_init();
            string[] input_txt_array = Directory.GetFiles(input_txt_folder);
            send_msg("要處理檔案數量 : " + input_txt_array.Length + "\r\n", false);


            string process_file_info = "";
            foreach (string input_file in input_txt_array)
            {
                bool is_error = false;
                try
                {
                    List<string> pure_list;
                    pure_list = handle_complex_artical_to_pure_list(input_file);
                    multithread_article_analysis(pure_list);
                    prepared_list.Sort();
                    split_write_to_mvq(out_clt_folder + "/" + Path.GetFileNameWithoutExtension(input_file), prepared_list);
                }
                catch(Exception e)
                {
                    is_error = true;
                }

                process_count++;
                if (is_error)
                {
                    process_file_info += process_count + ". " + input_file + " : FAIL\r\n";
                    
                }
                else
                {
                    process_file_info += process_count + ". " + input_file + " : SUCCESS\r\n";
                }

                send_msg(process_file_info, false);
            }
        }

        private void split_write_to_mvq(string filename, List<voc_object> voc_write_list)
        {
            List<voc_object> tmp_list = new List<voc_object>();
            int total_count = voc_write_list.Count;
            int curret_count = 0;
            int file_idx = 0;
            foreach (voc_object voc in voc_write_list)
            {
                tmp_list.Add(voc);
                total_count--;
                curret_count++;
                //if (curret_count == 2500 || total_count == 0)
                if (curret_count == max_element || total_count == 0)
                {
                    file_idx++;
                    tmp_list.Reverse();
                    write_to_mvq(filename + "_" + file_idx.ToString(), tmp_list);
                    tmp_list = new List<voc_object>();
                    curret_count = 0;
                }
            }
        }

        private void write_to_mvq(string filename, List<voc_object> voc_write_list)
        {
            mvq_write_clt_file(voc_write_list, filename + ".clt");
        }

        private List<string> leave_the_valid_char(string[] complex_line)
        {
            List<string> line_with_valid_char = new List<string>();
            foreach (string messyText in complex_line)
            {
                string pure = Regex.Replace(messyText, @"[^a-zA-Z\s\x21\x22\x23\x24\x25\x26\x27\x28\x29
                                                            \x2A\x2B\x2C\x2D\x2E\x2F\x3A\x3B\x3C\x3D\x3E\x3F\x40
                                                            \x5B\x5C\x5D\x5E\x5F\x60\x7B\x7C\x7D\x7E]", "").Trim();
                if(pure.Length > 0)
                    line_with_valid_char.Add(pure);
            }

            return line_with_valid_char;
        }

        private List<string> split_to_single_word(List<string> multi_words)
        {
            List<string> line_with_single_word = new List<string>();
            foreach (string messyText in multi_words)
            {
                string[] delimiter = new string[] { " ", ",", "\t", "!", "\"", "#", "$", "%", "&", "'", "(", ")"
                , "*", "+", ".", "/", ":", ";", "<", "=", ">", "?", "@", "[", @"\", "]", "^", "_"
                , "`", "{", "|", "}", "~"};
                string[] tokens = messyText.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                foreach(string s in tokens)
                    line_with_single_word.Add(s);
            }

            return line_with_single_word;
        }

        private List<string> remove_the_duplicate_word(List<string> duplicate_words)
        {
            List<string> noDupes = duplicate_words.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            return noDupes;
        }

        private List<string> leave_sting_length_large_than_one(List<string> with_a_char)
        {
            List<string> sting_length_large_than_one = new List<string>();
            foreach (string str in with_a_char)
            {
                string result = str.Trim().Trim(new Char[] { '-' });
                if (result.Length > 1)
                    sting_length_large_than_one.Add(result);
            }

            return sting_length_large_than_one;
        }

        private List<string> handle_complex_artical_to_pure_list(string file_name)
        {
            string source_file = file_name;
            Encoding currentEncoding;
            using (var reader = new System.IO.StreamReader(source_file, true))
            {
                currentEncoding = reader.CurrentEncoding;
            }
            original_text = System.IO.File.ReadAllLines(source_file, currentEncoding);
            List<string> pure_list_without_valid_char = leave_the_valid_char(original_text);
            List<string> pure_list_split_to_single_word = split_to_single_word(pure_list_without_valid_char);
            List<string> pure_list_with_long_sting_length = leave_sting_length_large_than_one(pure_list_split_to_single_word);
            List<string> pure_list_without_duplicate = remove_the_duplicate_word(pure_list_with_long_sting_length);
            List<string> pure_list = pure_list_without_duplicate;
            return pure_list;
        }

        public static readonly object control_merge_system = new object();
        private int smart_merge_thread_count = 0;
        private int merge_process_count = 0;
        private bool execute_all_thread = true;
        public bool mgr_execute_all_thread
        {
            set
            {
                lock (control_merge_system)
                {
                    execute_all_thread = value;
                }
            }

            get
            {
                lock (control_merge_system)
                {
                    return execute_all_thread;
                }
            }
        }
        public int mgr_merge_process_count
        {
            set
            {
                lock (control_merge_system)
                {
                    merge_process_count = value;
                }
            }

            get
            {
                lock (control_merge_system)
                {
                    return merge_process_count;
                }
            }
        }
        public int mgr_smart_merge_thread_count
        {
            set
            {
                lock (control_merge_system)
                {
                    smart_merge_thread_count = value;
                }
            }

            get
            {
                lock (control_merge_system)
                {
                    return smart_merge_thread_count;
                }
            }
        }

        public void smart_search_worker(object parameterObject)
        {
            smart_search_info search_info = (smart_search_info)parameterObject;
            int start = search_info.search_start;
            int end = search_info.search_end;
            List<voc_object> local_result = new List<voc_object>();
            ArrayList search_list = search_info.search_list;
            try
            {
                for (int i = start; i < end && mgr_execute_all_thread; i++)
                {
                    mgr_merge_process_count++;
                    string line = (string)search_list[i];
                    string context = line;

                    if (line.Length != 0)
                    {
                        if (this.checkBox1.Checked)
                        {
                            int line_count = 0;
                            foreach (string o_line in original_text)
                            {
                                if (o_line.Trim().Length > 0)
                                {
                                    string[] a_line = new string[] { o_line };
                                    List<string> pure_list_without_valid_char = leave_the_valid_char(a_line);
                                    List<string> pure_list_split_to_single_word = split_to_single_word(pure_list_without_valid_char);
                                    bool is_get = false;
                                    foreach (string word in pure_list_split_to_single_word)
                                    {
                                        if (word.Trim().Trim(new Char[] { '-' }).Equals(line, StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            is_get = true;
                                            break;
                                        }
                                    }
                                    if (is_get)
                                    {
                                        line_count++;
                                        if (line_count == 1)
                                        {
                                            context += " = ";
                                        }
                                        string start_str = "######## " + line_count.ToString() + " ########\r\n";
                                        context += start_str + o_line.Trim().Replace("=", "＝") + "\r\n";
                                    }
                                }
                            }
                        }
                        local_result.Add(new voc_object(line, DateTime.Now.ToString("yyyy'/'MM'/'dd"), context, 0));
                    }
                }

                lock (control_merge_system)
                {
                    prepared_list.AddRange(local_result);
                }
            }
            catch (Exception e) { }
            finally
            {
                lock (control_merge_system)
                {
                    mgr_smart_merge_thread_count--;
                }
            }

        }

        private void UI_disable()
        {
            this.button1.Enabled = false;
            this.button2.Enabled = false;
            this.checkBox1.Enabled = false;
            this.textBox3.Enabled = false;
        }

        private void UI_enable()
        {
            this.button1.Enabled = true;
            this.button2.Enabled = true;
            this.checkBox1.Enabled = true;
            this.textBox3.Enabled = true;
        }

        private List<voc_object> prepared_list = new List<voc_object>();
        private void multithread_article_analysis(List<string> pure_list)
        {
            ArrayList target_search = new ArrayList(pure_list);
            prepared_list = new List<voc_object>();
            mgr_merge_process_count = 0;
            mgr_smart_merge_thread_count = max_smart_merge_thread_count;
            int start_idx = 0, end_idx = 0;
            int total_count = target_search.Count;
            int scale = total_count / max_smart_merge_thread_count;
            int remain = total_count % max_smart_merge_thread_count;
            for (int i = 0; i < max_smart_merge_thread_count; i++)
            {
                start_idx = i * scale;
                end_idx = start_idx + scale;
                if (i == max_smart_merge_thread_count - 1)
                {
                    end_idx += remain;
                }
                smart_search_info tmp_smart_search_info = new smart_search_info();
                tmp_smart_search_info.search_start = start_idx;
                tmp_smart_search_info.search_end = end_idx;
                tmp_smart_search_info.search_list = target_search;

                ThreadPool.QueueUserWorkItem(new WaitCallback(smart_search_worker), tmp_smart_search_info);
            }

            UI_disable();
            while (true)
            {
                if (mgr_smart_merge_thread_count == 0 ||
                    mgr_execute_all_thread == false)
                    break;

                send_msg(mgr_merge_process_count.ToString() + " / " + pure_list.Count.ToString() + "\r\n", true);
                Thread.Sleep(100);
            }
            send_msg(mgr_merge_process_count.ToString() + " / " + pure_list.Count.ToString() + "\r\n", true);
            UI_enable();
        }

        char[] array1 = { 'p', 'c', 'h', 's', 'u', 'm', 'v', 'q' };
        char[] array2 = { '1', '0', '0', '6', '0', '2', '0', '7' };
        private static readonly byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };

        private byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Rijndael object 
            // with the specified key and IV. 
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }

        private string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an Rijndael object 
            // with the specified key and IV. 
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

        private RijndaelManaged GetRijndaelManaged(String secretKey)
        {
            var keyBytes = new byte[16];
            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
            return new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128,
                Key = keyBytes,
                IV = keyBytes
            };
        }

        private void EncryptFile(string source, string outputFile)
        {
            FileStream fsCrypt = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite);
            try
            {
                string password = new string(array1) + new string(array2);

                RijndaelManaged RMCrypto = GetRijndaelManaged(password);

                byte[] encrypted = EncryptStringToBytes(source, RMCrypto.Key, RMCrypto.IV);

                foreach (byte data in encrypted)
                    fsCrypt.WriteByte((byte)data);

                fsCrypt.Close();
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
                try
                {
                    fsCrypt.Close();
                }
                catch (Exception ee) { }
                try
                {
                    string[] lines = { "[]" };
                    System.IO.File.WriteAllLines(outputFile, lines);
                }
                catch (Exception ee) { }
            }
        }
        private string DecryptFile(FileStream fsCrypt)
        {
            string result = "";
            try
            {
                string password = new string(array1) + new string(array2);

                RijndaelManaged RMCrypto = GetRijndaelManaged(password);

                byte[] buffer = cvt_stream_to_byte_array(fsCrypt);

                result = DecryptStringFromBytes(buffer, RMCrypto.Key, RMCrypto.IV);

                fsCrypt.Close();
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
                result = "";
            }

            return result;
        }

        public List<voc_object> mvq_read_clt_file(string file)
        {
            List<voc_object> tmp = new List<voc_object>();
            string result;
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(fs, Encoding.UTF8);
            result = reader.ReadToEnd();

            if ((result.StartsWith("[") && result.EndsWith("]")) == false)
            {
                if (result.Length != 0)
                {
                    fs.Position = 0;
                    result = DecryptFile(fs);
                }
            }

            if (result.Length == 0)
                result = "[]";
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;
            tmp = serializer.Deserialize<List<voc_object>>(result);
            reader.Close();
            return tmp;
        }


        public void mvq_write_clt_file(List<voc_object> tmp, string filename)
        {
            var jsonSerialiser = new JavaScriptSerializer();
            jsonSerialiser.MaxJsonLength = Int32.MaxValue;
            string json = jsonSerialiser.Serialize(tmp);
            EncryptFile(json, filename);
        }

        public static byte[] cvt_stream_to_byte_array(Stream input)
        {
            byte[] buffer = new byte[1024 * 1024 * 5];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folder_init();
            MessageBox.Show("初始化完畢 !");
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            bool has_error = false;
            int get_int = default_max_element;
            try
            {
                get_int = Int32.Parse(textBox3.Text);

                if (get_int > 25000)
                {
                    textBox3.Text = "25000";
                    get_int = 25000;
                }
            }
            catch(Exception ee)
            {
                has_error = true;
            }

            if(get_int < 1)
                has_error = true;

            if (has_error)
                get_int = default_max_element;

            set_max_element(get_int);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData as string);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData as string);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mgr_execute_all_thread = false;
        }
    }
}
