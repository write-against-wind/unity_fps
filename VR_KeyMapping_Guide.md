# VR手柄按键映射设置指南

## 概述
现在您的VR FPS游戏支持以下手柄按键映射：

### 🎮 按键布局
- **左手柄 X键** → 检视武器
- **左手柄 Y键** → 切换武器  
- **右手柄 A键** → 换弹
- **右手柄 B键** → 奔跑
- **左手扳机** → 射击
- **右手扳机** → 瞄准

## 🔧 设置步骤

### 第一步：配置Input Actions
1. 打开Unity的Input Actions配置面板
2. 确保您有以下Input Actions：
   - `PressA` (右手A键)
   - `LeftX` (左手X键) 
   - `LeftY` (左手Y键)
   - `RightB` (右手B键)

### 第二步：设置武器脚本
在每个武器的 `Weapon_AutomaticGun` 组件中：

1. 展开 **VR按键映射** 部分
2. 将对应的Input Action拖拽到字段中：
   - **Press A Action** → PressA Input Action
   - **Left X Action** → LeftX Input Action  
   - **Left Y Action** → LeftY Input Action
   - **Right B Action** → RightB Input Action

### 第三步：验证设置
启动VR测试，确认按键功能：
- 按左手X键应该播放武器检视动画
- 按左手Y键应该切换到下一把武器
- 按右手A键应该开始换弹动画
- 按住右手B键应该让角色奔跑

## 📋 Input Action配置详情

### 推荐的Input Action设置：

#### PressA (右手A键)
- **Action Type**: Button
- **Control Type**: Button  
- **Binding**: `XR Controller (Right Hand)/primaryButton`

#### LeftX (左手X键)
- **Action Type**: Button
- **Control Type**: Button
- **Binding**: `XR Controller (Left Hand)/primaryButton`

#### LeftY (左手Y键)  
- **Action Type**: Button
- **Control Type**: Button
- **Binding**: `XR Controller (Left Hand)/secondaryButton`

#### RightB (右手B键)
- **Action Type**: Button  
- **Control Type**: Button
- **Binding**: `XR Controller (Right Hand)/secondaryButton`

## 🎯 功能说明

### 检视武器 (左手X键)
- 触发武器的Inspect动画
- 让玩家可以近距离观察武器细节
- 支持键盘I键备用

### 切换武器 (左手Y键)
- 循环切换库存中的武器
- 自动切换到下一把可用武器
- 如果只有一把武器则无效果

### 换弹 (右手A键)
- 检查是否需要换弹（当前弹药 < 弹夹容量 且 备弹 > 0）
- 不在换弹状态时才能触发
- 支持键盘R键备用

### 奔跑 (右手B键)  
- 按住时角色以奔跑速度移动
- 松开时恢复正常行走速度
- 支持键盘Shift键备用
- 瞄准时无法奔跑

## 🔄 兼容性

### 键盘备用控制
所有VR按键都保留键盘备用选项：
- `I键` → 检视武器
- `R键` → 换弹  
- `Shift键` → 奔跑
- 滚轮 → 切换武器

### 多武器支持
- 每把武器都需要单独配置Input Action引用
- 所有武器共享相同的按键映射逻辑
- 切换武器时按键功能自动跟随

## ⚠️ 注意事项

### 设置要求
1. **确保所有武器都配置了Input Action引用**
2. **Input Actions必须在XR Origin上正确设置**
3. **每个武器预制体都需要配置相同的按键映射**

### 常见问题解决

#### 按键无响应
1. 检查Input Action Asset是否启用
2. 确认XR Origin配置正确
3. 验证Input Action引用是否为空

#### 切枪不工作
1. 确认Inventory组件存在
2. 检查武器是否在weapons列表中
3. 验证currentWeaponID是否有效

#### 奔跑无效果
1. 确认PlayerController能找到当前武器
2. 检查rightBAction是否public
3. 验证Input Action绑定正确

## 🚀 扩展建议

### 可以添加的功能
- **菜单键**: 暂停游戏/设置菜单
- **抓取键**: 拾取物品/手榴弹投掷
- **蹲下键**: 下蹲动作
- **交互键**: 开门/使用物品

### 自定义按键
可以通过修改脚本中的Input Action引用来自定义按键映射，满足不同玩家的操作习惯。

---

这个VR按键映射系统为您的VR FPS游戏提供了完整的手柄控制方案，让玩家可以自然地使用VR控制器进行各种游戏操作！ 