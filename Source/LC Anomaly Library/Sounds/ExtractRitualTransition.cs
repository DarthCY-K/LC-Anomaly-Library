﻿using RimWorld;
using System.Linq;
using Verse.AI.Group;
using Verse;
using KTrie;
using LCAnomalyLibrary.Jobs;

namespace LCAnomalyLibrary.Sounds
{
    public class ExtractRitualTransition : MusicTransition
    {
        public override bool IsTransitionSatisfied()
        {
            if (!ModsConfig.AnomalyActive)
            {
                return false;
            }

            if (!base.IsTransitionSatisfied())
            {
                return false;
            }

            foreach (Map map in Find.Maps)
            {
                if (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.UnnaturalDarkness) || map.gameConditionManager.ConditionIsActive(GameConditionDefOf.DeathPall) || map.gameConditionManager.ConditionIsActive(GameConditionDefOf.BloodRain))
                {
                    return true;
                }
            }

            foreach (Map map2 in Find.Maps)
            {
                if (map2.listerThings.ThingsInGroup(ThingRequestGroup.BuildingGroundSpawner).Any())
                {
                    return true;
                }

                foreach (Pawn item in map2.mapPawns.AllPawnsSpawned)
                {
                    if (IsValidPawn(item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsValidPawn(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            if (lord != null)
            {
                if (lord.LordJob is LordJob_ExtractRitual)
                {
                    Log.Warning("播放提取bgm");
                    return true;
                }
            }

            return false;
        }
    }
}