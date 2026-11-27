using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine;

namespace Rent_A_Train
{
    // Separate from RentalContract, so the first train prototype's saved byte layout
    // stays unchanged. This entity references an original road outside connection.
    public struct BusRentalSource : IComponentData, ISerializable
    {
        public Entity Connection;
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter { writer.Write(Connection); }
        public void Deserialize<TReader>(TReader reader) where TReader : IReader { reader.Read(out Connection); }
    }
    public class RentalDepotPrefab : PrefabBase { }
    public partial class RentalPrefabSystem : GameSystemBase
    {
        internal Entity BusPrefab;
        protected override void OnCreate()
        {
            base.OnCreate();
            var prefab = ScriptableObject.CreateInstance<RentalDepotPrefab>();
            prefab.name = "RentATrain.VirtualBusDepot";
            var depot = prefab.AddComponent<Game.Prefabs.TransportDepot>();
            depot.m_TransportType = TransportType.Bus;
            depot.m_EnergyTypes = EnergyTypes.Fuel;
            depot.m_VehicleCapacity = 0;
            // Register before save deserialization so saved bus proxies resolve by ID.
            var system = World.GetOrCreateSystemManaged<PrefabSystem>();
            if (!system.AddPrefab(prefab)) throw new System.InvalidOperationException("Cannot register virtual bus depot prefab.");
            BusPrefab = system.GetEntity(prefab);
        }
        protected override void OnUpdate() { }
    }
}
