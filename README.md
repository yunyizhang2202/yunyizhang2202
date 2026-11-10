# Undead Legacy Category Backpack / 亡灵遗产分类背包

> **Experimental community mod** · v0.3.4-rc1 · Windows · **not an official Undead Legacy / 7 Days to Die mod**

A **category-filtering UI inside the existing Undead Legacy inventory**. The goal is to find items quickly without expanding the actual backpack capacity or moving items between saved inventory slots.

**中文说明：** 本模组给《七日杀》亡灵遗产的原背包增加分类按钮与材料二级分类，不增加背包容量。不会主动改变实际物品数据或 Smart Network 的自动仓储规则。它是第三方实验版，并非《七日杀》或《亡灵遗产》官方作品。

## Status / 当前状态

- Targeted at **7 Days to Die V2.6 b14 + Undead Legacy 2.7.40**, verified against specific supplied game DLL SHA-256 values at install time; other builds are not confirmed.
- Earlier v0.3.2 was installed and exercised in-game by a user; v0.3.3 compiled and passed mock/contract tests on that user's Windows installation.
- **v0.3.4-rc1 was reported working in-game by the original tester on 2026-11-10**, including the updated categories. This is a single-user confirmation, **not** independent validation across other PCs, game versions, multiplayer servers, or anti-cheat modes. **Back up your saves and Mods folder and test in a spare world first.**
- Requires running with **EAC disabled** for Harmony mod loading. Use only in sessions where this is permitted.

## Features / 功能

- Main tabs: All / Weapons / Ammo / Tools / Equipment / Medical / Food / Materials / Knowledge / Other.
- Materials subcategories: Ores & Ingots, Basic Stock, Industrial Parts, Vehicle Parts, Chemicals, Building, Farming, Energy.
- Keeps native Undead Legacy inventory slots, scrollbar and item interactions in place; **does not enlarge capacity**.
- Per-item map plus optional exact-ID overrides, with unknown items falling back to Other.
- Designed to coexist with Smart Network without modifying its storage routing. **This is a design goal, not a blanket compatibility guarantee.**

## Download / 下载

- [Download the complete source installer ZIP / 下载完整源码安装包](RELEASES/UndeadLegacy_CategoryBackpack_v0.3.4-rc1_PUBLIC_SOURCE.zip). Extract all files before running `INSTALL.cmd`. This archive contains original project files only; the plugin DLL is compiled on the player's computer.

## Install / 安装

1. Install an **owned copy** of 7 Days to Die V2.6 b14 and Undead Legacy 2.7.40 separately. Neither game nor overhaul is included here.
2. Close the game and server. Back up your saves and current Mods directory.
3. Download the **Source code (ZIP)** of this repository, or clone it. Extract **all files**, keeping the folder structure.
4. Double-click `INSTALL.cmd` from the extracted project root. Enter your game folder (the folder containing `7DaysToDie.exe`). The prompt's default path may be different from yours.
5. The installer checks package hashes and the exact supported DLL builds, runs isolated C# contract tests, compiles the plugin **using your own local game DLLs**, and backs up/replaces **only this category-backpack mod**.
6. Wait for `INSTALLED v0.3.4-rc1. Compiler and mock tests passed.` This proves installation/tests, **not gameplay**.
7. With EAC disabled, enter a spare world, press `Tab`, test tabs and Material submenu, then test wheel, hover, drag, locked slots and nearby Smart Network storage.

**中文速装：** 退出游戏 → 备份 → 完整解压本仓库 ZIP → 双击 `INSTALL.cmd` → 输入游戏目录 → 看到 `INSTALLED` 后进入测试世界验证。**不要手动直接复制未编译的模组文件夹。**

## Uninstall / Rollback

- `ROLLBACK_LAST.cmd` restores the last category-backpack version backed up by this installer (if present).
- `UNINSTALL.cmd` disables the category-backpack mod; it does **not** remove Undead Legacy or other mods.
- All game-save changes are outside the scope of the installer. Retain backups until you have tested.

## Compatibility and safety

- Windows PowerShell and the .NET Framework C# compiler bundled with supported Windows installations are needed. Windows/Unity C# compilation **was not independently run in the preparation environment**.
- The version check is deliberately strict and verifies the local `UndeadLegacy.dll` and `Assembly-CSharp.dll` hashes. If it fails, **do not bypass it or edit game version files**. Open an issue with the build information (do not share game DLLs publicly).
- Installer runs entirely locally, with no download/network action. It does not include game DLLs, third-party MOD binaries, save files, player profiles or screenshots.
- No game or mod assets are provided; game, overhaul and optional Smart Network are **separate dependencies**.

## Classification limitations

The category mappings were derived by examining configuration files from a legally held 7 Days to Die / Undead Legacy installation, including a static snapshot of some third-party item definitions. These mappings are **not a guarantee of all game items**. Chinese/localized names depend on the player's own game files. The optional full catalogue from private development is intentionally **not** included in this public repository.

`Config/CategoryOverrides.tsv` can provide exact-ID category corrections. Do not edit or distribute another mod's proprietary source code to add mappings.

## License / 许可

MIT applies to **the original code, UI patch text, categorization rules and documentation authored for this project**. It does **not** license or grant rights in the game, Undead Legacy, third-party mods, names, images or other separately owned assets. See [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md).

## Related projects and credits

Independent mod, inspired in **functionality only** by existing inventory/storage mods. Similar projects already exist; this project does **not** claim to be the first categorized backpack mod:

- [SortedBackpack by madsoul](https://www.nexusmods.com/7daystodie/mods/9213) – customizable page tabs; adds inventory slots (different design).
- [Mazriq's Auto Sorting Backpack](https://www.nexusmods.com/7daystodie/mods/10869) – automatic category UI idea.
- [Smart Network by Prostrell3001](https://www.nexusmods.com/7daystodie/mods/12283) – separate nearby-storage routing system, not included or modified here.
- The Fun Pimps (7 Days to Die); Subquake (Undead Legacy).

**No source code or downloadable assets from those mods are included.** Their licenses/permissions apply independently.

## Contributions

Please report classification errors with **item ID, expected category, actual category, game/UL version**, optionally an inventory screenshot (redact personal paths/IDs). Don't upload proprietary game DLLs, save files or other authors' mod archives to GitHub issues.
