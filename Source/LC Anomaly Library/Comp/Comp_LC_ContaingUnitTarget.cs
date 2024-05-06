﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.AI.Group;
using Verse.AI;
using Verse;
using LCAnomalyLibrary.Building;
using LCAnomalyLibrary.Utility;

namespace LCAnomalyLibrary.Comp
{
    [StaticConstructorOnStartup]
    public class Comp_LC_ContainingUnitTarget : ThingComp
    {
        private const int CheckInitiateEscapeIntervalTicks = 2500;

        private static readonly SimpleCurve JoinEscapeChanceFromEscapeIntervalCurve = new SimpleCurve
        {
            new CurvePoint(120f, 0.33f),
            new CurvePoint(60f, 0.5f),
            new CurvePoint(10f, 0.9f)
        };

        private static readonly CachedTexture CaptureIcon = new CachedTexture("UI/Commands/CaptureEntity");

        private static readonly CachedTexture TransferIcon = new CachedTexture("UI/Commands/TransferEntity");

        private static readonly Texture2D CancelTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public Thing targetHolder;

        public bool isEscaping;

        public EntityContainmentMode containmentMode;

        public bool extractBioferrite;

        [Unsaved(false)]
        private Comp_LC_Studiable compStudiable;

        public CompProperties_LC_ContainingUnitTarget Props => (CompProperties_LC_ContainingUnitTarget)props;

        public Comp_LC_Studiable Comp_LC_Studiable => compStudiable ?? (compStudiable = parent.GetComp<Comp_LC_Studiable>());

        public Comp_LC_EntityContainer EntityHolder => targetHolder.TryGetComp<Comp_LC_EntityContainer>();

