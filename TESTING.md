# Prototype 0.2.5 test guide

Restart Cities: Skylines II after the build. Use a separate test save, including
when upgrading a save from prototype 0.1. Existing train contracts should restore.

## Priority regression: Finance UI

Open Economy / Budget, switch tabs, close it, reopen it several times, and press
ESC. The rest of the UI and pause menu must remain usable. Check UI.log for no
`TutorialGuideTarget requires a single child react element` error. If another
budget-extension error occurs, Rent a Train should log it and show the native
budget view instead of losing the whole interface.

## Compact panel, fleet overview and icon

Verify that the train/dollar graphic is visible, `8 / 10` stays on one line, and
no Rental Fleet heading, simulation-running hint or Options footer appears.
Compare Vehicles needed against configured bus/train line vehicle counts. Add or
upgrade a local depot and verify Depot capacity changes; rented capacity must not
be counted as an owned depot. Check Rented and the calculated Shortfall. Disabled
lines are excluded, while configured day/night lines and train cargo lines count.
Summary refresh runs only while the panel is open and also works while paused.

## Settings

1. Find the cyan rounded-square **half-train / dollar** button at the top left.
2. Open the panel, switch between **Buses** and **Trains**, drag the quantity slider,
   and use minus/plus. Apply a rental and resume the simulation.
3. Check defaults in **Options > Rent a Train**: 20 buses at 1,000 each per day;
   10 train sets at 5,000 each per day.
4. Each limit can be set to 100; each price ranges from 100 to 10,000. Confirm that
   the panel's slider maximum and displayed rate follow their own vehicle type.
5. Change values, then use **General > Reset to defaults**. Confirm all four defaults
   return. Reducing a maximum clamps an active rental after resuming.
6. Check layout and slider usability at your usual resolution/UI scale. The current
   build is verified structurally outside the game; native Cohtml rendering needs testing.

## Depot behavior

- **Train:** without a built rail yard, rent 3 sets. Make a local passenger train
  line on tracks reaching a railway outside connection and request vehicles through
  the line's ordinary controls. Check arrivals and actual passenger boarding.
- **Bus:** without a built bus depot, rent 5 buses. Make a bus line reachable from a
  road outside connection, then request vehicles normally. Check that they arrive,
  board passengers and remain on their routes. The original outside taxi function
  should continue; the bus depot is a separate invisible proxy.
- Add another line of the same type. Its rental pool is shared, not duplicated.
- Lower quantities, observe excess vehicles returning, then raise them again.
- Run long enough to observe maintenance return/replacement. Cargo trains can share
  the train pool, so use passenger-only lines for the first test.
- Save with both rentals active, exit to desktop, restart and reload. Check quantities,
  fleets, rates and charged totals; no Apply action is required for saved contracts.

## Budget

Rent 5 buses and 3 train sets at the defaults. The combined rate is **20,000 per
simulation day** (5,000 bus + 15,000 train). In **Economy > Budget**, verify:

- **Rented vehicles** appears under Expenses.
- Selecting it shows **Rented buses** and **Rented trains** separately.
- The expense total, monthly balance and stacked expense bar all include that rate.
- Changing one vehicle type updates the correct cost; setting both rentals to zero
  leaves a zero-valued row and removes their contribution from the totals.
- The rental panel's charged total grows by 20,000 over a full standard simulation
  day, apart from rounding carried between ticks. Other city income/expenses affect
  the treasury separately. Pausing stops accrual.

Budget uses the game's native monthly-display convention for simulation-day rates.
The rental is deducted once by the simulation. The budget decoration never deducts
money. Historical Statistics-panel categories and toolbar income forecasts remain
vanilla in this prototype.

## Cleanup

Set both rental quantities to 0, apply and resume. Wait for deployed/returning counts
to reach 0. In **Options > Rent a Train > General**, press **Remove inactive rental
data**, resume, save and exit. Test disabling the mod only on this disposable save.
Infrastructure unlocks remain after cleanup.

## Problem report

Use **General > Write diagnostic report**, resume, and provide relevant contents of:

`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\RentATrain.log`

For UI loading errors, also check `Logs/UI.log` and `Player.log` in the parent folder.
Include the game version, status text, rented/deployed numbers, built depots, cargo
lines, other transport/budget mods and the step that failed. Distinguish a vehicle
never being created from a listed vehicle stuck or not physically spawned.

