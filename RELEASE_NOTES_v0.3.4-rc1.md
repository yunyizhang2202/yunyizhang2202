# v0.3.4-rc1 — Release notes / 发布说明

**Undead Legacy Category Backpack** is a community-made inventory classification extension for **7 Days to Die V2.6 b14 + Undead Legacy 2.7.40**.

**Tested:** The original player reports that v0.3.4 works in-game (2026-11-10). This is **one installation**, not cross-system or multiplayer certification. Keep backups and test separately.

**Features / 功能**
- Filter within the original backpack instead of opening a separate search window.
- Preserve underlying bag capacity and native grid behavior.
- Main category tabs for weapons, ammunition, tools, equipment, medical, food, materials, knowledge, etc.
- Materials second-level tabs: ores/ingots, basic stock, industrial parts, vehicle parts, chemicals, building, farming, energy.
- Rule corrections for knowledge, books, components, and exact item-ID overrides.

**Installation / 安装**
1. Install the supported game and Undead Legacy separately; they are not distributed with this project.
2. Quit the game, back up saves/Mods, and extract the complete source package.
3. Double-click `INSTALL.cmd`. It checks the supported DLL hashes and builds the plugin locally against your own installed game. Do **not** copy the uncompiled source-only Mod folder into `Mods`.
4. Launch with EAC disabled where permitted, then test the categories, scroll wheel, item selection, drag-and-drop, and Smart Network in a throwaway test world before normal play.
5. Use `ROLLBACK_LAST.cmd` to restore the previous category-backpack version if an issue occurs.

**Limitations / 限制**
- Strictly limited to tested 7DTD V2.6 b14 / UL 2.7.40 DLL hashes.
- Does not include game DLLs, other mods, or game assets.
- Smart Network co-existence is a design target, not universal assurance.
- Third-party item additions and translations may require rule overrides.
- This is an independent fan project, not official The Fun Pimps or Subquake software.

**License:** MIT for original project code and material; not for third-party games or mods. See `NOTICE.md`.
