using System;

namespace Rent_A_Train
{
    public static class RentalMaintenancePolicy
    {
        public static bool NeedsReplacement(bool returning, bool requiresMaintenance, bool parked, bool dummy)
            => returning && requiresMaintenance && !parked && !dummy;

        // Extra slots account for old vehicles that are no longer available to
        // lines but are still physically travelling back. Never bill these slots.
        public static int DepotCapacity(int rented, int maintenanceReturns)
            => rented <= 0 ? 0 : rented + Math.Max(0, maintenanceReturns);
    }
}
