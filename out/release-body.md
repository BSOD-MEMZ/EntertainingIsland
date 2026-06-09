# 1.1.0.5


### 新增

-  **摄像头安全指示器**：当有应用偷偷调用摄像头（点名批评掌上看家采集端，小橙看班等）时，主界面会显示一个安卓风格的绿色小圆点
- 摄像头监控支持自动化规则——可以在 ClassIsland 自动化里设置"摄像头被使用时"触发任意行动
- 发布流程半自动化，不用每次都手动上传cipx了

### 更新构建脚本

我终于不用手动发 Release 啦~

- 打包产物后缀改为 `.cipx`（ClassIsland 商店标准格式）
- 推送 tag 自动触发 GitHub Actions 构建发布
- `release.ps1` 自动把我的Typora调出来，写好release note自动追加 MD5
- 总之就是非常人性化

<!-- CLASSISLAND_PKG_MD5 {"entertainingisland.app.cipx": "1b9c6bdd549f1cdd46ab45e631863a52"} -->
