using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;

namespace MissingEncounterZonesPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "MissingEncounterZonesPatch.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            ISkyrimModGetter? mezfEntries = state.LoadOrder.TryGetValue(ModKey.FromFileName("MissingEZs_AllExteriors.esp"))?.Mod ?? state.LoadOrder.TryGetValue(ModKey.FromFileName("MissingEZsFixed.esp"))?.Mod ?? null;

            if (mezfEntries is null)
                throw new Exception("Couldn't find either Missing Encounter Zones Fixed plugin. Make sure to install the mod correctly.");

            foreach (var mezfLocation in mezfEntries.Locations)
            {
                if (!mezfLocation.AsLink().TryResolve(state.LinkCache, out var winningRecordGetter)) continue;
                if (mezfLocation.Equals(winningRecordGetter)) continue;

                ILocation winningRecord = state.PatchMod.Locations.GetOrAddAsOverride(winningRecordGetter);
                winningRecord.ActorCellEncounterCell.Clear();
                winningRecord.ActorCellEncounterCell.Concat(mezfLocation.ActorCellEncounterCell);
            }
            
           foreach (var mezfCellBlock in mezfEntries.Cells.Records)
            {
                foreach(var mezfSubCellBlock in mezfCellBlock.SubBlocks)
                {
                    foreach(var mezfCell in mezfSubCellBlock.Cells)
                    {
                        if (!mezfCell.AsLink().TryResolveContext<ISkyrimMod, ISkyrimModGetter, ICell, ICellGetter>(state.LinkCache, out var winningRecordContext)) continue;
                        if (winningRecordContext.Record.Equals(mezfCell)) continue;
                        
                        ICell winningRecord = winningRecordContext.GetOrAddAsOverride(state.PatchMod);
                        if(!mezfCell.EncounterZone.Equals(FormLinkNullableGetter<IEncounterZoneGetter>.Null))
                            winningRecord.EncounterZone = mezfCell.EncounterZone.AsSetter().AsNullable();
                    }
                }
            }

            foreach (var mezfWorldspace in mezfEntries.Worldspaces)
            {
                if(mezfWorldspace.AsLink().TryResolveContext<ISkyrimMod, ISkyrimModGetter, IWorldspace, IWorldspaceGetter>(state.LinkCache, out var winningWorldspaceContext))
                {
                    if(!winningWorldspaceContext.Record.Equals(mezfWorldspace))
                    {
                        IWorldspace winningRecord = winningWorldspaceContext.GetOrAddAsOverride(state.PatchMod);
                        if (!mezfWorldspace.EncounterZone.Equals(FormLinkNullableGetter<IEncounterZoneGetter>.Null))
                            winningRecord.EncounterZone = mezfWorldspace.EncounterZone.AsSetter().AsNullable();
                    }
                }

                foreach(var mezfCellBlock in mezfWorldspace.SubCells)
                {
                    foreach(var mezfSubCellBlock in mezfCellBlock.Items)
                    {
                        foreach(var mezfCell in mezfSubCellBlock.Items)
                        {
                            if (!mezfCell.AsLink().TryResolveContext<ISkyrimMod, ISkyrimModGetter, ICell, ICellGetter>(state.LinkCache, out var winningCellContext)) continue;
                            if (winningCellContext.Record.Equals(mezfCell)) continue;

                            ICell winningRecord = winningCellContext.GetOrAddAsOverride(state.PatchMod);
                            if(!mezfCell.EncounterZone.Equals(FormLinkNullableGetter<IEncounterZoneGetter>.Null))
                                winningRecord.EncounterZone = mezfCell.EncounterZone.AsSetter().AsNullable();
                        }
                    }
                }
            }
        }
    }
}