Automated accounting tests cover limits, rounding, rate changes, save-like fractional
carry, pause and timeline resets. UI tests cover the budget row, separate costs,
expense total, balance, detail panel, chart and preserving native inputs. They do not
replace actual Unity, save/load, bus spawning, or Cohtml rendering tests.


## 0.2.2 priority checks

1. Locate the icon in the top toolbar and check that no white focus/shadow shape
   appears around it when opened, closed or hovered.
2. Click the middle and both ends of the slider track; drag its thumb. Values must
   follow the position rather than resetting to zero. Check plus/minus too.
3. Set 5 buses, switch to Trains, set 8 trains, switch back: the draft must still be
   5. Apply once and verify both contracts after simulation resumes.
4. Confirm the label is `Change confirmed`, stays that way across tab switches,
   and returns to `Apply` after either tab's quantity is changed.
5. Edit without applying, close, reopen: quantities must match the last applied
   contracts rather than the discarded edits.
6. Rent buses through an existing ordinary road outside connection. No dedicated
   bus road is needed. Check creation, physical arrival and actual line service.
   If connection lookup fails, the log now automatically reports road markers,
   their owner/attachment and the resolved lane source.

The road resolver and UI interaction checks are automated; physical bus entry
and native toolbar rendering still need validation in the running game.

## 0.2.3 priority checks: reachable rental window

1. After restarting, check that the Rent a Train icon is beside the top-left infoview
   control at the same vertical position, without additional top/left offset.
2. Open it. The entire window must be visible in the screen center regardless of
   which toolbar/tool is active. Drag the title bar in several directions and
   confirm that the slider, tabs, Apply and close button remain usable.
3. In **General > Interface**, enable **Use Universal Mod Menu**. The dedicated icon
   should disappear and a Rent a Train entry should appear inside that menu.
   Both routes must open the same rental window. Disable the option to restore
   the dedicated icon.
4. Change **Initial window position** to **Upper left**. Reopen and check its position
   near the usual infoview panel. It must still be freely draggable. Restore
   **Center** and check reopening again.
5. Edit one tab, switch to the other, apply both, and verify `Change confirmed`.
   Unconfirmed edits should still survive tab changes but be discarded on closing.
6. All visible rental cost labels must use month terminology. Values and billing
   must match 0.2.2; this update does not multiply costs or migrate contracts.

The old 0.2.2 instruction to look in the construction toolbar is superseded by
these checks. Native Cohtml positioning and Universal Mod Menu integration still
need a game test; automated checks verify registrations, modes and panel props.

## 0.2.4 visual checks

- Compare the dedicated icon directly with the native infoview button: identical
  outer height and top alignment, with no smaller inset tile or additional offset.
- In Upper left mode, confirm a visible gap below the button row matching the
  normal infoview panel. Check at the usual UI scale and a second resolution.
- Drag the panel after initial placement; it should follow the pointer normally
  without jumping by the starting offset.
- Enable Universal Mod Menu. Its Rent a Train entry should use a compact native
  round button with a transparent glyph, without the old large cyan square.
- The previously verified maximum-rental reduction should remain unchanged.

## 0.2.5 maintenance replacement

Use lines whose demand fills the rented pool. For each of buses and trains:

1. Rent a small fleet and let it run until a vehicle starts returning for maintenance.
2. Confirm a replacement is dispatched while the old vehicle is still on its way
   out, rather than waiting for the old one to reach the map boundary.
3. Distinguish the replacement being listed/approaching from its later arrival on
   the line. Normal outside travel time and native dispatch intervals still apply.
4. Several maintenance returns should each free one service slot. The combined
   visible fleet can temporarily exceed the contract while returners remain.
5. After old vehicles leave, reserve slots must disappear; replacements must not
   themselves be retired merely because the old returner finished its trip.
6. Reduce the contract while maintenance returns are travelling. The remaining
   service slots should match the smaller contract. End the rental entirely and
   confirm that no replacement is spawned for a zero contract.
7. Save with a maintenance return and replacement present, restart and reload.
   Check ownership, capacity and billing resume correctly.
8. Confirm rent remains based on the configured contract (e.g. 10 train sets cost
   50,000 per monthly cycle), including during temporary extra returner capacity.

Write diagnostics during steps 2 and 4. The report includes maintenanceReturns for
both rental types. Automated policy checks do not replace this physical lifecycle test.
