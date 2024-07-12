//Creator:Sadasy
//Using:该类用于在UI中调用，通过该类调用WindowsTools（简称为Tool）中的系统API
//部分情况下无法只使用一个函数来直接从Tool中调用，尽量朝这个放下靠近
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SetWindow : MonoBehaviour
{
    private static SetWindow instance;
    public static SetWindow Instance
    {
        get
        {
            if (instance == null)
            {
                if (FindObjectOfType<SetWindow>() != null)
                    instance = FindObjectOfType<SetWindow>();
                else
                    Debug.LogError("错误：窗口管理脚本异常");
            }
            return instance;
        }
    }
    #region 变量

    /// <summary>
    /// 透明背景使用
    /// </summary>
    public Camera currentCamera;
    public Canvas canvas;
    public InputField widthInput, heightInput, messageContent;
    //public Toggle[] toggles_WindowKeyHook;

    public Toggle hookKeyToggle;
    public Text hookKeyLabel;

    //前台确认，在UI中调用

    /// <summary>
    /// 键盘钩子是否前台屏蔽
    /// </summary>
    public bool focusSelect { get; set; }
    /// <summary>
    /// 系统方法类实体
    /// </summary>
    private static WindowsTools winTool = new WindowsTools();
    /// <summary>
    /// 设置屏幕分辨率
    /// </summary>
    public Vector2 screenRect = new Vector2(720, 1280);
    /// <summary>
    /// 文件后缀
    /// </summary>
    public InputField filePostFix;
    /// <summary>
    /// 得到的文件路径
    /// </summary>
    public InputField filePath;

    /// <summary>
    /// 读取命令行参数
    /// </summary>
    public Text argsContent;
    public static string[] args;


    Rect _rect;

    CameraClearFlags originalCameraClearFlags;
    Color originalCameraBackground;
    DoubleClickClass dragUI;
    Action doubuleClick;//双击事件
    #endregion

    #region 生命周期

    //这个函数被视为：程序入口
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void MyMain()
    {
        args = Environment.GetCommandLineArgs();

        /*
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                    if (!Screen.fullScreen)
                    {
                        SetNoFrame(false);
                    }
#endif
        */
    }

    /// <summary>
    /// 首先获取窗口句柄，设置分辨率和窗口Rect
    /// 设置屏蔽键盘钩子
    /// 设置透明窗口基础参数
    /// 设置双击事件
    /// </summary>
    private void Start()
    {
#if UNITY_EDITOR
        winTool.SetHandle();
#endif
#if UNITY_STANDALONE_WIN
        winTool.SetHandle(UnityEngine.Application.productName);
#endif
        //currentWindow = WindowsTools.GetForegroundWindow();
        InitResolutionArgs();
        SetResolution();
        focusSelect = true;
        hookKeyLabel.text = "屏蔽按键：" + winTool.hookKeyTypeFromJson.ToString();

        if (currentCamera == null) currentCamera = Camera.main;
        originalCameraClearFlags = currentCamera.clearFlags;
        originalCameraBackground = currentCamera.backgroundColor;
        dragUI = new DoubleClickClass();
        doubuleClick = new Action(SetFullScreen);

        for (int i = 0; i < args.Length; i++)
        {
            argsContent.text += $"args[{i}]: " + args[i] + "\n";
        }
    }

    private void Update()
    {
        TransparentUpdate();
    }
    #endregion

    #region 设置分辨率

    void InitResolutionArgs()
    {
        if (PlayerPrefs.GetInt("width") != 0 && PlayerPrefs.GetInt("height") != 0)
        {
            widthInput.text = PlayerPrefs.GetInt("width").ToString();
            heightInput.text = PlayerPrefs.GetInt("height").ToString();
        }
    }
    public void SetResolution()
    {
        int width = int.Parse(widthInput.text);
        int height = int.Parse(heightInput.text);

        widthInput.text = width.ToString();
        heightInput.text = height.ToString();

        PlayerPrefs.SetInt("width", width);
        PlayerPrefs.SetInt("height", height);

        screenRect = new Vector2(width, height);
        Screen.SetResolution(width, height, false);
        canvas.GetComponent<CanvasScaler>().referenceResolution = screenRect;
        DisposeResolution();
    }
    #endregion

    #region 窗口置顶
    public void TopWindow(bool isOn)
    {
        winTool.TopWin(isOn, _rect);
    }
    #endregion

    #region 设置最小化窗口

    /// <summary>
    /// 最小化窗口，ui上调用即可
    /// </summary>
    public void SetMinScreen()
    {
        winTool.SetMinWindows();
    }
    #endregion

    #region 设置全屏窗口/恢复窗口
    /// <summary>
    /// 全屏
    /// </summary>
    public void SetFullScreen()
    {

        //#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (!Screen.fullScreen)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
        }
        else
        {
            Screen.SetResolution((int)screenRect.x, (int)screenRect.y, false);
            //等待当前帧完成 再去除边框
            SetNoFrame(false);
        }
        //#endif

    }
    #endregion

    #region 设置有、无边框

    /// <summary>
    /// 在ui上调用即可
    /// </summary>
    /// <param name="isOn">true则打开，false关闭</param>
    public void SetFrame(bool isOn)
    {
        SetNoFrame(isOn);
    }

    /// <summary>
    /// 设置Rect
    /// </summary>
    void DisposeResolution()
    {
        //窗口大小  以此为例
        float windowWidth = screenRect.x;
        float windowHeight = screenRect.y;
        //计算框体显示位置

        winTool.GetWindowRect();
        float posX = (Screen.currentResolution.width - windowWidth) / 2;
        float posY = (Screen.currentResolution.height - windowHeight) / 2;
        _rect = new Rect(posX, posY, windowWidth, windowHeight);
    }

    private void SetNoFrame(bool isOn)
    {
        //DisposeResolution();
        if (isOn)
        {
            winTool.SetOpenFrameWindow(_rect);
        }
        else
        {
            winTool.SetNoFrameWindow(_rect);
        }
    }

    #endregion

    #region 顶部按钮（最大化最小化按钮）

    //弃用代码
    /// <summary>
    /// 禁用最小化按钮
    /// </summary>
    public void SetMinButton()
    {
        //DisposeResolution();
        winTool.SetOpenFrameWindow(_rect);
    }
    /// <summary>
    /// 禁用最大化按钮
    /// </summary>
    public void SetMaxButton()
    {
        //DisposeResolution();
        winTool.SetMaxButton(_rect);
    }

    public void SetDefaultFrame()
    {
        winTool.SetDefauleFrame(_rect);
    }

    public void MouseChangeSize()
    {
        DisposeResolution();
        winTool.MouseDrag(_rect);
    }


    //设置空边框（只剩标题）
    public void SetEmptyFrame()
    {
        //DisposeResolution();
        winTool.SetEmptyFrame(_rect);
    }
    #endregion

    #region 拖动窗口
    /// <summary>
    /// 在UI上调用即可
    /// </summary>
    public void DragWindows()
    {
        winTool.DragWindow();
    }


    /// <summary>
    /// 双击拖拽UI进行全屏
    /// </summary>
    public void DoubleClickDragUI()
    {
        dragUI.DoubleClick(doubuleClick, 500);
    }
    #endregion

    #region 屏蔽按键

    /// <summary>
    /// 在UI上调用即可
    /// </summary>
    /// <param name="ison">true时屏蔽，false时解除屏蔽</param>
    public void HookKey(bool isOn)
    {
        winTool.SetHookFunc(isOn);
    }

    /// <summary>
    /// 重新读取
    /// </summary>
    public void RefreshHook()
    {
        //winTool.SetHookFunc(false);
        //winTool.ReadJson();
        //winTool.SetHookFunc(true);
        StartCoroutine(IRefreshHook());
    }
    IEnumerator IRefreshHook()
    {
        winTool.SetHookFunc(false);
        yield return null;
        winTool.ReadJson();
        yield return null;
        winTool.SetHookFunc(true);
        hookKeyLabel.text = "屏蔽按键：" + winTool.hookKeyTypeFromJson.ToString();
    }

    /// <summary>
    /// 当脱离程序焦点时，取消键盘钩子
    /// 当恢复键盘焦点时，挂载键盘钩子
    /// </summary>
    /// <param name="focus"></param>
    private void OnApplicationFocus(bool focus)
    {
        if (focusSelect)
        {
            if (hookKeyToggle.isOn)
            {
                HookKey(focus);
            }
        }
    }

    #endregion

    #region 设置透明背景

    /// <summary>
    /// 启用此项，需要把Project Setting-Player-Resolution and Presentation-Use DXGI Flip Mode Swapchain for D3D11关闭
    /// </summary>
    /// <param name="transparent"></param>
    public void SetTransparent(bool transparent)
    {
        DisposeResolution();
        //winTool.SetTransparent();
        if (transparent)
        {
            currentCamera.clearFlags = CameraClearFlags.SolidColor;
            currentCamera.backgroundColor = Color.clear;
            winTool.SetTransparent();
            winTool.SetNoFrameWindow(_rect);
            //winTool.SetTransparent();
        }
        else
        {
            currentCamera.clearFlags = originalCameraClearFlags;
            currentCamera.backgroundColor = originalCameraBackground;
            winTool.SetOpenFrameWindow(_rect);
        }
    }
    #endregion

    #region 设置窗口透明度
    public void SetAplha(float alpha)
    {
#if !UNITY_EDITOR
        winTool.SetAlpha((byte)alpha);
#endif
    }
    #endregion

    #region 可选择到下方窗口

    public bool isTransparent;
    public void OtherWin(bool isOn)
    {
        isTransparent = isOn;
        DisposeResolution();
        winTool.OtherWin(isOn);
    }

    /// <summary>
    /// 当启用穿透之后，实时判定是否点击在UI上
    /// </summary>
    public void TransparentUpdate()
    {
        if (isTransparent)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                winTool.DisableClickThrough();
                winTool.SetForeground();
            }
            else
            {
                winTool.EnableClickThrough();
            }
        }
    }
    #endregion

    #region 设置窗口整体透明度
    #endregion

    #region 文件管理器

    public void OpenFile()
    {
        if (String.IsNullOrEmpty(filePostFix.text))
        {
            filePath.text = winTool.Load("*");
        }
        else
        {
            string[] fileType = filePostFix.text.Split('*');
            filePath.text = winTool.Load(fileType);
        }
    }

    public void SaveFile()
    {
        if (String.IsNullOrEmpty(filePostFix.text))
        {
            filePath.text = winTool.Load("*");
        }
        else
        {
            string[] fileType = filePostFix.text.Split('*');
            filePath.text = winTool.Save(fileType);
        }
    }

    /// <summary>
    /// 打开路径
    /// </summary>
    public void OpenDir()
    {
        filePath.text = winTool.GetDir();
    }
    public void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = filePath.text;
    }
    #endregion

    #region 颜色拾取
    #endregion

    #region 设置背景图片（分辨率控制）
    #endregion

    #region 通知消息
    public void OpenMessageBox()
    {
        //winTool.SendMessage();
        winTool.OpenMessageBox();
    }

    public void SystemMessage()
    {
        winTool.SystemMessage("Sadasy的系统通知", messageContent.text);
    }
    #endregion

    #region 托盘
    /// <summary>
    /// 隐藏窗口到托盘
    /// </summary>
    public void HideTray()
    {
        winTool.HideTray();
    }

    /// <summary>
    /// 创建托盘图标
    /// </summary>
    public void CreateNotifyIcon()
    {
        winTool.CreateNotifyIcon();
    }
    /// <summary>
    /// 关闭托盘图标
    /// </summary>
    public void DestroyNotifyIcon()
    {
        winTool.DisposeNotifyIcon();
    }
    #endregion

    #region 拦截最大化最小化关闭UI（Windows自带UI）
    public void OpenWnd(bool isOn)
    {
        winTool.OpenWndProc(isOn);
    }
    #endregion

    #region 修改鼠标系统指针
    public void SystemCursor(bool isOn)
    {
        winTool.SystemCursor(isOn);
        /*
        if (isOn)
            winTool.SystemCursor(EventSystem.current.IsPointerOverGameObject());*/
    }
    /// <summary>
    /// 限制鼠标范围
    /// </summary>
    /// <param name="isOn"></param>
    public void ClipCursor(bool isOn)
    {
        winTool.ClipCursor(isOn);
    }
    #endregion

    public void OnApplicationQuit()
    {
        winTool.SetHookFunc(false);
    }
    public void Exit()
    {
        winTool.QuitApplication();
    }
}

/// <summary>
/// 双击类
/// </summary>
public class DoubleClickClass
{
    bool firstClick = false;
    DateTime firstClickTime;
    /// <summary>
    /// 双击函数
    /// </summary>
    /// <param name="action">一个委托，添加双击需要调用的函数</param>
    /// <param name="millSeconds">双击间隔时间，默认500毫秒</param>
    public void DoubleClick(Action action, int millSeconds = 500)
    {
        if (firstClick == true)
        {
            Debug.Log((DateTime.Now - firstClickTime).TotalMilliseconds);
            if ((DateTime.Now - firstClickTime).TotalMilliseconds < millSeconds)
            {
                action.Invoke();
                firstClick = false;
            }
            else
            {
                firstClick = false;
            }
        }
        else
        {
            firstClick = true;
            firstClickTime = DateTime.Now;
        }
    }

}