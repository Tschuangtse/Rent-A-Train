using System;

namespace Rent_A_Train
{
    public static class RentalAccounting
    {
        public const double FramesPerDay = 262144.0;
        public static int ClampCapacity(int requested, int maximum) => Math.Max(0, Math.Min(100, Math.Min(requested, maximum)));

        // Carry fractional currency across updates and save/load; rounding each tick
        // independently would make small contracts free or overcharge them.
        public static int Accrue(int capacity, int dailyPrice, uint elapsed, ref double remainder)
        {
            if (elapsed > int.MaxValue) return 0;
            remainder += (double)capacity * dailyPrice * elapsed / FramesPerDay;
            // Stay inside PlayerMoney's arithmetic range even after a large time jump.
            int amount = (int)Math.Min(100000000, Math.Floor(remainder));
            remainder -= amount;
            return amount;
        }
    }
}
