using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Spy
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            foreach (HandleData data in HandleData.GetWindows())
            {
                TreeNode treeNode = new TreeNode(data.ToString());
                treeNode.Tag = data;
                AddChild(treeNode);
                treeView1.Nodes.Add(treeNode);
            }

        }

        private void AddChild(TreeNode node)
        {
            node.Nodes.Clear();
            foreach (HandleData data in ((HandleData)node.Tag).Childs)
            {
                TreeNode newNode = new TreeNode(data.ToString());
                newNode.Tag = data;
                node.Nodes.Add(newNode);
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            foreach (TreeNode node in e.Node.Nodes)
            {
                AddChild(node);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ((HandleData)(treeView1.SelectedNode.Tag)).SetFocus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ((HandleData)(treeView1.SelectedNode.Tag)).GetPosition(out int x, out int y, out int w, out int h);
            Control.MouseMove(x + w / 2, y + h / 2);
        }

        private void button3_Click(object sender, EventArgs e)
        {            
            HandleData data = ((HandleData)(treeView1.SelectedNode.Tag));
            using (Graphics g = Graphics.FromHwnd((IntPtr)data.Handle))
            {
                SolidBrush sb = new SolidBrush(Color.FromArgb(128, 255, 0, 0));
                data.GetPosition(out int x, out int y, out int w, out int h);
                g.FillRectangle(sb, 0, 0, w,h);
                g.DrawRectangle(Pens.Black, 0, 0,w,h);
            }
        }

        private string Shorten(string str,int len=50)
        {
            if (str.Length > len)
            {
                str = str.Substring(0, len-3)+"...";
            }
            return str;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string root = "";
            TreeNode node = treeView1.SelectedNode;
            for (; node.Parent != null;)
            {
                root = "[" + node.Parent.Nodes.IndexOf(node) + "]" + Shorten(node.Tag.ToString()) +"\r\n"+root;
                node = node.Parent;
            }
            root = "Root : "+Shorten(node.ToString()) + "\r\n" + root;
            textBox1.Text = root;
        }
    }

    public class HandleData
    {
        [DllImport("user32")]
        private static extern Int32 SendMessage(int hWnd, int uMsg, int WParam, StringBuilder LParam);

        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern int GetWindow(int hWnd1, int uCmd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(int handle, StringBuilder title, int size);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern long GetClassName(int hwnd, StringBuilder lpClassName, long nMaxCount);

        [DllImport("user32.dll")]
        private static extern void SetForegroundWindow(int hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(int hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(int hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        const int WM_GETTEXTLENGTH = 0x000E;
        const int WM_GETTEXT = 0x000D;
        const int SW_SHOWNORMAL = 1;

        int handle;

        public int Handle
        {
            get { return handle; }
        }

        public string Caption
        {
            get
            {
                StringBuilder builder = new StringBuilder(1024);
                GetWindowText(handle, builder, 1024);
                return builder.ToString();
            }
        }

        public string Class
        {
            get
            {
                StringBuilder builder = new StringBuilder(1024);
                GetClassName(handle, builder, 1024);
                return builder.ToString();
            }
        }

        public string Text
        {
            get
            {
                int length = SendMessage(handle, WM_GETTEXTLENGTH, 0, null);
                StringBuilder sb = new StringBuilder(length + 1);
                SendMessage(handle, WM_GETTEXT, sb.Capacity, sb);
                return sb.ToString();
            }
        }

        public List<HandleData> Childs
        {
            get
            {
                List<HandleData> childs = new List<HandleData>();
                int firstChildHandle = GetWindow(handle, 5); //Get First Child
                if (firstChildHandle == 0) return childs;

                for (int beforeHandle = firstChildHandle; beforeHandle != 0;)
                {
                    childs.Add(new HandleData(beforeHandle));
                    beforeHandle = GetWindow(beforeHandle, 2);
                }

                return childs;
            }
        }

        public override string ToString()
        {
            return Caption + "(" + handle + ")" + " / " + Text;
        }

        public HandleData(int handle)
        {
            this.handle = handle;
        }

        public static List<HandleData> GetWindows()
        {
            List<HandleData> list = new List<HandleData>();
            foreach (Process p in Process.GetProcesses())
                if (p.MainWindowHandle != IntPtr.Zero)
                    if (p.MainWindowTitle != "")
                        list.Add(FromCaption(p.MainWindowTitle));
            return list;
        }

        public static HandleData FromCaption(string caption)
        {
            return new HandleData(FindWindow(null, caption));
        }

        public void SetFocus()
        {
            SendMessage(handle, 0x0007, 0, null);
        }

        public void GetPosition(out int x, out int y, out int w, out int h)
        {
            RECT rct = new RECT();
            GetWindowRect(handle, ref rct);
            x = rct.Left;
            y = rct.Top;
            w = rct.Right - x;
            h = rct.Bottom - y;
        }

        public void LoadWindow()
        {
            ShowWindow(handle, SW_SHOWNORMAL);
            SetForegroundWindow(handle);
        }
    }

    public class Control
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]

        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public static void MouseMove(int x, int y)
        {
            var screenBounds = Screen.PrimaryScreen.Bounds;
            x *= 65535 / screenBounds.Width;
            y *= 65535 / screenBounds.Height;
            mouse_event(0x8000 | 0x0001, x, y, 0, 0); //Absolute and Move
        }
    }
}