        public bool StudiedAtContainingUnit
        {
            get
            {
                if (!EverStudiable)
                {
                    return false;
                }

                if (parent is Pawn pawn)
                {
                    if (pawn.IsNonMutantAnimal && !pawn.RaceProps.IsAnomalyEntity)
                    {
                        return false;
                    }

                    if (!pawn.IsMutant)
                    {
                        if (!pawn.RaceProps.Humanlike && !pawn.Inhumanized())
                        {
                            return !pawn.kindDef.studiableAsPrisoner;
                        }

                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        public bool CanStudy
        {
            get
            {
                if (containmentMode == EntityContainmentMode.Study)
                {
                    return EverStudiable;
                }

                return false;
            }
        }

        private bool EverStudiable
        {
            get
            {
                if (parent.Destroyed)
                {
                    return false;
                }

                if (parent is Pawn)
                {
                    if (Comp_LC_Studiable != null)
                    {
                        return Comp_LC_Studiable.AnomalyKnowledge > 0f;
                    }

                    return false;
                }

                return true;
            }
        }

        public Building_LC_ContainingUnit HeldPlatform => parent.ParentHolder as Building_LC_ContainingUnit;

        public bool CurrentlyHeldOnPlatform
        {
            get
            {
                if (HeldPlatform != null)
                {
                    return parent.SpawnedOrAnyParentSpawned;
                }

                return false;
            }
        }

        public bool CanBeCaptured
        {
            get
            {
                if (!(parent is Pawn pawn))
                {
                    return true;
                }

                if (pawn.Faction == Faction.OfPlayer)
                {
                    return false;
                }

                if (pawn.IsMutant && !pawn.mutant.Def.canBeCaptured)
                {
                    return false;
                }

                Comp_LC_Studiable compStudiable = pawn.TryGetComp<Comp_LC_Studiable>();

                if (compStudiable != null && Find.Anomaly.Level < compStudiable.Props.minMonolithLevelForStudy && Find.Anomaly.GenerateMonolith)
                {
                    return false;
                }

                if (!pawn.Downed)
                {
                    return pawn.GetComp<CompActivity>()?.IsDormant ?? false;
                }

                return true;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (targetHolder != null && parent.Map != targetHolder.Map)
            {
                targetHolder = null;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent is Pawn pawn)
            {
                if (CurrentlyHeldOnPlatform)
                {
                    CaptivityTick(pawn);
                }
                else if (targetHolder != null && (targetHolder.Destroyed || EntityHolder.HeldPawn != null))
                {
                    targetHolder = null;
                }

                if (isEscaping && pawn.mindState.enemyTarget == null)
                {
                    isEscaping = false;
                }
            }

            Building_LC_ContainingUnit heldPlatform = HeldPlatform;
            if (heldPlatform != null && heldPlatform.HasAttachedBioferriteHarvester)
            {
                extractBioferrite = false;
            }

            Log.Message("111");
            if (CanBeCaptured)
            {
                LessonAutoActivator.TeachOpportunity(ConceptDefOf.CapturingEntities, OpportunityType.Important);
            }
        }

        private void CaptivityTick(Pawn pawn)
        {
            pawn.mindState.entityTicksInCaptivity++;
            if (targetHolder is Building_LC_ContainingUnit Building_LC_ContainingUnit && Building_LC_ContainingUnit != HeldPlatform && Building_LC_ContainingUnit.Occupied)
            {
                targetHolder = null;
            }

            if (parent.IsHashIntervalTick(2500))
            {
                float num = LC_ContainmentUtility.InitiateEscapeMtbDays(pawn);
                if (num >= 0f && Rand.MTBEventOccurs(num, 60000f, 2500f))
                {
                    Escape(initiator: true);
                }
            }
        }

        public void Notify_HeldOnPlatform(ThingOwner newOwner)
        {
            targetHolder = null;
            Pawn pawn = null;
            if (parent is Pawn pawn2)
            {
                pawn2.mindState.lastAssignedInteractTime = Find.TickManager.TicksGame;
                PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn2);
                pawn = pawn2;
            }

            if (newOwner != null)
            {
                if (Props.heldPawnKind != null)
                {
                    Pawn pawn3 = PawnGenerator.GeneratePawn(new PawnGenerationRequest(Props.heldPawnKind, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, 0f));
                    newOwner.TryAdd(pawn3);
                    pawn3.TryGetComp<Comp_LC_ContainingUnitTarget>()?.Notify_HeldOnPlatform(newOwner);
                    pawn = pawn3;
                    //if (Props.heldPawnKind == Defs.LCEggedEntityDef)
                    //{
                    //    CompBiosignatureOwner compBiosignatureOwner = parent.TryGetComp<CompBiosignatureOwner>();
                    //    if (compBiosignatureOwner != null)
                    //    {
                    //        //pawn3.TryGetComp<CompRevenant>().biosignature = compBiosignatureOwner.biosignature;
                    //    }

                    //    if (pawn3.TryGetComp<Comp_LC_Studiable>(out var comp))
                    //    {
                    //        comp.lastStudiedTick = Find.TickManager.TicksGame;
                    //    }
                    //}

                    Find.HiddenItemsManager.SetDiscovered(pawn3.def);
                    parent.Destroy();
                }

                containmentMode = EntityContainmentMode.Study;
            }

            if (pawn != null && HeldPlatform != null)
            {
                pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.MadePrisoner);
                pawn.TryGetComp<CompActivity>()?.Notify_HeldOnPlatform();
                Find.StudyManager.UpdateStudiableCache(HeldPlatform, HeldPlatform.Map);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.CapturingEntities, KnowledgeAmount.Total);
                LessonAutoActivator.TeachOpportunity(ConceptDefOf.ContainingEntities, OpportunityType.Important);
            }
        }

        public void Notify_ReleasedFromPlatform()
        {
            Find.StudyManager.UpdateStudiableCache(HeldPlatform, HeldPlatform.Map);
        }

        public void Escape(bool initiator)
        {
            List<Pawn> list = new List<Pawn>();
            List<Verse.Building> list2 = new List<Verse.Building> { HeldPlatform };
            HeldPlatform.EjectContents();
            if (!(parent is Pawn pawn))
            {
                return;
            }

            pawn.health.overrideDeathOnDownedChance = 0f;
            list.Add(pawn);
            isEscaping = true;
            if (Props.lookForTargetOnEscape && !pawn.Downed)
            {
                Pawn enemyTarget = (Pawn)AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, (Thing x) => x is Pawn && (int)x.def.race.intelligence >= 1, 0f, 9999f, default(IntVec3), float.MaxValue, canBashDoors: true, canTakeTargetsCloserThanEffectiveMinRange: true, canBashFences: true);
                pawn.mindState.enemyTarget = enemyTarget;
            }

            //CompRevenant compRevenant = pawn.TryGetComp<CompRevenant>();
            //if (compRevenant != null)
            //{
            //    compRevenant.revenantState = RevenantState.Escape;
            //}

            pawn.GetInvisibilityComp()?.BecomeVisible(instant: true);
            if (!initiator)
            {
                return;
            }

            Room room = pawn.GetRoom();
            if (room == null)
            {
                return;
            }

            foreach (Building_LC_ContainingUnit item in room.ContainedAndAdjacentThings.Where((Thing x) => x is Building_LC_ContainingUnit).ToList())
            {
                Pawn heldPawn = item.HeldPawn;
                if (heldPawn == null || heldPawn == pawn)
                {
                    continue;
                }

                Comp_LC_ContainingUnitTarget Comp_LC_ContainingUnitTarget = heldPawn.TryGetComp<Comp_LC_ContainingUnitTarget>();
                if (Comp_LC_ContainingUnitTarget != null && Comp_LC_ContainingUnitTarget.CurrentlyHeldOnPlatform)
                {
                    float num = LC_ContainmentUtility.InitiateEscapeMtbDays(heldPawn);
                    if (!(num <= 0f) && Rand.Chance(JoinEscapeChanceFromEscapeIntervalCurve.Evaluate(num)))
                    {
                        list.Add(heldPawn);
                        list2.Add(Comp_LC_ContainingUnitTarget.HeldPlatform);
                        Comp_LC_ContainingUnitTarget.Escape(initiator: false);
                    }
                }
            }

            Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("LetterLabelEscapingFromHoldingPlatform".Translate(), "LetterEscapingFromHoldingPlatform".Translate(list.Select((Pawn p) => p.LabelCap).ToLineList("  - ")), LetterDefOf.ThreatBig, list2));
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            //Log.Warning($"{StudiedAtContainingUnit && !CurrentlyHeldOnPlatform && CanBeCaptured}");
            if (StudiedAtContainingUnit && !CurrentlyHeldOnPlatform && CanBeCaptured)
            {

                if (targetHolder != null)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "CancelCapture".Translate(),
                        defaultDesc = "CancelCaptureDesc".Translate(parent).Resolve(),
                        icon = CancelTex,
                        action = delegate
                        {
                            targetHolder = null;
                        }
                    };
                }
                else if (parent.Spawned)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "CaptureEntity".Translate() + "...",
                        defaultDesc = "CaptureEntityDesc".Translate(parent).Resolve(),
                        icon = CaptureIcon.Texture,
                        action = delegate
                        {
                            LC_StudyUtility.TargetHoldingPlatformForEntity(null, parent);
                        },
                        activateSound = SoundDefOf.Click,
                        Disabled = !LC_StudyUtility.HoldingPlatformAvailableOnCurrentMap(),
                        disabledReason = "NoHoldingPlatformsAvailable".Translate()
                    };
                }
            }

            if (CurrentlyHeldOnPlatform)
            {
                if (targetHolder != null && targetHolder != HeldPlatform)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "CancelTransfer".Translate(),
                        defaultDesc = "CancelTransferDesc".Translate(),
                        icon = CancelTex,
                        action = delegate
                        {
                            targetHolder = null;
                        }
                    };
                }
                else
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "TransferEntity".Translate(parent) + "...",
                        defaultDesc = "TransferEntityDesc".Translate(parent).Resolve(),
                        icon = TransferIcon.Texture,
                        action = delegate
                        {
                            LC_StudyUtility.TargetHoldingPlatformForEntity(null, parent, transferBetweenPlatforms: true, HeldPlatform);
                        },
                        activateSound = SoundDefOf.Click
                    };
                }
            }

            if (DebugSettings.ShowDevGizmos && CurrentlyHeldOnPlatform)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Escape",
                    action = delegate
                    {
                        Escape(initiator: true);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Kill",
                    action = delegate
                    {
                        parent.Kill();
                    }
                };
            }
        }

        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            Log.Warning($"CanBeCaptured = {CanBeCaptured}");
            if (CanBeCaptured && LC_ContainmentUtility.ShowContainmentStats(parent))
            {
                float num = parent.GetStatValue(StatDefOf.MinimumContainmentStrength);
                if (Props.heldPawnKind != null)
                {
                    num = Props.heldPawnKind.race.GetStatValueAbstract(StatDefOf.MinimumContainmentStrength);
                }

                if (!text.NullOrEmpty())
                {
                    text += "\n";
                }

                text += "Capturable".Translate() + ". " + StatDefOf.MinimumContainmentStrength.LabelCap + ": " + num.ToString("F1");
            }

            return text;
        }

        public override void PostExposeData()
        {
            Scribe_References.Look(ref targetHolder, "targetHolder");
            Scribe_Values.Look(ref isEscaping, "isEscaping", defaultValue: false);
            Scribe_Values.Look(ref containmentMode, "containmentMode", EntityContainmentMode.MaintainOnly);
            Scribe_Values.Look(ref extractBioferrite, "extractBioferrite", defaultValue: false);
        }
    }
}