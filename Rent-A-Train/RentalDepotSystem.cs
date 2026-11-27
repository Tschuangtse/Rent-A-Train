using System;
using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using SubLane = Game.Net.SubLane;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Depot = Game.Buildings.TransportDepot;
using Outside = Game.Objects.OutsideConnection;

namespace Rent_A_Train
{
    /// <summary>
    /// Gives one existing railway outside connection a depot role. Vehicle requests,
    /// pathfinding, spawning, ownership, maintenance return and retirement stay vanilla.
    /// A dedicated bus proxy preserves road outside connections and their taxi role.\n    /// No building, Harmony patch or full-city vehicle scan is needed.
    /// </summary>
    public partial class RentalDepotSystem : GameSystemBase
    {
        internal static readonly List<RentalRequest> Requests = new List<RentalRequest>();
        internal struct RentalRequest { public bool Bus; public int Capacity; }
        internal static RentalView BusView = new RentalView(), TrainView = new RentalView();
        internal static bool CityReady;
        internal static int Revision;
        internal sealed class RentalView { public int Capacity, Fleet; public long Charged; }
        internal static void RequestRental(int type, int capacity)
        {
            if (type != 0 && type != 1) return;
            Requests.Add(new RentalRequest { Bus = type == 0, Capacity = capacity });
        }
        internal static bool DiagnosticsRequested;
        internal static bool CleanupRequested;
        internal static string Status = "Load a city and use the train-and-dollar button to rent vehicles.";
        private EntityQuery outsideQuery, contractQuery, depotPrefabQuery, moneyQuery;
        private SimulationSystem simulation;
        private PrefabSystem prefabs;
        private bool ready;
        private readonly HashSet<Entity> injectedPrefabs = new HashSet<Entity>();


        public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;

        protected override void OnCreate()
        {
            base.OnCreate();
            simulation = World.GetOrCreateSystemManaged<SimulationSystem>();
            prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
            outsideQuery = GetEntityQuery(ComponentType.ReadOnly<Outside>(), ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            contractQuery = GetEntityQuery(ComponentType.ReadWrite<RentalContract>(), ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            depotPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<TransportDepotData>(), ComponentType.ReadOnly<PrefabData>());
            moneyQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerMoney>());
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            ready = false;
            Requests.Clear();
            DiagnosticsRequested = CleanupRequested = CityReady = false;
            BusView = new RentalView(); TrainView = new RentalView(); Revision++;
            RestorePrefabData();
            Status = "Waiting for a city to load.";
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            ready = CityReady = mode == GameMode.Game;
            Revision++;
            if (ready)
            {
                using (var saved = contractQuery.ToEntityArray(Allocator.Temp))
                {
                    foreach (Entity entity in saved)
                    {
                        Entity prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                        if (!EntityManager.HasComponent<TransportDepotData>(prefab)) continue;
                        var data = EntityManager.GetComponentData<TransportDepotData>(prefab);
                        var contract = EntityManager.GetComponentData<RentalContract>(entity);
                        if (IsRailway(prefab) && data.m_TransportType == TransportType.Train
                            && data.m_ProductionDuration == 0 && data.m_MaintenanceDuration == 0
                            && data.m_VehicleCapacity >= contract.Capacity)
                            injectedPrefabs.Add(prefab);
                    }
                }
            }
            Status = "City loaded. Resume the simulation to restore saved rentals or apply a new rental.";
        }

