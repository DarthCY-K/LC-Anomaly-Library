﻿using System.Collections.Generic;
using Verse;

namespace LCAnomalyLibrary.Comp
{
    /// <summary>
    /// 可获取饰品的Comp
    /// </summary>
    public class LC_CompAccessoryable : ThingComp
    {
        /// <summary>
        /// CompProperties
        /// </summary>
        public LC_CompProperties_Accessoryable Props => (LC_CompProperties_Accessoryable)props;

        #region 工具方法

        /// <summary>
        /// 检查是否给予饰品
        /// </summary>
        /// <param name="studier">研究者</param>
        public void CheckGiveAccessory(Pawn studier)
        {
            //概率排前面是为了减少计算量，避免下面的foreach每次都要触发
            if (Rand.Chance(Props.accessoryChance))
            {
                SpawnAccessory(studier);
                Log.Message($"工作：{studier.Name} 获取饰品成功");
            }
            else
            {
                Log.Message($"工作：{studier.Name} 获取饰品失败，概率判定失败");
            }
        }

        /// <summary>
        /// 生成饰品
        /// </summary>
        private void SpawnAccessory(Pawn studier)
        {
            if(Props.accessoryDef != null)
            {
                Thing thing = ThingMaker.MakeThing(Props.accessoryDef);
                thing.stackCount = 1;

                //研究者不为空则生成在研究者附近，否则实体附近
                var target = studier ?? parent;
                GenPlace.TryPlaceThing(thing, target.PositionHeld, target.MapHeld, ThingPlaceMode.Near);
            }
        }

        #endregion

        #region UI

        /// <summary>
        /// Gizmos
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Verse.Gizmo> CompGetGizmosExtra()
        {
            foreach (Verse.Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Spawn Accessory",
                    action = delegate
                    {
                        Log.Warning($"Dev：产生E.G.O饰品{Props.accessoryDef.label.Translate()}");
                        SpawnAccessory(null);
                    }
                };
            }
        }

        #endregion UI
    }
}