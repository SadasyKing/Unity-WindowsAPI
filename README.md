# Unity-Windows-API
Unity中调用WindowsAPI

参考：
https://download.csdn.net/download/luoyikun/87362220

https://blog.csdn.net/sinat_25415095/article/details/121176468
      
使用时，可直接参照场景：Scene-WindowsCtrl场景下的UI绑定的函数进行学习调用。
所有脚本绑定在该场景下的Windows物体上，TopWindows是另外的置顶窗口脚本，需要另外调用。

注意事项：
1.注意键盘钩子的释放。如果需要多个键盘钩子默认启用，将streamingAssets下的winhook.json中增加键盘秘钥即可。

2.注意不要随意修改"csc.rsp"文件。如果有关于该文件的报错，请将.net环境(Project Settings-Player-Other Settings-Api Compatibility Level)设置为.Net Framework即可。

3.注意：直接调用WindowsAPI的函数必须为静态。

4.键盘秘钥地址：https://learn.microsoft.com/zh-cn/windows/win32/inputdev/virtual-key-codes

注1： 创建托盘小窗后，通过托盘小窗/win右上角关闭按钮直接关闭，会产生莫名崩溃
      该bug尚未解决，避免方式为：打开UI中的拦截最大化最小化关闭UI。
      疑似是NotifyIcon没有释放，在关闭窗体的时候，没有优先调用notifyicon的GC，暂无办法根治
      
注2： 在选择透明背景-可选择下方程序后，如果选择其他程序，需要从菜单栏选择该程序后，才可以进行交互
      这是因为在UI选中的时候，才会取消穿透交互。最优方案是不需要点击该程序就可以继续交互，但暂无方案


![Image](https://github.com/SadasyKing/Unity-Windows-API/blob/main/winapi.png)