        protected override void OnUpdate()
        {
            if (!ready || Mod.Settings == null || moneyQuery.CalculateEntityCount() != 1) return;
            // All mutations are on the simulation thread. ECS access synchronizes only the
            // relevant component dependencies; we do not force CompleteAllTrackedJobs().
            while (Requests.Count > 0)
            {
                var request = Requests[0]; Requests.RemoveAt(0);
                try { ApplyRental(request.Bus, request.Capacity); }
                catch (Exception ex) { Fail("Cannot apply rental", ex); }
            }
            using (var contracts = contractQuery.ToEntityArray(Allocator.Temp))
            {
                int buses = 0, trains = 0;
                foreach (Entity depot in contracts) { if (IsBus(depot)) buses++; else trains++; }
                if (buses > 1 || trains > 1)
                { Status = "Duplicate rental depots found. No changes made; write diagnostics."; return; }
                foreach (Entity entity in contracts)
                {
                    try { TickContract(entity); }
                    catch (Exception ex)
                    {
                        // An unexpected runtime error must not repeatedly mutate a city.
                        Fail("Rental simulation stopped; reload the test save", ex);
                        ready = CityReady = false;
                        return;
                    }
                }
            }
            if (CleanupRequested)
            {
                CleanupRequested = false;
                using (var contracts = contractQuery.ToEntityArray(Allocator.Temp))
                {
                    foreach (Entity entity in contracts)
                    {
                        var contract = EntityManager.GetComponentData<RentalContract>(entity);
                        if (contract.Capacity != 0 || CountFleet(entity) != 0)
                        {
                            Status = "Set rental capacity to zero and wait for all rented vehicles to return before removing rental data.";
                            return;
                        }
                    }
                    // Validate every contract before removing any rental data.
                    foreach (Entity entity in contracts)
                    {
                        var contract = EntityManager.GetComponentData<RentalContract>(entity);
                        RemoveEmptyDepot(entity, contract);
                        EntityManager.RemoveComponent<RentalContract>(entity);
                        if (IsBus(entity)) EntityManager.AddComponent<Deleted>(entity);
                    }
                }
                Status = "Rental data removed. Save the city before disabling the prototype. Infrastructure unlocks remain.";
                BusView = new RentalView(); TrainView = new RentalView(); Revision++;
                Mod.log.Info(Status);
            }
            if (DiagnosticsRequested)
            {
                DiagnosticsRequested = false;
                WriteDiagnostics();
            }
        }

        private void ApplyRental(bool bus, int capacity)
        {
            int requested = RentalAccounting.ClampCapacity(capacity, Mod.Settings.Limit(bus));
            int price = Mod.Settings.Price(bus);
            using (var contracts = contractQuery.ToEntityArray(Allocator.Temp))
            {
                Entity entity = Entity.Null;
                foreach (Entity candidate in contracts)
                {
                    if (IsBus(candidate) != bus) continue;
                    if (entity != Entity.Null) throw new InvalidOperationException("Duplicate rental depots exist.");
                    entity = candidate;
                }
                if (entity == Entity.Null)
                {
                    if (requested == 0) { Status = "No active rental. Choose a capacity above zero."; return; }
                    entity = bus ? CreateBusProxy() : FindConnection();
                    if (entity == Entity.Null)
                    {
                        Status = bus ? "No road outside connection with usable lanes found." : "No compatible railway outside connection found.";
                        Mod.log.Warn(Status);
                        WriteDiagnostics();
                        return;
                    }
                    // Validate before adding any components to an existing game object.
                    if (!bus) ValidatePrefab(entity);
                    EntityManager.AddComponentData(entity, new RentalContract
                    {
                        LastFrame = simulation.frameIndex,
                        AddedDispatchBuffer = !EntityManager.HasBuffer<ServiceDispatch>(entity),
                        AddedOwnedBuffer = !EntityManager.HasBuffer<OwnedVehicle>(entity)
                    });
                }
                var contract = EntityManager.GetComponentData<RentalContract>(entity);
                Charge(ref contract); // Settle elapsed time at the old contract price first.
                contract.Capacity = requested;
                contract.DailyPrice = price;
                EntityManager.SetComponentData(entity, contract);
                if (requested > 0)
                {
                    EnsureDepot(entity, contract);
                    UnlockDepotBuildRequirements(bus);
                }
                Revision++;
                Status = $"Rental applied: {requested} {(bus ? "buses" : "train sets")}, {requested * price:N0}/month.";
                Mod.log.Info(Status + $" Connection={entity}, prefab={Name(EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab)}");
            }
        }

        private bool IsBus(Entity entity) => EntityManager.HasComponent<BusRentalSource>(entity);

