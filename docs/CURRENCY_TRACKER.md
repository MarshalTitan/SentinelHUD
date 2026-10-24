# Currency Tracker (0.8.7.0)

Open **HUD Elements > Currency Tracker** in Modern, or the **Currency Tracker** Classic tab. Enable the module and choose currencies from the searchable checklist. It starts disabled with Gil selected. Select up to 24 entries; use Up/Down in Selected display order to arrange them.

Compact mode (**Show Names** off) displays game icon + amount, plus an optional cap. Full mode adds the localized currency name. Horizontal entries wrap within the configured width; Vertical stacks them. Show Icons, Show Caps, Full/Compact numbers, scale, opacity, width/entry height and normalized X/Y position are configurable. Unlock to move/resize it like other modules. The default anchor is near the bottom; locked gameplay remains click-through.

The catalog is discovered once from the current Item sheet's currency category/filter and the TomestonesItem sheet. It includes Gil, company seals, MGP, Wolf Marks, Bicolor Gemstones, hunt currencies and current/historical tomestones exposed by the client. Names/icons/holding caps follow game data rather than a list of localized currency names. New currencies therefore do not require hard-coded display metadata. Tomestone balance reads use their actual item IDs; Mnemonics is selected by its current game-data name. Selection IDs and order are saved and unknown IDs are retained through updates.

The adapter reads only selected balances, at most twice per second while enabled and the HUD is active. It uses InventoryManager's Gil/MGP/Wolf/Company/Tomestone helpers and generic inventory counts for other currencies. No pointer is retained between updates. Company-seal caps use GetMaxCompanySeals for the relevant company, so they follow rank; other known holding caps use Item.StackSize. Gil always shows its current amount. Weekly tomestone earnings/weekly limits are separate quantities and are not displayed as a holding cap. Unavailable amounts show `--`; unknown/removed currency IDs stay saved but have no fabricated balance.

Logout hides the gameplay module through the standard visibility policy; unavailable inventory reads clear to `--`. Editing can show the real layout with available balances or placeholders. The icon provider owns shared game textures; HUD does not dispose its shared wraps.

Schema 14 adds Currency Tracker and Arc text defaults only. Existing settings remain intact, and the protected configuration store makes its usual pre-migration backup. Core is unchanged at 0.3.1.

Primary API/schema references: [InventoryManager](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/InventoryManager.cs), [Item schema](https://github.com/xivdev/EXDSchema/blob/latest/Item.yml), [TomestonesItem schema](https://github.com/xivdev/EXDSchema/blob/latest/TomestonesItem.yml), [Dalamud texture provider](https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Plugin/Services/ITextureProvider.cs).

Automated tests cover configuration preservation, ordering/selection normalization, value/cap formatting, unavailable data and native compact/full wrapping. Actual character balances, currency icons and gain/spend transitions require the FFXIV live checklist.
