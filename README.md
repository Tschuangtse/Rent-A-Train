# Rent a Train

Cities: Skylines II code mod, prototype **0.2.5**. In-game language: English.

Rent buses and train sets from virtual depots instead of building local depots.
The game requests vehicles for lines through its normal dispatch system. Vehicles
enter from a suitable outside connection. No manual rental-to-line assignment is needed.

## Controls

Use the **cyan train-and-dollar button beside the infoview control**. The rental
panel has separate **Buses** and **Trains** tabs. Choose a quantity, inspect its daily
cost and press **Apply rental**. Resume the simulation for changes to take effect.
Setting a quantity to zero ends that rental; excess vehicles return through game logic.
The compact panel shows Vehicles needed, effective city Depot capacity, Rented, and Shortfall, followed by quantity and cost controls.

**Options > Rent a Train** also has **Buses**, **Trains**, and **General** tabs:

| Setting | Buses | Train sets | Allowed range |
|---|---:|---:|---:|
| Maximum rented vehicles | 20 | 10 | 1–100 per type |
| Price per vehicle per simulation day | 1,000 | 5,000 | 100–10,000 per type |

The General tab provides **Reset to defaults**, diagnostics, and removal of inactive
rental data. Reset restores limits/prices, retaining quantities within the restored
limits. Price/limit changes apply to active contracts when simulation resumes.
The previous prototype's train contracts retain the same serialized byte layout.
Its obsolete option keys do not override the new default limits.

## Costs and Budget

All contracted capacity is billed, including unused slots. Charges accrue in
proportion to simulation time, retaining fractional currency across updates and
save/load. A standard simulation day is 262,144 frames. Paused time costs nothing.

**Economy > Budget > Expenses > Rented vehicles** shows the current combined rate,
with separate bus/train details on selection. The expense total, monthly balance,
and stacked chart include rent. This follows the game's own budget convention:
its displayed monthly amounts are charged over a standard simulation-day cycle;
there is no extra multiplication by 30.

The new row is a UI extension to the existing budget layout. Actual treasury
charges remain in the rental simulation and are not applied again by the display.
Historical Statistics-panel categories and the toolbar's vanilla income projection
are not extended yet. A dynamic native expense enum category is not introduced.

## Architecture and limits

- The working train implementation uses one original railway outside connection.
- Buses use a separate, invisible depot entity with a stable registered prefab and
  an owner link to one existing road outside connection. The original road
  connection and its taxi depot are preserved. It references the existing lanes;
  no extra outside city or duplicate lane network is created.
- Existing depot dispatch, vehicle selection, travel, return and retirement remain
  in the game's systems. The new bus integration still needs in-game verification.
- One connection per type is selected. Its road/rail network must reach the lines.
  A selector for disconnected networks is not implemented yet.
- Diesel buses are supported by the first bus prefab. Electric variants are pending.
- Cargo trains may share the train pool because vanilla train depots serve both.
- Lowering a limit may temporarily leave excess deployed/returning vehicles.
- Depot-build requirements are unlocked when first renting that type. Other
  progression requirements remain; unlocked infrastructure stays unlocked.
- Mods changing simulation-day length or the same budget components may need
  compatibility work. No performance measurements in the game are claimed.

The rental simulation runs every 256 frames and inspects its two contracts and
owned fleets, rather than scanning all city vehicles. UI bindings update only
when rental state, status or settings change. React is supplied by the game.
No Harmony patches or network services are used. Normal vehicle/pathfinding costs
still apply to each active vehicle.

## Build and verification

Requires the game's installed CS-II modding toolchain (`CSII_*` user variables),
.NET SDK and .NET Framework 4.8 targeting tools. Targets installed game **1.6.4f1**.

```powershell
dotnet build .\Rent-A-Train.sln -c Debug
.\tests\Test-Accounting.ps1
node .\tests\Test-UI.mjs
```

A build postprocesses and deploys the assembly and UI files into the configured
local Mods folder (`Mods/Rent-A-Train`). It performs no publishing action.
See [TESTING.md](TESTING.md) for in-game acceptance tests and cleanup.

## 0.2.1 fixes

- Fixed the confirmed Budget crash: recursively decorating a single React child no
  longer converts it to an array. The game tutorial wrapper requires one element.
- Budget page/chart extensions have local error boundaries with native UI fallback.
- Embedded the SVG icon as a data URI, avoiding the failed external UI resource URL.
- Reduced rental-panel width from 430 to 320 UI units and removed redundant text.
  Quantities are a single text node, so `8 / 10` cannot split into separate blocks.
- Fleet overview refreshes twice per second only while the panel is open, including
  while the simulation is paused. Demand uses the native vehicle-count calculation,
  line interval modifiers, stable segment durations and stop durations. Disabled
  lines are excluded; day/night schedules both contribute their configured demand.
- Owned depot capacity uses installed upgrades and the game's efficiency calculation.
  Rental depots are excluded. Shortfall = max(0, needed - owned capacity - rented).
  Train demand includes cargo lines, because they share the native train depot pool.