        private Entity CreateBusProxy()
        {
            Entity connection = Entity.Null;
            using (var connections = outsideQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity candidate in connections)
                {
                    Entity prefab = EntityManager.GetComponentData<PrefabRef>(candidate).m_Prefab;
                    if (!EntityManager.HasComponent<OutsideConnectionData>(prefab) ||
                        (EntityManager.GetComponentData<OutsideConnectionData>(prefab).m_Type & OutsideConnectionTransferType.Road) == 0 ||
                        GetBusLaneSource(candidate) == Entity.Null) continue;
                    if (connection == Entity.Null || candidate.Index < connection.Index) connection = candidate;
                }
            }
            if (connection == Entity.Null) return Entity.Null;
            Entity busPrefab = World.GetOrCreateSystemManaged<RentalPrefabSystem>().BusPrefab;
            if (!EntityManager.Exists(busPrefab) || !EntityManager.HasComponent<TransportDepotData>(busPrefab))
                throw new InvalidOperationException("Virtual bus depot prefab is not initialized yet; try again after resuming.");
            Entity proxy = EntityManager.CreateEntity(typeof(PrefabRef), typeof(Game.Objects.Transform), typeof(Owner),
                typeof(BusRentalSource), typeof(Depot), typeof(OwnedVehicle), typeof(ServiceDispatch), typeof(SubLane));
            EntityManager.SetComponentData(proxy, new PrefabRef(busPrefab));
            EntityManager.SetComponentData(proxy, EntityManager.GetComponentData<Game.Objects.Transform>(connection));
            EntityManager.SetComponentData(proxy, new Owner(connection));
            EntityManager.SetComponentData(proxy, new BusRentalSource { Connection = connection });
            RefreshBusLanes(proxy);
            Mod.log.Info($"Created virtual bus depot {proxy} at road outside {connection}; original taxi depot retained.");
            return proxy;
        }

        private Entity GetBusLaneSource(Entity connection)
        {
            return RoadConnectionResolver.FindLaneSource(connection,
                entity => EntityManager.Exists(entity) && !EntityManager.HasComponent<Deleted>(entity)
                    && EntityManager.HasBuffer<SubLane>(entity) && EntityManager.GetBuffer<SubLane>(entity,true).Length > 0,
                entity => EntityManager.Exists(entity) && EntityManager.HasComponent<Owner>(entity)
                    ? EntityManager.GetComponentData<Owner>(entity).m_Owner : Entity.Null,
                entity => EntityManager.Exists(entity) && EntityManager.HasComponent<Game.Objects.Attached>(entity)
                    ? EntityManager.GetComponentData<Game.Objects.Attached>(entity).m_Parent : Entity.Null);
        }

        private void RefreshBusLanes(Entity entity)
        {
            Entity connection = EntityManager.GetComponentData<BusRentalSource>(entity).Connection;
            Entity laneSource=GetBusLaneSource(connection);
            if (!EntityManager.Exists(connection) || EntityManager.HasComponent<Deleted>(connection)
                || laneSource == Entity.Null)
                throw new InvalidOperationException("The rental's road outside connection was removed.");
            if (EntityManager.HasComponent<Game.Objects.Attached>(connection))
            {
                var attached=EntityManager.GetComponentData<Game.Objects.Attached>(connection);
                if (!EntityManager.HasComponent<Game.Objects.Attached>(entity)) EntityManager.AddComponentData(entity,attached);
                else EntityManager.SetComponentData(entity,attached);
            }
            var source = EntityManager.GetBuffer<SubLane>(laneSource, true);
            var target = EntityManager.GetBuffer<SubLane>(entity);
            bool same = source.Length == target.Length;
            for (int i = 0; same && i < source.Length; i++) same = source[i].m_SubLane == target[i].m_SubLane;
            if (!same) { target.Clear(); foreach (var lane in source) target.Add(lane); }
        }

