﻿using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using LCAnomalyLibrary.Comp;
using System.Text;

namespace LCAnomalyLibrary.Building
{
    public class Building_QliphothIndicator : Verse.Building
    {
        private bool initalized;

        protected int qliphothCounter;
        public int QliphothCounter
        {
            get => qliphothCounter;
            set
            {
                if(value ==  qliphothCounter) 
                    return;

                if(value < 0) 
                { 
                    qliphothCounter = 0;
                }
                else
                {
                    qliphothCounter = value;
                    Log.Message($"逆卡巴拉计数器设备值变更为：{qliphothCounter}");
                }
            }
        }

        [Unsaved(false)]
        protected List<Graphic> cachedTopGraphic;
        private List<Graphic> TopGraphic
        {
            get
            {
                if (this.cachedTopGraphic == null)
                {
                    cachedTopGraphic = new List<Graphic>();
                    for (int i = 0; i < 6; i++)
                    {
                        Log.Warning("Things/Building/QliphothIndicator/Top" + i);
                        cachedTopGraphic.Add(GraphicDatabase.Get<Graphic_Single>("Things/Building/QliphothIndicator/Top" + i, ShaderDatabase.Transparent, this.def.graphicData.drawSize, Color.white));
                    }
                }
                return this.cachedTopGraphic;
            }
        }

        private CompFacility facilityComp;

        private CompPowerTrader powerComp;

        public CompFacility FacilityComp => facilityComp ?? (facilityComp = GetComp<CompFacility>());

        public CompPowerTrader Power => powerComp ?? (powerComp = GetComp<CompPowerTrader>());

        public List<Thing> Platforms => FacilityComp.LinkedBuildings;


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Initialize();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);

            initalized = false;
        }

        private void Initialize()
        {
            if (initalized)
            {
                return;
            }

            initalized = true;

            foreach (Thing platform in Platforms)
            {
                if (platform is Building_HoldingPlatform building_HoldingPlatform)
                {
                    building_HoldingPlatform.innerContainer.OnContentsChanged += UpdateQliphothCounter;
                }
            }

            UpdateQliphothCounter();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (!initalized)
            {
                Initialize();
            }

            this.TopGraphic[qliphothCounter].Draw(this.DrawPos + Altitudes.AltIncVect * 2f, base.Rotation, this, 0f);
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(250))
            {
                //TODO 这里可以更新计数器
                UpdateQliphothCounter();
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }

            stringBuilder.Append("QliphothCounterInspect".Translate());
            stringBuilder.Append($"：{qliphothCounter}");
            return stringBuilder.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }

        public override void Notify_DefsHotReloaded()
        {
            base.Notify_DefsHotReloaded();
            UpdateQliphothCounter();
        }

        /// <summary>
        /// 更新逆卡巴拉计数器
        /// </summary>
        private void UpdateQliphothCounter()
        {
            foreach (Thing thing in Platforms)
            {
                var platform = thing as Building_HoldingPlatform;
                if (platform != null)
                {
                    var entity = platform.HeldPawn.TryGetComp<LC_CompEntity>();
                    if (entity != null)
                    {
                        QliphothCounter = entity.QliphothCountCurrent;
                    }
                    else
                    {
                        QliphothCounter = 0;
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref qliphothCounter, "qliphothCounterCurrent", 0);
        }
    }
}
