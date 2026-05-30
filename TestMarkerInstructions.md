# AR Marker 识别问题排查指南

## 快速测试方案

### 方案 A：使用高质量测试图片（最推荐）

1. **下载 Unity 官方测试图**：
   - 访问：https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.2/manual/image-tracking.html
   - 下载官方提供的测试图像
   - 或使用任何高质量、高对比度的图片

2. **特征丰富的图像**：
   - 建议使用有很多细节的图片（如杂志封面、产品包装）
   - 避免纯色、渐变、重复图案
   - 黑白图案效果最好

### 方案 B：临时测试用简单图案

打印以下任意一种：

**选项 1：棋盘格**
```
打印一个 8x8 的棋盘格
每个格子 2.5cm x 2.5cm
总尺寸：20cm x 20cm
```

**选项 2：二维码**
```
生成任意二维码（如你的网址）
打印尺寸：15cm x 15cm
高对比度打印
```

**选项 3：高对比度图案**
```
使用任何黑白对比强烈的图案
如：公司 Logo、游戏图标等
```

---

## 配置检查清单

### Unity 配置

#### 1. Reference Image Library 设置
```
Project > MarkerLibrary > Inspector

必须设置：
✓ Add Image（添加图像）
✓ Name: "DesktopMarker"（注意大小写）
✓ Specify Size: ✅ 勾选
✓ Physical Size: 0.2（20cm 或根据实际打印尺寸）
✓ Keep Texture at Runtime: ✅ 必须勾选
```

#### 2. 图像导入设置
```
选中你的 Marker 图像 > Inspector

纹理设置：
- Texture Type: Default 或 Sprite (2D and UI)
- Max Size: 2048 或更高
- Format: 自动或 RGBA32
- 点击 Apply
```

#### 3. AR Tracked Image Manager
```
XR Origin > Inspector > AR Tracked Image Manager

设置：
- Serialized Library: MarkerLibrary（必须选择）
- Max Number Of Moving Images: 1
- Tracked Image Prefab: 留空
```

---

## 测试距离建议

### 最佳识别距离
```
打印尺寸 20cm：
- 最佳距离：40cm - 80cm
- 最小距离：30cm
- 最大距离：150cm

打印尺寸 15cm：
- 最佳距离：30cm - 60cm
- 最小距离：25cm
- 最大距离：100cm
```

### 测试方法
1. 从 50cm 开始
2. 慢慢靠近或远离
3. 观察 Console 日志
4. 看到 "✓ 检测到 Marker" 即成功

---

## 环境要求

### 光线
- ✅ 充足的自然光或室内灯光
- ❌ 避免阳光直射（会过曝）
- ❌ 避免太暗的环境
- ❌ 避免 Marker 上有阴影

### Marker 放置
- ✅ 平放在桌面
- ✅ 完整可见（不被遮挡）
- ❌ 避免反光（使用普通纸，不要光面纸）
- ❌ 避免褶皱或弯曲

---

## 调试日志

### 预期的 Console 输出

**成功识别时**：
```
✓ 检测到 Marker: DesktopMarker
✓✓✓ Marker 校准完成！
  - 基准距离: 0.52m
  - 基准位置: (0.0, 0.3, -0.4)
  - 基准旋转: (0.0, 180.0, 0.0)
```

**未识别时**：
```
（没有任何 Marker 相关日志）
或者
"Marker 追踪质量下降"
"Marker 丢失"
```

---

## 常见问题

### Q1: 左上角显示 "Tracking: ✗"
**原因**：完全没有检测到 Marker
**解决**：
1. 检查 Reference Image Library 是否正确配置
2. 尝试不同的图像
3. 检查相机权限

### Q2: 看到 "Tracking: ✓" 但没有显示模型
**原因**：MarkerVisualizer 未正确配置
**解决**：
1. 确认 ARMarkerTracker 上有 MarkerVisualizer 组件
2. 检查 Console 有无错误

### Q3: 识别很慢或不稳定
**原因**：图像特征不够丰富
**解决**：
1. 更换更复杂的图案
2. 改善光线条件
3. 保持稳定距离

---

## 快速测试流程

1. **使用杂志封面或产品包装**（最快速）
   - 找一本杂志封面或任何印刷品
   - 拍照或扫描
   - 导入 Unity 作为测试图像
   - 放置实物，测试识别

2. **打印测试图案**
   - 使用黑白棋盘格或二维码
   - 高质量打印
   - 尺寸：15-20cm

3. **调整设置**
   - Physical Size 设置为实际打印尺寸
   - 距离保持在 50cm 左右
   - 光线充足

---

## 终极解决方案

如果以上都不行，使用这个 100% 成功的方法：

### 使用 Unity 示例场景的图像
1. 从 Unity Package Manager 安装 AR Foundation Samples
2. 使用其中提供的测试图像
3. 这些图像经过优化，识别率最高


