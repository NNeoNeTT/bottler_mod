using DubsBadHygiene;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DubsWaterPatch
{
    public class JobDriver_BottleWaterManual : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Obtener la mesa (banco de trabajo)
            Thing bench = job.GetTarget(TargetIndex.A).Thing;

            // Verificar si tiene agua conectada (usando CompWaterNet del mod Dubs Bad Hygiene)
            var comp = bench.TryGetComp<CompWaterNet>();
            if (comp == null || !comp.WaterOn)
            {
                EndJobWith(JobCondition.Incompletable);
                yield break;
            }

            // Reservar la mesa
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            // Ir a la mesa
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // Acción de embotellado
            yield return Toils_General.Do(delegate
            {
                Thing bottledWater = ThingMaker.MakeThing(ThingDef.Named("BottledWater"));
                GenPlace.TryPlaceThing(bottledWater, pawn.Position, Map, ThingPlaceMode.Near);
            });

            // Simula tiempo de trabajo con barra de progreso
            yield return Toils_General.Wait(60).WithProgressBarToilDelay(TargetIndex.A);
        }
    }

    public class WorkGiver_BottleWater : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerThings.ThingsOfDef(ThingDef.Named("WaterBottlerBench"));
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction) return false;
            if (!pawn.CanReserve(t)) return false;
            if (t.IsBurning()) return false;

            var comp = t.TryGetComp<CompWaterNet>();
            return comp != null && comp.WaterOn;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("BottleWaterManual"), t);
        }
    }
}
