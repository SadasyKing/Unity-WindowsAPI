//Creator:Sadasy

//Using:调用WinAPI
//注1： 创建托盘小窗后，通过托盘小窗/win右上角关闭按钮直接关闭，会产生莫名崩溃
//      该bug尚未解决，避免方式为：打开UI中的拦截最大化最小化关闭UI。
//      疑似是NotifyIcon没有释放，在关闭窗体的时候，没有优先调用notifyicon的GC，暂无办法根治
//注2： 在选择透明背景-可选择下方程序后，如果选择其他程序，需要从菜单栏选择该程序后，才可以进行交互
//      这是因为在UI选中的时候，才会取消穿透交互。最优方案是不需要点击该程序就可以继续交互，但暂无方案

using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using LitJson;
using System.Text;
using System.Drawing;
public class WindowsTools
{
    IntPtr myWindowHandle = new IntPtr(0);
    static IntPtr myWindowHandle_S = new IntPtr(0);

    #region 系统函数
    //设置当前窗口的显示状态
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    //获取当前激活窗口（获取句柄）
    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    public static extern IntPtr GetForegroundWindow();

    //获得窗体位置
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public extern static bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);
    //设置窗口边框
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);

    //设置屏幕焦点
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    //设置窗口位置，大小
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    //窗口拖动
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

    //获取窗口句柄
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    //设置键盘钩子
    [DllImport("user32.dll")]
    static extern IntPtr SetWindowsHookEx(int idHook, KeyBoardHookProc callback, IntPtr hInstance, uint threadId);
    //卸载钩子
    //[DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
    //static extern bool UnhookWindowsHookEx(int idHook);
    [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
    static extern bool UnhookWindowsHookEx(IntPtr hInstance);
    [DllImport("user32.dll")]
    static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KeyboardHookData lParam);
    [DllImport("kernel32.dll")]
    public static extern IntPtr LoadLibrary(string lpFileName);

    //读取文件
    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] FileName ofn);
    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] FileName ofd);
    [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SHBrowseForFolder([In, Out] DirName ofn);

    [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    private static extern bool SHGetPathFromIDList([In] IntPtr pidl, [In, Out] char[] fileName);

    //消息盒子
    [DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    //设置透明
    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    //可以与下方交互
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    //设置窗体位置
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);


    //参考文档：https://blog.csdn.net/sinat_25415095/article/details/121176468
    //重新设置此窗体
    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

    //发送消息
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIcon", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref _NotifyIconData lpData);
    [DllImport("user32.dll")]
    public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    // 将消息信息传递给指定的窗口过程
    [DllImport("user32.dll")]
    public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool CloseWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hwnd, uint Msg, uint wParam, uint lParam);
    /// <summary>
    /// 半透明窗口
    /// </summary>
    /// <param name="alpha"></param>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    #endregion

    #region 变量相关
    /// <summary>
    /// 钩子结构函数
    /// https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN
    /// </summary>
    public struct KeyboardHookData
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public int dwExtraInfo;
    }

    /// <summary>
    /// 系统通知
    /// 注意一定要指定字符集为Unicode，否则气泡内容不能支持中文
    /// 链接：https://learn.microsoft.com/zh-cn/windows/win32/api/shellapi/ns-shellapi-notifyicondataa
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct _NotifyIconData
    {
        internal int cbSize;
        internal IntPtr hWnd;
        internal int uID;
        internal int uFlags;
        internal int uCallbackMessage;
        internal IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string szTip;
        internal int dwState; // 这里往下几个是 5.0 的精华
        internal int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string szInfo;
        internal int uTimeoutAndVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        internal string szInfoTitle;
        internal int dwInfoFlags;
    }
    public enum SystemIcons
    {
        IDI_APPLICATION = 32512,
        IDI_HAND = 32513,
        IDI_QUESTION = 32514,
        IDI_EXCLAMATION = 32515,
        IDI_ASTERISK = 32516,
        IDI_WINLOGO = 32517,
        IDI_WARNING = IDI_EXCLAMATION,
        IDI_ERROR = IDI_HAND,
        IDI_INFORMATION = IDI_ASTERISK,
    }

    /// <summary>
    /// 定义方式：https://learn.microsoft.com/zh-cn/previous-versions/windows/desktop/legacy/ms644985(v=vs.85)
    /// 如果打不开，在网址后增加一个反括号，或者复制整个网址再打开
    /// </summary>
    /// <param name="code"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    public delegate int KeyBoardHookProc(int code, int wParam, ref KeyboardHookData lParam);

    //边框参数
    const uint SWP_SHOWWINDOW = 0x0040;
    const int GWL_STYLE = -16;//自定义边框
    const int WS_BORDER = 1;
    const int WS_POPUP = 0x800000;//关闭边框（弹出窗口）
    const int SW_SHOWMINIMIZED = 2;//(最小化窗口)

    const int WS_CAPTION = 0x00C00000;//打开边框（标题栏，无最大化、最小化、关闭）
    const int WS_MINIMIZEBOX = 0x00020000;//最小化按钮
    const int WS_MAXIMIZEBOX = 0x00010000;//最大化按钮
    const int WS_THICKFRAME = 0x00040000;//鼠标拖动边框
    const int WS_SYSMENU = 0x00080000;
    const int WS_SIZEBOX = 0x00040000;//可以修改大小边框
    const int WS_DLGFRAME = 0x00400000;//显示正常边框


    const int WS_OVERLAPPED = 0x00000000;
    //增加这个属性，可以在win10中拖动边框到屏幕边占半屏
    const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

    //键盘钩子
    //https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setwindowshookexa
    static readonly int WH_KEYBOARD_LL = 13;//键盘钩子代码

    static readonly int WM_KEYDOWN = 0x100;
    static readonly int WM_KEYUP = 0x101;
    static readonly int WM_SYSKEYDOWN = 0x104;
    static readonly int WM_SYSKEYUP = 0x105;

    static IntPtr hhook = IntPtr.Zero;
    static IntPtr hModule = IntPtr.Zero;

    public KeyBoardHookProc keyHookDelegate;

    public const int GWL_EXSTYLE = -0x14;
    public const uint WS_EX_LAYERED = 0x00080000;
    public const uint WS_EX_TRANSPARENT = 0x00000020;
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    //任务栏小窗相关
    const int SW_HIDE = 0;       // 隐藏窗口，大小不变，激活状态不变
    const int SW_MAXIMIZE = 3;   // 最大化窗口，显示状态不变，激活状态不变
    const int SW_SHOW = 5;       // 在窗口原来的位置以原来的尺寸激活和显示窗口
    const int SW_MINIMIZE = 6;   // 最小化指定的窗口并且激活在Z序中的下一个顶层窗口
    const int SW_RESTORE = 9;    // 激活并显示窗口。如果窗口最小化或最大化，则系统将窗口恢复到原来的尺寸和位置。在恢复最小化窗口时，应用程序应该指定这个标志

    //系统通知

    const int NIIF_INFO = 0x01;
    const int NIM_ADD = 0x00;
    const int NIM_MODIFY = 0x01;
    const int NIM_DELETE = 0x02;
    const int NIM_SETFOCUS = 0x03;
    const int NIM_SETVERSION = 0x04;
    const int WM_NOTIFY_TRAY = 0x0400 + 2001;

    const int NIF_MESSAGE = 0x01;
    const int NIF_ICON = 0x02;
    const int NIF_TIP = 0x04;
    const int NIF_STATE = 0x08;
    const int NIF_INFO = 0x10;
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化
    /// </summary>
    public WindowsTools()
    {
        hModule = User();
        //keyHookDelegate = new KeyBoardHookProc(HookKeyFunc);
        ReadJson();
    }


    /// <summary>
    /// 获取本身的窗口句柄
    /// </summary>
    public void SetHandle(string name)
    {
        myWindowHandle = FindWindow(null, name);
        myWindowHandle_S = myWindowHandle;
    }
    public void SetHandle()
    {
        myWindowHandle = GetForegroundWindow();
    }

    public IntPtr User()
    {
        return LoadLibrary("User32");
    }
    #endregion

    #region 窗口控制
    //最小化窗口
    public void SetMinWindows()
    {
        ShowWindow(myWindowHandle, SW_SHOWMINIMIZED);
        //具体窗口参数看这 https://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx
    }
    //获得窗体位置
    public Rectangle GetWindowRect()
    {
        Rectangle temp = new Rectangle();
        GetWindowRect(myWindowHandle, out temp);
        return temp;
    }

    //https://learn.microsoft.com/zh-cn/windows/win32/winmsg/window-styles
    //设置无边框，并设置框体大小，位置
    public void SetNoFrameWindow(Rect rect)
    {
        SetWindowLong(myWindowHandle, GWL_STYLE, WS_POPUP);
        SetWindowPos(myWindowHandle, 0, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
    }

    //最大化按钮禁用
    public void SetMaxButton(Rect rect)
    {
        SetWindowLong(myWindowHandle, GWL_STYLE, WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX);
        SetWindowPos(myWindowHandle, 0, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
    }

    //最小化按钮禁用
    public void SetOpenFrameWindow(Rect rect)
    {
        SetWindowLong(myWindowHandle, GWL_STYLE, WS_CAPTION | WS_SYSMENU | WS_MAXIMIZEBOX);
        SetWindowPos(myWindowHandle, 0, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
    }

    //恢复默认窗体
    public void SetDefauleFrame(Rect rect)
    {
        SetWindowLong(myWindowHandle, GWL_STYLE, WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        SetWindowPos(myWindowHandle, 0, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
    }

    //只剩边框
    public void SetEmptyFrame(Rect rect)
    {
        SetWindowLong(myWindowHandle, GWL_STYLE, WS_CAPTION);
        SetWindowPos(myWindowHandle, 0, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
    }

    //拖拽修改宽高
    public void MouseDrag(Rect rect)
    {
        SetWindowLong(myWindowHandle, GWL_STYLE, WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_SIZEBOX);
        SetWindowPos(myWindowHandle, 0, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
    }
    //置顶窗口
    public void TopWin(bool isOn, Rect rect)
    {
        SetWindowLong(myWindowHandle, GWL_STYLE, WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        if (isOn)
        {
            SetWindowPos(myWindowHandle, -1, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
        }
        else
        {
            SetWindowPos(myWindowHandle, -2, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, SWP_SHOWWINDOW);
        }
    }

    //拖动窗口
    public void DragWindow()
    {
        ReleaseCapture();
        SendMessage(myWindowHandle, 0xA1, 0x02, 0);
        SendMessage(myWindowHandle, 0x0202, 0, 0);
    }
    #endregion

    #region 键盘钩子
    /// <summary>
    /// 屏蔽按键
    /// </summary>
    /// <param name="isOn"></param>
    public void SetHookFunc(bool isOn)
    {
        if (isOn)
        {
            hhook = SetHook(WH_KEYBOARD_LL, new KeyBoardHookProc(HookProc), hModule, 0);
        }
        else
        {
            Unhook(hhook);
        }
    }

    /// <summary>
    /// 此处的键盘钩子，通过Json文件进行增加删除
    /// </summary>
    /// <param name="code"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    static int HookProc(int code, int wParam, ref KeyboardHookData lParam)
    {
        if (code >= 0)
        {
            if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP || wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
            {
                //Debug.Log("lParam.vkCode = " + lParam.vkCode + " / lParam.flags = " + lParam.flags);
                bool Suppress = false;

                int tempFlags;
                int tempVkCode;
                foreach (var temp in testKeyBoards)
                {
                    tempFlags = lParam.flags;
                    tempVkCode = lParam.vkCode;
                    if (temp.flags.Exists(t => t == tempFlags) && temp.vkCode.Exists(t => t == tempVkCode))
                    {
                        Suppress = true;
                    }
                }
                if (Suppress)
                {
                    return 1;
                }
            }
        }
        return CallNextHookEx(hhook, code, wParam, ref lParam);
    }
    //设置键盘钩子
    public IntPtr SetHook(int idHook, KeyBoardHookProc callback, IntPtr hInstance, uint threadId)
    {
        return SetWindowsHookEx(idHook, callback, hInstance, threadId);
    }
    //释放键盘钩子
    public void Unhook(IntPtr hhook)
    {
        UnhookWindowsHookEx(hhook);
    }
    #endregion

    #region 读取Json，使用键盘钩子
    public StringBuilder hookKeyTypeFromJson;
    [Serializable]
    public struct TestKeyBoard
    {
        public List<int> flags;
        public List<int> vkCode;
    }
    [SerializeField]
    public static List<TestKeyBoard> testKeyBoards;
    public void ReadJson()
    {
        testKeyBoards = new List<TestKeyBoard>();
        hookKeyTypeFromJson = new StringBuilder();
        string jsonText = File.ReadAllText(UnityEngine.Application.streamingAssetsPath + "/WinHook.json");

        JsonData json = JsonMapper.ToObject(jsonText);
        for (int i = 0; i < json.Count; i++)
        {
            var temp = new TestKeyBoard();
            temp.flags = new List<int>();
            temp.vkCode = new List<int>();

            for (int j = 0; j < json[i][0].Count; j++)
            {
                temp.vkCode.Add(Convert.ToInt32(json[i][0][j].ToString(), 16));
            }
            for (int j = 0; j < json[i][1].Count; j++)
            {
                temp.flags.Add(Convert.ToInt32(json[i][1][j].ToString()));
            }
            testKeyBoards.Add(temp);
        }
        foreach (var temp in json.Keys)
        {
            hookKeyTypeFromJson.Append(temp.ToString() + "、");
        }
        //Debug.Log(hookKeyTypeFromJson.ToString());
    }
    #endregion

    #region 打开文件夹路径
    //参考：https://blog.csdn.net/m0_67636792/article/details/134996606

    public string Load(params string[] ext)
    {
        //ext[0] = "*";
        FileName i = new FileName(ext);
        i.title = "打开";
        GetOpenFileName(i);
        return i.file;
    }
    public string Save(params string[] ext)
    {
        //ext[0] = "*";
        FileName i = new FileName(ext);
        i.title = "保存";
        GetSaveFileName(i);
        return i.file;
    }
    /// <summary>
    /// 列表状打开路径
    /// </summary>
    /// <returns></returns>
    public string GetDir()
    {
        DirName d = new DirName();
        IntPtr i = SHBrowseForFolder(d);
        char[] c = new char[256];
        SHGetPathFromIDList(i, c);
        return new string(c);
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class DirName
    {
        public IntPtr hwndOwner = IntPtr.Zero;
        public IntPtr pidlRoot = IntPtr.Zero;
        public String pszDisplayName = null;
        public String lpszTitle = null;
        public UInt32 ulFlags = 0;
        public IntPtr lpfn = IntPtr.Zero;
        public IntPtr lParam = IntPtr.Zero;
        public int iImage = 0;
        public DirName()
        {
            pszDisplayName = new string(new char[256]);
            ulFlags = 0x00000040 | 0x00000010; //BIF_NEWDIALOGSTYLE | BIF_EDITBOX;
            lpszTitle = "打开目录";
        }
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class FileName
    {
        public int structSize = 0;
        private IntPtr dlgOwner = IntPtr.Zero;
        private IntPtr instance = IntPtr.Zero;
        private string filter = null;
        private string customFilter = null;
        private int maxCustFilter = 0;
        private int filterIndex = 0;
        public string file { get; set; }
        private int maxFile = 0;
        public string fileTitle { get; set; }
        private int maxFileTitle = 0;
        public string initialDir { get; set; }
        public string title { get; set; }
        private int flags = 0;
        private short fileOffset = 0;
        private short fileExtension = 0;
        private string defExt = null;
        private IntPtr custData = IntPtr.Zero;
        private IntPtr hook = IntPtr.Zero;
        private string templateName = null;
        private IntPtr reservedPtr = IntPtr.Zero;
        private int reservedInt = 0;
        private int flagsEx = 0;
        public FileName(params string[] ext)
        {
            structSize = Marshal.SizeOf(this);
            defExt = ext[0];
            string n = null;
            string e = null;
            foreach (string _e in ext)
            {
                if (_e == "*")
                {
                    n += "All Files";
                    e += "*.*;";
                }
                else
                {
                    string _n = "." + _e + ";";
                    n += _n;
                    e += "*" + _n;
                }
            }
            n = n.Substring(0, n.Length - 1);
            filter = n + "\0" + e + "\0";
            file = new string(new char[256]);
            maxFile = file.Length;
            fileTitle = new string(new char[64]);
            maxFileTitle = fileTitle.Length;
            //flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;//OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR
            flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
            initialDir = UnityEngine.Application.dataPath;
        }
    }

    #endregion

    #region 透明背景
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cxTopHeight;
        public int cxBottomHeight;
    }

    public void SetTransparent()
    {
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(myWindowHandle, ref margins);
    }

    /// <summary>
    /// 设置屏幕焦点
    /// </summary>
    public void SetForeground()
    {
        SetForegroundWindow(myWindowHandle);
    }

    /// <summary>
    /// 点击下方程序
    /// </summary>
    /// <param name="rect"></param>
    public void OtherWin(bool isOn)
    {
        //SetWindowLong(myWindowHandle, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        //SetWindowPos(myWindowHandle, HWND_TOPMOST, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, 0);
        if (isOn)
        {
            EnableClickThrough();
        }
        else
        {
            DisableClickThrough();
        }
    }

    public void EnableClickThrough()
    {
        int initialStyle = GetWindowLong(myWindowHandle, GWL_EXSTYLE);
        SetWindowLong(myWindowHandle, GWL_EXSTYLE, (uint)initialStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
    }

    public void DisableClickThrough()
    {
        int initialStyle = GetWindowLong(myWindowHandle, GWL_EXSTYLE);
        SetWindowLong(myWindowHandle, GWL_EXSTYLE, (uint)initialStyle & ~WS_EX_TRANSPARENT);
    }

    #endregion

    #region 窗口整体透明度
    public const uint LWA_ALPHA = 0x2;
    public void SetAlpha(byte alpha)
    {
        SetWindowLong(myWindowHandle, GWL_EXSTYLE, WS_EX_LAYERED);
        SetLayeredWindowAttributes(myWindowHandle, 0, alpha, LWA_ALPHA);
    }
    #endregion

    #region  发送系统消息 
    //参考：https://blog.csdn.net/sinat_25415095/article/details/121176468

    _NotifyIconData nIData;
    /// <summary>
    /// 弹窗通知
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public int OpenMessageBox(string title = "我的标题", string content = "我的内容")
    {
        return MessageBox(myWindowHandle, content, title, 1);
    }

    public static int OpenMessageBox_S(string title = "我的标题", string content = "我的内容")
    {
        return MessageBox(myWindowHandle_S, content, title, 1);
    }

    /// <summary>
    /// 发送系统消息，自动添加再删除小窗
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    public void SystemMessage(string title = "我的标题", string content = "我的内容")
    {
        //if (_notifyIcon == null) CreateNotifyIcon();
        if (string.IsNullOrEmpty(content)) content = "无通知内容";
        nIData = GetData(IntPtr.Zero, "", title, content);
        Shell_NotifyIcon(NIM_ADD, ref nIData);
        Shell_NotifyIcon(NIM_DELETE, ref nIData);
    }

    /// <summary>
    /// 设置小窗信息
    /// </summary>
    /// <param name="iconHwnd"></param>
    /// <param name="sTip"></param>
    /// <param name="boxTitle"></param>
    /// <param name="boxText"></param>
    /// <returns></returns>
    private _NotifyIconData GetData(IntPtr iconHwnd, string sTip, string boxTitle, string boxText)
    {
        _NotifyIconData nData = new _NotifyIconData();
        // 结构的大小
        nData.cbSize = Marshal.SizeOf(nData);
        // 处理消息循环的窗体句柄，可以移成主窗体
        nData.hWnd = myWindowHandle;
        // 消息的 WParam，回调时用
        nData.uID = 5000;
        // 标志，表示由消息、图标、提示、信息组成
        nData.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_INFO;
        // 消息ID，回调用
        nData.uCallbackMessage = WM_NOTIFY_TRAY;
        if (iconHwnd != IntPtr.Zero)
        {
            nData.hIcon = iconHwnd;
        }
        else
        {
            // 使用默认的程序图标
            nData.hIcon = LoadIcon(IntPtr.Zero, (IntPtr)SystemIcons.IDI_APPLICATION);
        }

        // 提示的超时值（几秒后自动消失）和版本
        //nData.uTimeoutAndVersion = 10 * 1000 | NOTIFYICON_VERSION; 
        // 类型标志，有INFO、WARNING、ERROR，更改此值将影响气泡提示框的图标类型
        nData.dwInfoFlags = NIIF_INFO;

        // 图标的提示信息
        nData.szTip = sTip;
        // 气泡提示框的标题
        nData.szInfoTitle = boxTitle;
        // 气泡提示框的提示内容
        nData.szInfo = boxText;

        return nData;
    }

    #endregion

    #region 创建任务栏小窗，最小化到托盘
    //参考：https://download.csdn.net/download/luoyikun/87362220

    NotifyIcon _notifyIcon;
    public void HideTray()
    {
        ShowWindow(myWindowHandle, SW_HIDE);
        if (_notifyIcon == null)
        {
            CreateNotifyIcon();
        }
        else
        {
            _notifyIcon.Visible = true;
        }
    }

    /// <summary>
    /// 创建任务栏小窗
    /// </summary>
    public void CreateNotifyIcon()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
            return;
        }
        _notifyIcon = new NotifyIcon();
        _notifyIconS = _notifyIcon;
        //_notifyIcon.BalloonTipText = "Sadasy";//托盘气泡显示内容
        _notifyIcon.Text = "Sadasy的图标提示";//鼠标悬浮时显示的内容
        _notifyIcon.Visible = true;
        _notifyIcon.Icon = CustomTrayIcon(UnityEngine.Application.streamingAssetsPath + "/icon.png", 100, 100);
        MenuItem closeMenu = new MenuItem("关闭");
        MenuItem[] menuItems = new MenuItem[] { closeMenu };
        _notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(menuItems);
        closeMenu.Click += OnMenuClose;

        _notifyIcon.MouseClick += notifyIcon_MouseClick;
    }
    /// <summary>
    /// 关闭任务栏小窗（隐藏）
    /// </summary>
    public void DisposeNotifyIcon()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }
    }

    /// <summary>
    /// 后台关闭
    /// 注释代码代表莫名bug崩溃
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnMenuClose(object sender, EventArgs e)
    {
        //_notifyIcon.Visible = false;
        //_notifyIcon.Dispose();
        //_notifyIcon = null;
        //UnityEngine.Application.Quit();
        //QuitApplication();
        //SetWindow.Instance.Exit();
        _notifyIcon.Dispose();
        _notifyIcon = null;
        CloseWindow(myWindowHandle);
        PostMessage(myWindowHandle, WM_SYSCOMMAND, SC_CLOSE, 0);
    }

    /// <summary>
    /// 绘制图标
    /// </summary>
    /// <param name="iconPath"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    private static Icon CustomTrayIcon(string iconPath, int width, int height)
    {
        Bitmap bt = new Bitmap(iconPath);
        Bitmap fitSizeBt = new Bitmap(bt, width, height);
        return Icon.FromHandle(fitSizeBt.GetHicon());
    }
    private static void notifyIcon_MouseClick(object sender, MouseEventArgs e)//点击托盘图标
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowWindow(myWindowHandle_S, SW_RESTORE);
        }
    }
    #endregion

    #region 拦截窗口顶部操作  参考同上
    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    HandleRef mainRef;
    WndProcDelegate wndProc;
    IntPtr wndProcPtr;
    static IntPtr m_OldWndProcPtr;

    const int WM_SYSCOMMAND = 0x0112;
    const int WM_COMMAND = 0x0111;

    const int SC_CLOSE = 0xF060;
    const int SC_MAXIMIZE = 0xF030;
    const int SC_MINIMIZE = 0xF020;

    const int MaximizeID = 1001;                 // 任务栏菜单—最大化
    const int MinimizeID = 1002;                 // 任务栏菜单—最小化
    const int QuitID = 1003;                 // 任务栏菜单—退出

    /// <summary>
    /// UnityUI调用
    /// </summary>
    /// <param name="isOn"></param>
    public void OpenWndProc(bool isOn)
    {
        if (isOn)
        {
            WndProcInit();
        }
        else
        {
            WndProcDispose();
        }
    }

    public void WndProcInit()
    {
        mainRef = new HandleRef(null, myWindowHandle);
        wndProc = new WndProcDelegate(WndProc);
        wndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProc);
        m_OldWndProcPtr = SetWindowLongPtr(mainRef, -4, wndProcPtr);
    }
    public static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
        {
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }
    }
    [MonoPInvokeCallback]
    IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_SYSCOMMAND)
        {
            // 屏蔽窗口顶部关闭最小化事件
            switch ((int)wParam)
            {
                /*case MinimizeID:
                    // 最小化

                    Debug.Log("最小化");
                    OpenMessageBox_S("最小化窗口", "确认要最小化吗");
                    break;
                case MaximizeID:
                    OpenMessageBox_S("最大化窗口", "确认要最大化吗");
                    // 最大化
                    break;
                case QuitID:
                    OpenMessageBox_S("关闭窗口", "确认要关闭吗");
                    // 关闭
                    break;
                default:
                    Debug.Log($"未处理的菜单点击事件 ID值:{(int)wParam}");
                    break;*/

                case SC_CLOSE:
                    if (OpenMessageBox_S("关闭窗口", "确认要关闭吗") == 1)
                    {
                        QuitApplication_S();
                    }
                    // 关闭
                    return IntPtr.Zero;
                case SC_MAXIMIZE:
                    OpenMessageBox_S("最大化窗口", "确认要最大化吗");
                    // 最大化
                    break;
                case SC_MINIMIZE:
                    //OpenMessageBox_S("最小化窗口", "确认要最小化吗");
                    HideTray();
                    // 最小化
                    return IntPtr.Zero;
            }
        }
        return CallWindowProc(m_OldWndProcPtr, hWnd, msg, wParam, lParam);
    }

    void WndProcDispose()
    {
        SetWindowLongPtr(mainRef, -4, m_OldWndProcPtr);
        mainRef = new HandleRef(null, IntPtr.Zero);
        m_OldWndProcPtr = IntPtr.Zero;
        wndProcPtr = IntPtr.Zero;
        wndProc = null;
    }

    #endregion

    #region 退出程序
    public void QuitApplication()
    {
        if (_notifyIcon != null)
            _notifyIcon.Dispose();

        UnityEngine.Application.Quit();
    }
    static NotifyIcon _notifyIconS;
    public static void QuitApplication_S()
    {
        //if (_notifyIconS != null)
        //    _notifyIconS.Dispose();

        UnityEngine.Application.Quit();
    }
    public void GCThis()
    {
        if (_notifyIcon != null)
            _notifyIcon.Dispose();
        WndProcDispose();
        GC.SuppressFinalize(this);

    }
    ~WindowsTools()
    {
        GCThis();
    }
    #endregion
}

public class MonoPInvokeCallbackAttribute : Attribute
{
    public MonoPInvokeCallbackAttribute() { }
}