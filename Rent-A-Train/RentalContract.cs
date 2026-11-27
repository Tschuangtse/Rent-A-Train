using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Rent_A_Train
{
    // Stored on the original outside-connection object; its PrefabRef is never replaced.
    public struct RentalContract : IComponentData, ISerializable
    {
        public int Capacity;
        public int DailyPrice;
        public uint LastFrame;
        public double FractionalCharge;
        public long TotalCharged;
        public bool AddedDispatchBuffer;
        public bool AddedOwnedBuffer;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Capacity);
            writer.Write(DailyPrice);
            writer.Write(LastFrame);
            writer.Write(FractionalCharge);
            writer.Write(TotalCharged);
            writer.Write(AddedDispatchBuffer);
            writer.Write(AddedOwnedBuffer);
        }
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Capacity);
            reader.Read(out DailyPrice);
            reader.Read(out LastFrame);
            reader.Read(out FractionalCharge);
            reader.Read(out TotalCharged);
            reader.Read(out AddedDispatchBuffer);
            reader.Read(out AddedOwnedBuffer);
        }
    }
}