- Road outside markers attached to network nodes can now provide bus proxy lanes.

The preview helper in `tests/Preview-UI.mjs` uses production markup with lightweight
native-component substitutes. It checks layout outside the game, not Cohtml behavior.

## 0.2.2 interaction and road-connection fixes

- The icon is inside the native top toolbar, using a plain image button without
  the generic UI button's white focus decoration.
- Slider clicks use the native `(start, end, normalizedPosition)` contract.
- Bus/train drafts survive tab changes. Apply submits both tab quantities together.
  The button reads `Change confirmed` until a quantity is edited. Closing and
  reopening discards unconfirmed edits and reads the actual rental quantities.
- Bus connection lookup follows both Owner and Attached relationships to a lane
  source. Ordinary road outside connections are supported; no bus-only road or
  dedicated bus outside connection is required. Failed lookups log road diagnostics.
- Pricing is unchanged: one full 24-hour day/night cycle is one in-game month in
  CS2. Ten trains at 5,000 cost 50,000 over that cycle, also shown as 50,000 in the
  monthly budget. There is no extra factor of 30.

Run `tests/Test-RoadConnections.ps1` to verify the production connection resolver's
Owner/Attached traversal, absent connections and cycle handling. `Test-UI.mjs` now
also exercises actual UI handlers across edits, tab changes, confirmation and close.

## 0.2.3 access and window placement

The rental button is back in `GameTopLeft`, aligned with the native infoview button.
Its extra wrapper margins have been removed. It is no longer attached to the
bottom construction toolbar. The rental window is registered separately in `Game`
with a full-screen, non-blocking fixed layer, so its coordinates are independent
of the selected icon entry. The native draggable panel opens at the screen center.

**General > Interface** provides:

- **Use Universal Mod Menu**: show the entry in the Universal Mod Menu and hide the
  dedicated top-left icon. Off by default.
- **Initial window position**: **Center** (default) or **Upper left**, approximately
  where the infoview panel opens. Drag the title bar to reposition the window.

The window resets to the configured starting location on reopening. Changing this
option while open recreates the native panel at the selected starting location.
Both entry points operate the same window and the same bus/train draft state.

All in-game price labels now say **per month** and **Monthly rental cost**. The
stored setting keys, contract format, prices and billing calculation are unchanged.
Existing `BusDailyPrice`/`TrainDailyPrice` identifiers remain internal compatibility
keys, with monthly labels in the options UI.

## 0.2.4 native button sizing and infoview spacing

Dedicated entry: the game's native floating button, with no custom size, margin,
image wrapper or focus rules. Universal Mod Menu entry: native round button using
only the transparent train/dollar glyph, without the large cyan tile background.
Native tooltips and selected-state handling are used for both entries.

Upper-left window placement now measures the panel and a CSS probe at the native
infoview origin: left 10rem, top calc(10rem + var(--floatingToggleSize) + 6rem).
These pixel coordinates are converted into the native draggable panel's normalized
position using its actual dimensions and viewport. No persistent margin or transform
is applied, so title-bar dragging remains controlled by the game. Centered mode is
unchanged. If initial measurement fails, a bounded retry falls back to the previous
usable upper-left location rather than keeping the window hidden.

The configured maximum rental reduction and free dragging have been confirmed
in the user's game tests. Native icon rendering and the revised gap need another
visual check in the game. UI tests cover native variants, transparent menu artwork,
measured coordinates at different resolutions, and the earlier interaction fixes.

## 0.2.5 maintenance replacement capacity

The old prototype counted every owned moving vehicle against the rental limit,
including maintenance vehicles still returning to the outside connection. That
could block a replacement until the old vehicle left the map.

The runtime depot now receives one temporary extra slot per genuine maintenance
returner (Returning + RequiresMaintenance, excluding parked/deleted/dummy traffic).
Returners are prioritized in the depot's owned-vehicle buffer because the native
capacity/retirement logic processes that order. This leaves the contracted number
of slots for service vehicles even during a rental reduction. Ordinary retirement
or an unused vehicle returning does not create a replacement allowance.

The allowance is recomputed from actual vehicles, not accumulated. It disappears
when returners leave. A zero contract always has zero replacement capacity.
Contract quantities, configured limits, invoice calculation and serialized contract
layout are unchanged. Saved runtime depot data can include a maintenance allowance
and is adopted/rebuilt when loading the contract.

Replacement still uses native line requests and dispatch. The mod releases capacity
on its next 256-frame simulation update; it does not manually assign a replacement
to a line or teleport it. Only existing line demand causes a spawn, and travel from
the outside connection remains necessary. Therefore it cannot promise uninterrupted
physical vehicle counts on every line or instantly available replacement at stops.
`tests/Test-Maintenance.ps1` checks replacement eligibility, multiple returns,
reserve removal, rental reductions/cancellation and unchanged contract billing.
Physical maintenance return and replacement timing still need an in-game test.