        private Entity FindConnection()
        {
            Entity best = Entity.Null;
            using (var connections = outsideQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity entity in connections)
                {
                    Entity prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                    if (!IsRailway(prefab) || EntityManager.HasComponent<Depot>(entity)) continue;
                    // Shared vanilla prefab data must not change an existing depot's behavior.
                    if (EntityManager.HasComponent<TransportDepotData>(prefab) && !injectedPrefabs.Contains(prefab)) continue;
                    if (best == Entity.Null || entity.Index < best.Index) best = entity;
                }
            }
            return best;
        }

        private bool IsRailway(Entity prefab) => EntityManager.HasComponent<OutsideConnectionData>(prefab)
            && (EntityManager.GetComponentData<OutsideConnectionData>(prefab).m_Type & OutsideConnectionTransferType.Train) != 0;

        private void ValidatePrefab(Entity entity)
        {
            Entity prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            if (!IsRailway(prefab)) throw new InvalidOperationException("Saved rental no longer points to a railway outside connection.");
            if (EntityManager.HasComponent<TransportDepotData>(prefab) && !injectedPrefabs.Contains(prefab))
                throw new InvalidOperationException("Outside-connection prefab already has depot data; refusing to overwrite it.");
        }

        private TransportDepotData GetTrainTemplate()
        {
            using (var candidates = depotPrefabQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity prefab in candidates)
                {
                    var data = EntityManager.GetComponentData<TransportDepotData>(prefab);
                    if (data.m_TransportType == TransportType.Train && !EntityManager.HasComponent<OutsideConnectionData>(prefab))
                    {
                        data.m_ProductionDuration = 0; // Spawn on demand, never manufacture vehicles.
                        data.m_MaintenanceDuration = 0; // Returning trains leave the map for servicing.
                        data.m_DispatchCenter = false;
                        return data;
                    }
                }
            }
            throw new InvalidOperationException("No railway depot prefab is loaded.");
        }

        private void EnsureDepot(Entity entity, RentalContract contract)
        {
            int effectiveCapacity=RentalMaintenancePolicy.DepotCapacity(contract.Capacity,PrepareMaintenanceReturns(entity));
            Entity prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            if (IsBus(entity))
            {
                RefreshBusLanes(entity);
                var busData = EntityManager.GetComponentData<TransportDepotData>(prefab);
                if (busData.m_VehicleCapacity != effectiveCapacity)
                { busData.m_VehicleCapacity = effectiveCapacity; EntityManager.SetComponentData(prefab, busData); }
                if (!EntityManager.HasComponent<Depot>(entity)) EntityManager.AddComponentData(entity, default(Depot));
                return;
            }
            ValidatePrefab(entity);
            var data = injectedPrefabs.Contains(prefab)
                ? EntityManager.GetComponentData<TransportDepotData>(prefab) : GetTrainTemplate();
            data.m_VehicleCapacity = effectiveCapacity;
            if (!injectedPrefabs.Contains(prefab))
            {
                EntityManager.AddComponentData(prefab, data);
                injectedPrefabs.Add(prefab);
                Mod.log.Info($"Installed virtual train depot at {entity}; source prefab {Name(prefab)}.");
            }
            else if (EntityManager.GetComponentData<TransportDepotData>(prefab).m_VehicleCapacity != data.m_VehicleCapacity)
                EntityManager.SetComponentData(prefab, data);
            if (!EntityManager.HasBuffer<ServiceDispatch>(entity)) EntityManager.AddBuffer<ServiceDispatch>(entity);
            if (!EntityManager.HasBuffer<OwnedVehicle>(entity)) EntityManager.AddBuffer<OwnedVehicle>(entity);
            if (!EntityManager.HasComponent<Depot>(entity)) EntityManager.AddComponentData(entity, default(Depot));
        }

        private bool IsMaintenanceReturn(Entity vehicle)
        {
            if (!EntityManager.Exists(vehicle) || EntityManager.HasComponent<Deleted>(vehicle)) return false;
            bool parked=EntityManager.HasComponent<ParkedCar>(vehicle)||EntityManager.HasComponent<ParkedTrain>(vehicle);
            if (EntityManager.HasComponent<Game.Vehicles.PublicTransport>(vehicle))
            {
                var flags=EntityManager.GetComponentData<Game.Vehicles.PublicTransport>(vehicle).m_State;
                return RentalMaintenancePolicy.NeedsReplacement((flags&PublicTransportFlags.Returning)!=0,
                    (flags&PublicTransportFlags.RequiresMaintenance)!=0,parked,(flags&PublicTransportFlags.DummyTraffic)!=0);
            }
            if (EntityManager.HasComponent<Game.Vehicles.CargoTransport>(vehicle))
            {
                var flags=EntityManager.GetComponentData<Game.Vehicles.CargoTransport>(vehicle).m_State;
                return RentalMaintenancePolicy.NeedsReplacement((flags&CargoTransportFlags.Returning)!=0,
                    (flags&CargoTransportFlags.RequiresMaintenance)!=0,parked,(flags&CargoTransportFlags.DummyTraffic)!=0);
            }
            return false;
        }

        private int PrepareMaintenanceReturns(Entity entity)
        {
            if (!EntityManager.HasBuffer<OwnedVehicle>(entity)) return 0;
            var vehicles=EntityManager.GetBuffer<OwnedVehicle>(entity);
            int returning=0;
            for (int i=0;i<vehicles.Length;i++)
            {
                if (!IsMaintenanceReturn(vehicles[i].m_Vehicle)) continue;
                // Vanilla enables vehicles in this buffer's order. Put credited
                // maintenance returners first, leaving exactly rented slots for
                // service vehicles even after reducing the contract maximum.
                if (i!=returning)
                {
                    var swap=vehicles[returning];
                    vehicles[returning]=vehicles[i];vehicles[i]=swap;
                }
                returning++;
            }
            return returning;
        }

        private void TickContract(Entity entity)
        {
            var contract = EntityManager.GetComponentData<RentalContract>(entity);
            bool bus = IsBus(entity);
            int limit = Mod.Settings.Limit(bus), price = Mod.Settings.Price(bus);
            if (contract.Capacity > limit || contract.DailyPrice != price)
            {
                Charge(ref contract);
                contract.Capacity = Math.Min(contract.Capacity, limit);
                contract.DailyPrice = price;
            }
            int fleet = CountFleet(entity);
            // Recreate runtime prefab data after loading, including while vehicles return.
            if (contract.Capacity > 0 || fleet > 0)
            {
                EnsureDepot(entity, contract);
                Charge(ref contract);
                EntityManager.SetComponentData(entity, contract);
                // Shared status is reserved for action results and errors.
            }
            else
            {
                Charge(ref contract);
                EntityManager.SetComponentData(entity, contract);
                RemoveEmptyDepot(entity, contract);
                // Keep the cumulative total after the contract ends.
            }
            UpdateView(bus, contract, fleet);
        }

        private void UpdateView(bool bus, RentalContract contract, int fleet)
        {
            var view = bus ? BusView : TrainView;
            if (view.Capacity == contract.Capacity && view.Fleet == fleet && view.Charged == contract.TotalCharged) return;
            view.Capacity = contract.Capacity; view.Fleet = fleet; view.Charged = contract.TotalCharged; Revision++;
        }

        private int CountFleet(Entity entity)
        {
            if (!EntityManager.HasBuffer<OwnedVehicle>(entity)) return 0;
            int count = 0;
            var vehicles = EntityManager.GetBuffer<OwnedVehicle>(entity, true);
            foreach (var owned in vehicles)
            {
                Entity vehicle = owned.m_Vehicle;
                if (!EntityManager.Exists(vehicle) || EntityManager.HasComponent<Deleted>(vehicle)) continue;
                if (EntityManager.HasComponent<Game.Vehicles.PublicTransport>(vehicle)
                    && (EntityManager.GetComponentData<Game.Vehicles.PublicTransport>(vehicle).m_State & PublicTransportFlags.DummyTraffic) == 0) count++;
                else if (EntityManager.HasComponent<Game.Vehicles.CargoTransport>(vehicle)
                    && (EntityManager.GetComponentData<Game.Vehicles.CargoTransport>(vehicle).m_State & CargoTransportFlags.DummyTraffic) == 0) count++;
            }
            return count;
        }

        private void Charge(ref RentalContract contract)
        {
            uint frame = simulation.frameIndex;
            uint elapsed = unchecked(frame - contract.LastFrame);
            contract.LastFrame = frame;
            int amount = RentalAccounting.Accrue(contract.Capacity, contract.DailyPrice, elapsed, ref contract.FractionalCharge);
            if (amount <= 0) return;
            var money = moneyQuery.GetSingleton<PlayerMoney>();
            money.Subtract(amount);
            moneyQuery.SetSingleton(money);
            contract.TotalCharged += amount;
        }

        private void RemoveEmptyDepot(Entity entity, RentalContract contract)
        {
            if (EntityManager.HasComponent<Depot>(entity)) EntityManager.RemoveComponent<Depot>(entity);
            if (contract.AddedDispatchBuffer && EntityManager.HasBuffer<ServiceDispatch>(entity))
                EntityManager.RemoveComponent<ServiceDispatch>(entity);
            if (contract.AddedOwnedBuffer && EntityManager.HasBuffer<OwnedVehicle>(entity)
                && EntityManager.GetBuffer<OwnedVehicle>(entity).Length == 0)
                EntityManager.RemoveComponent<OwnedVehicle>(entity);
            Entity prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            if (IsBus(entity))
            {
                var data = EntityManager.GetComponentData<TransportDepotData>(prefab);
                if (data.m_VehicleCapacity != 0) { data.m_VehicleCapacity = 0; EntityManager.SetComponentData(prefab, data); }
                return;
            }
            if (injectedPrefabs.Remove(prefab) && EntityManager.Exists(prefab))
                EntityManager.RemoveComponent<TransportDepotData>(prefab);
        }

        private void UnlockDepotBuildRequirements(bool bus)
        {
            var requirements = new HashSet<Entity>();
            using (var depotPrefabs = depotPrefabQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity prefab in depotPrefabs)
                {
                    if (EntityManager.GetComponentData<TransportDepotData>(prefab).m_TransportType != (bus ? TransportType.Bus : TransportType.Train)
                        || !EntityManager.HasBuffer<UnlockOnBuildData>(prefab)) continue;
                    foreach (var entry in EntityManager.GetBuffer<UnlockOnBuildData>(prefab, true))
                        if (EntityManager.HasComponent<ObjectBuiltRequirementData>(entry.m_Entity)) requirements.Add(entry.m_Entity);
                }
            }
            foreach (Entity requirement in requirements)
            {
                if (!EntityManager.HasComponent<Locked>(requirement) || !EntityManager.IsComponentEnabled<Locked>(requirement)) continue;
                int count = EntityManager.GetComponentData<ObjectBuiltRequirementData>(requirement).m_MinimumCount;
                EntityManager.SetComponentData(requirement, new UnlockRequirementData { m_Progress = count });
                Entity unlock = EntityManager.CreateEntity(typeof(Game.Common.Event), typeof(Unlock));
                EntityManager.SetComponentData(unlock, new Unlock(requirement));
                Mod.log.Info($"Requested depot-build requirement unlock: {Name(requirement)}.");
            }
        }

        private void RestorePrefabData()
        {
            foreach (Entity prefab in injectedPrefabs)
                if (EntityManager.Exists(prefab) && EntityManager.HasComponent<TransportDepotData>(prefab))
                    EntityManager.RemoveComponent<TransportDepotData>(prefab);
            injectedPrefabs.Clear();
        }

        private string Name(Entity entity)
        {
            try { return prefabs.GetPrefabName(entity); }
            catch { return entity.ToString(); }
        }

        private void WriteDiagnostics()
        {
            Mod.log.Info("=== Rent a Train prototype diagnostic report ===");
            Mod.log.Info($"Frame={simulation.frameIndex}; status={Status}");
            using (var connections = outsideQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity entity in connections)
                {
                    Entity prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                    if (EntityManager.HasComponent<OutsideConnectionData>(prefab))
                    {
                        var transfer=EntityManager.GetComponentData<OutsideConnectionData>(prefab).m_Type;
                        if ((transfer & OutsideConnectionTransferType.Road) != 0)
                        {
                            Entity owner=EntityManager.HasComponent<Owner>(entity)?EntityManager.GetComponentData<Owner>(entity).m_Owner:Entity.Null;
                            Entity attached=EntityManager.HasComponent<Game.Objects.Attached>(entity)?EntityManager.GetComponentData<Game.Objects.Attached>(entity).m_Parent:Entity.Null;
                            Mod.log.Info($"Road outside={entity}; prefab={Name(prefab)}; owner={owner}; attached={attached}; laneSource={GetBusLaneSource(entity)}");
                        }
                    }
                    if (!IsRailway(prefab)) continue;
                    int pending = EntityManager.HasBuffer<ServiceDispatch>(entity) ? EntityManager.GetBuffer<ServiceDispatch>(entity).Length : 0;
                    Mod.log.Info($"Rail outside={entity}; prefab={Name(prefab)}; prefabDepotData={EntityManager.HasComponent<TransportDepotData>(prefab)}; position={EntityManager.GetComponentData<Game.Objects.Transform>(entity).m_Position}; depot={EntityManager.HasComponent<Depot>(entity)}; fleet={CountFleet(entity)}; dispatches={pending}; contract={EntityManager.HasComponent<RentalContract>(entity)}");
                    if (EntityManager.HasComponent<Depot>(entity))
                    {
                        var depot = EntityManager.GetComponentData<Depot>(entity);
                        Mod.log.Info($"Depot flags={depot.m_Flags}; available={depot.m_AvailableVehicles}; request={depot.m_TargetRequest}");
                    }
                }
            }
            using (var contracts = contractQuery.ToEntityArray(Allocator.Temp))
                foreach (Entity entity in contracts)
                {
                    var contract = EntityManager.GetComponentData<RentalContract>(entity);
                    Mod.log.Info($"Rental={(IsBus(entity) ? "Bus" : "Train")}; entity={entity}; rented={contract.Capacity}; fleet={CountFleet(entity)}; price={contract.DailyPrice}; total={contract.TotalCharged}; maintenanceReturns={PrepareMaintenanceReturns(entity)}");
                }
            Mod.log.Info("=== End report ===");
        }

        private static void Fail(string message, Exception ex)
        {
            Status = message + ": " + ex.Message; Revision++;
            Mod.log.Error(ex, message);
        }
    }
}






