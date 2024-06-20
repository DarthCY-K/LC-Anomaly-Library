﻿using LCAnomalyLibrary.Defs;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace LCAnomalyLibrary.Toils
{
    public class ExtractRitualToil_GatherForInvocation : PsychicRitualToil_Multiplex
    {
        protected PsychicRitualToil_Goto fallbackToil;

        protected PsychicRitualGraph invokerToil;

        protected PsychicRitualToil_Goto invokerFinalToil;

        private static List<Pawn> blockingPawns = new List<Pawn>(16);

        protected ExtractRitualToil_GatherForInvocation()
        {
        }

        protected ExtractRitualToil_GatherForInvocation(ExtractRitualDef_InvocationCircle def, PsychicRitualToil_Goto fallbackToil, PsychicRitualGraph invokerToil)
            : base(new Dictionary<PsychicRitualRoleDef, PsychicRitualToil> { { def.InvokerRole, invokerToil } }, fallbackToil)
        {
            this.fallbackToil = fallbackToil;
            this.invokerToil = invokerToil;
            invokerFinalToil = (PsychicRitualToil_Goto)invokerToil.GetToil(invokerToil.ToilCount - 1);
        }

        public ExtractRitualToil_GatherForInvocation(PsychicRitual psychicRitual, ExtractRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions)
            : this(def, FallbackToil(psychicRitual, def, rolePositions), InvokerToil(def, rolePositions))
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref fallbackToil, "fallbackToil");
            Scribe_References.Look(ref invokerToil, "invokerToil");
            Scribe_References.Look(ref invokerFinalToil, "invokerFinalToil");
        }

        public override string GetReport(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            blockingPawns.Clear();
            blockingPawns.AddRange(fallbackToil.BlockingPawns);
            if (invokerToil.CurrentToil == invokerFinalToil)
            {
                blockingPawns.AddRange(invokerFinalToil.BlockingPawns);
            }
            else
            {
                blockingPawns.AddRange(invokerFinalToil.ControlledPawns(psychicRitual));
            }

            string text = "PsychicRitualToil_GatherForInvocation_Report".Translate();
            string text2 = blockingPawns.Select((Pawn pawn) => pawn.LabelShortCap).ToCommaList();
            return text + ": " + text2;
        }

        public static PsychicRitualToil_Goto FallbackToil(PsychicRitual psychicRitual, ExtractRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions)
        {
            return new PsychicRitualToil_Goto(rolePositions.Slice(rolePositions.Keys.Except(def.InvokerRole)));
        }

        public static PsychicRitualGraph InvokerToil(ExtractRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions)
        {
            return new PsychicRitualGraph(InvokerGatherPhaseToils(def, rolePositions))
            {
                willAdvancePastLastToil = false
            };
        }

        public static IEnumerable<PsychicRitualToil> InvokerGatherPhaseToils(ExtractRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions)
        {
            if (def.RequiredOffering != null)
            {
                Log.Warning("1");
                yield return new ExtractRitualToil_GatherOfferings(def.InvokerRole, def.RequiredOffering);
            }

            if (def.TargetRole != null)
            {
                Log.Warning("2");
                yield return new PsychicRitualToil_CarryAndGoto(def.InvokerRole, def.TargetRole, rolePositions);
                yield break;
            }

            Log.Warning("3");
            yield return new PsychicRitualToil_Goto(rolePositions.Slice(def.InvokerRole));
        }
    }
}