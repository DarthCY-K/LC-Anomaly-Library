﻿using Verse;

namespace LCAnomalyLibrary.Comp
{
    /// <summary>
    /// LC可提取EGO CompProperties
    /// </summary>
    public class LC_CompProperties_EgoExtractable : CompProperties
    {
        #region XML字段

        /// <summary>
        /// XML：EGO武器
        /// </summary>
        public ThingDef weaponExtracted;
        /// <summary>
        /// XML：EGO装备
        /// </summary>
        public ThingDef armorExtracted;

        /// <summary>
        /// XML：可提取EGO武器的上限数量
        /// </summary>
        public int amountMaxWeapon;
        /// <summary>
        /// XML：可提取EGO装备的上限数量
        /// </summary>
        public int amountMaxArmor;

        /// <summary>
        /// XML: 提取EGO武器所需PeBox数量
        /// </summary>
        public int weaponExtractedNeed;
        /// <summary>
        /// XML: 提取EGO装备所需PeBox数量
        /// </summary>
        public int armorExtractedNeed;

        /// <summary>
        /// 提取EGO所需完成的科技
        /// </summary>
        public ResearchProjectDef researchProject;

        #endregion

        /// <summary>
        /// Comp
        /// </summary>
        public LC_CompProperties_EgoExtractable()
        {
            compClass = typeof(LC_CompEgoExtractable);
        }
    }
}