# 最小战斗 Demo

打开 `Assets/Scenes/SampleScene.unity` 并进入 Play。

- A/D 或左右方向键：移动；Space：跳跃 / 蹬墙跳；左 Shift：冲刺。
- 鼠标左键：攻击。动作期间再次点击可缓存下一段，最多三段。
- R：重载当前场景，重置玩家、敌人和胜负状态。

玩家与骷髅初始生命均为 100。玩家三段伤害为 25 / 30 / 35；骷髅攻击伤害为 20，前摇 0.65 秒，攻击动作结束后冷却 0.55 秒。

每段攻击只判定一次，同一目标即使有多个碰撞体也只受一次伤害。攻击不会穿过 Ground / Wall。受击会打断当前动作、击退并闪红；玩家受击后有 0.65 秒保护时间。死亡会停用碰撞和刚体模拟；骷髅播放现有 Dead 动画，玩家停帧并变暗。所有敌人死亡显示 VICTORY，玩家死亡显示 DEFEATED。

可调参数：Entity 的 Combat 区域设置生命、受击时长和击退；Player 的 Attack details 设置伤害、攻击范围与位移；Enemy_Skeleton 的 Melee attack 设置攻击距离、伤害、前摇和冷却。

复用资源：`Assets/Graphics/Enemies/Skeleton/Skeleton Attack.png`、`Skeleton Hit.png`、`Skeleton Dead.png`。对应动画已接入 Skeleton_AC。

验证入口：Tools → Combat → Run smoke test。测试会进入 Play，检查命中、背面不命中、墙体阻挡、多碰撞体去重、受击保护、双方死亡、重开，以及玩家与敌人的定时攻击。运行会临时改变测试场景，退出 Play 后还原；执行前请保存当前场景。结果写到项目根目录 `combat-test-result.txt`。

当前界面使用简单的运行时 HUD，玩家死亡采用停帧反馈；正式 UI、玩家死亡动作、音效和打击特效可后续扩展。
