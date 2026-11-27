using Colossal.UI.Binding;
using Game.UI;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Pathfind;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace Rent_A_Train
{
    public partial class RentalUISystem : UISystemBase
    {
        private RawValueBinding state;
        private int revision = -1, settingsHash;
        private string lastStatus;
        private bool panelOpen, panelVisible;
        private int summaryCountdown, busNeeded, trainNeeded, busCapacity, trainCapacity;
        private EntityQuery lines, depots;
        protected override void OnCreate()
        {
            base.OnCreate();
            lines = GetEntityQuery(ComponentType.ReadOnly<TransportLine>(), ComponentType.ReadOnly<Route>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            depots = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportDepot>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>());
            AddBinding(new TriggerBinding<bool>("RentATrain", "setPanelOpen", open => { panelOpen = open; summaryCountdown = 0; }));
            AddBinding(new TriggerBinding<bool>("RentATrain", "setPanelVisible", visible =>
            { panelVisible=visible; panelOpen=visible; summaryCountdown=0; revision=-1; }));
            AddBinding(state = new RawValueBinding("RentATrain", "state", WriteState));
            AddBinding(new TriggerBinding<int, int>("RentATrain", "setRental", RentalDepotSystem.RequestRental));
            AddBinding(new TriggerBinding<int, int>("RentATrain", "setRentals", (buses,trains) =>
            {
                if (buses != RentalDepotSystem.BusView.Capacity) RentalDepotSystem.RequestRental(0,buses);
                if (trains != RentalDepotSystem.TrainView.Capacity) RentalDepotSystem.RequestRental(1,trains);
            }));
        }
        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;
            if (!RentalDepotSystem.CityReady) panelVisible=panelOpen=false;
            bool summaryChanged = false;
            if (panelOpen && --summaryCountdown <= 0)
            {
                summaryCountdown = 30;
                try { summaryChanged = UpdateSummary(); }
                catch (System.Exception ex) { Mod.log.Error(ex, "Cannot refresh fleet overview"); }
            }
            var s = Mod.Settings;
            int hash = ((s.BusMaximum * 101 + s.TrainMaximum) * 10001 + s.BusDailyPrice) * 10001 + s.TrainDailyPrice;
            hash = unchecked((hash * 3 + (s.UseUniversalModMenu ? 1 : 0)) * 3 + (int)s.InitialWindowPosition);
            if (!summaryChanged && revision == RentalDepotSystem.Revision && hash == settingsHash && lastStatus == RentalDepotSystem.Status) return;
            revision = RentalDepotSystem.Revision; settingsHash = hash; lastStatus = RentalDepotSystem.Status;
            state.Update();
        }
        private bool UpdateSummary()
        {
            int bn=0, tn=0, bc=0, tc=0;
            if (RentalDepotSystem.CityReady)
            {
                using (var entities = lines.ToEntityArray(Allocator.Temp))
                foreach (Entity entity in entities)
                {
                    var route=EntityManager.GetComponentData<Route>(entity);
                    if (RouteUtils.CheckOption(route, RouteOption.Inactive)) continue;
                    Entity prefab=EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                    if (!EntityManager.HasComponent<TransportLineData>(prefab)) continue;
                    var data=EntityManager.GetComponentData<TransportLineData>(prefab);
                    if (data.m_TransportType != TransportType.Bus && data.m_TransportType != TransportType.Train) continue;
                    float interval=data.m_DefaultVehicleInterval, duration=0;
                    if (EntityManager.HasBuffer<RouteModifier>(entity))
                        RouteUtils.ApplyModifier(ref interval, EntityManager.GetBuffer<RouteModifier>(entity,true), RouteModifierType.VehicleInterval);
                    if (EntityManager.HasBuffer<RouteSegment>(entity))
                        foreach (var segment in EntityManager.GetBuffer<RouteSegment>(entity,true))
                            if (EntityManager.HasComponent<PathInformation>(segment.m_Segment))
                                duration += EntityManager.GetComponentData<PathInformation>(segment.m_Segment).m_Duration;
                    if (EntityManager.HasBuffer<RouteWaypoint>(entity))
                        foreach (var waypoint in EntityManager.GetBuffer<RouteWaypoint>(entity,true))
                            if (EntityManager.HasComponent<VehicleTiming>(waypoint.m_Waypoint)) duration += data.m_StopDuration;
                    int count=TransportLineSystem.CalculateVehicleCount(interval,duration);
                    if (data.m_TransportType==TransportType.Bus) bn+=count; else tn+=count;
                }
                var refs=GetComponentLookup<PrefabRef>(true);
                var depotData=GetComponentLookup<TransportDepotData>(true);
                using (var entities = depots.ToEntityArray(Allocator.Temp))
                foreach (Entity entity in entities)
                {
                    Entity prefab=EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                    if (!EntityManager.HasComponent<TransportDepotData>(prefab)) continue;
                    var data=EntityManager.GetComponentData<TransportDepotData>(prefab);
                    if (data.m_TransportType!=TransportType.Bus && data.m_TransportType!=TransportType.Train) continue;
                    if (EntityManager.HasBuffer<InstalledUpgrade>(entity))
                        UpgradeUtils.CombineStats(ref data,EntityManager.GetBuffer<InstalledUpgrade>(entity,true),ref refs,ref depotData);
                    float efficiency=1;
                    if (EntityManager.HasBuffer<Efficiency>(entity))
                    {
                        var factors=EntityManager.GetBuffer<Efficiency>(entity,true);
                        efficiency=Unity.Mathematics.math.min(BuildingUtils.GetEfficiency(factors),BuildingUtils.GetImmediateEfficiency(factors));
                    }
                    int capacity=BuildingUtils.GetVehicleCapacity(efficiency,data.m_VehicleCapacity);
                    if (data.m_TransportType==TransportType.Bus)bc+=capacity;else tc+=capacity;
                }
            }
            bool changed=bn!=busNeeded||tn!=trainNeeded||bc!=busCapacity||tc!=trainCapacity;
            busNeeded=bn;trainNeeded=tn;busCapacity=bc;trainCapacity=tc;
            return changed;
        }

        private void WriteState(IJsonWriter writer)
        {
            var settings = Mod.Settings;
            writer.TypeBegin("RentATrain.State");
            writer.PropertyName("panelVisible"); writer.Write(panelVisible);
            writer.PropertyName("useUniversalMenu"); writer.Write(settings?.UseUniversalModMenu ?? false);
            writer.PropertyName("windowPosition"); writer.Write((int)(settings?.InitialWindowPosition ?? Setting.PanelPosition.Center));
            writer.PropertyName("ready"); writer.Write(RentalDepotSystem.CityReady);
            writer.PropertyName("busRented"); writer.Write(RentalDepotSystem.BusView.Capacity);
            writer.PropertyName("trainRented"); writer.Write(RentalDepotSystem.TrainView.Capacity);
            writer.PropertyName("busFleet"); writer.Write(RentalDepotSystem.BusView.Fleet);
            writer.PropertyName("trainFleet"); writer.Write(RentalDepotSystem.TrainView.Fleet);
            writer.PropertyName("busCharged"); writer.Write(RentalDepotSystem.BusView.Charged);
            writer.PropertyName("trainCharged"); writer.Write(RentalDepotSystem.TrainView.Charged);
            writer.PropertyName("busMaximum"); writer.Write(settings?.Limit(true) ?? 20);
            writer.PropertyName("trainMaximum"); writer.Write(settings?.Limit(false) ?? 10);
            writer.PropertyName("busPrice"); writer.Write(settings?.Price(true) ?? 1000);
            writer.PropertyName("trainPrice"); writer.Write(settings?.Price(false) ?? 5000);
            writer.PropertyName("busNeeded"); writer.Write(busNeeded);
            writer.PropertyName("trainNeeded"); writer.Write(trainNeeded);
            writer.PropertyName("busDepotCapacity"); writer.Write(busCapacity);
            writer.PropertyName("trainDepotCapacity"); writer.Write(trainCapacity);
            writer.PropertyName("status"); writer.Write(RentalDepotSystem.Status);
            writer.TypeEnd();
        }
    }
}

