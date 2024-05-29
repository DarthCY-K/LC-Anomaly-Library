﻿using LCAnomalyLibrary.Util;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace LCAnomalyLibrary.Comp
{
    /// <summary>
    /// LC基础实体抽象Comp
    /// </summary>
    public abstract class LC_CompEntity : ThingComp
    {
        #region 变量

        public LC_CompProperties_Entity Props => (LC_CompProperties_Entity)props;

        /// <summary>
        /// 生物特征
        /// </summary>
        public int biosignature;

        /// <summary>
        /// XML输入：逆卡巴拉计数器最大值
        /// </summary>
        public int QliphothCountMax => Props.qliphothCountMax;

        /// <summary>
        /// 逆卡巴拉计数器当前值
        /// </summary>
        public int QliphothCountCurrent
        {
            get => qliphothCountCurrent;
            set
            {
                if (qliphothCountCurrent == value)
                    return;

                //小于0强制归零，大于最大值时若当前值已经异常就强制归最大，其他情况正常变化
                if (value <= 0)
                {
                    qliphothCountCurrent = 0;
                    Log.Message("{SelfPawn.def.defName} 的逆卡巴拉计数器变化，变为：0");
                    QliphothMeltdown();
                }
                else if (value > Props.qliphothCountMax)
                {
                    if (qliphothCountCurrent > Props.qliphothCountMax)
                        qliphothCountCurrent = Props.qliphothCountMax;

                    return;
                }
                else
                {
                    qliphothCountCurrent = value;
                    Log.Message($"{SelfPawn.def.defName} 的逆卡巴拉计数器变化，变为：{QliphothCountCurrent}");
                }
            }
        }

        /// <summary>
        /// 逆卡巴拉计数器当前值
        /// </summary>
        private int qliphothCountCurrent;

        /// <summary>
        /// 生物特征名
        /// </summary>
        protected string biosignatureName;

        /// <summary>
        /// 生物特征名
        /// </summary>
        public string BiosignatureName => biosignatureName ?? (biosignatureName = AnomalyUtility.GetBiosignatureName(biosignature));

        /// <summary>
        /// Comp被挂载的Pawn
        /// </summary>
        protected Pawn SelfPawn => (Pawn)parent;

        #endregion

        #region 触发事件

        /// <summary>
        /// 被看到的操作
        /// </summary>
        protected virtual void CheckIfSeen() { }

        /// <summary>
        /// 逃脱收容后执行的操作
        /// </summary>
        public abstract void Notify_Escaped();

        /// <summary>
        /// 被研究后执行的操作
        /// </summary>
        public abstract void Notify_Studied(Pawn studier);

        /// <summary>
        /// 绑到收容平台上的操作
        /// </summary>
        public virtual void Notify_Holded()
        {
            QliphothCountCurrent = Props.qliphothCountMax;
        }

        /// <summary>
        /// 研究失败事件
        /// </summary>
        /// <param name="studier">研究者</param>
        protected virtual void StudyEvent_Failure(Pawn studier)
        {
            QliphothCountCurrent--;
            StudyUtil.DoStudyResultEffect(studier, SelfPawn, LC_StudyResult.Bad);
            CheckSpawnPeBox(studier, Props.amountPeBoxStudyFail);
        }
        
        /// <summary>
        /// 研究成功事件
        /// </summary>
        /// <param name="studier">研究者</param>
        protected virtual void StudyEvent_Success(Pawn studier)
        {
            QliphothCountCurrent++;
            StudyUtil.DoStudyResultEffect(studier, SelfPawn, LC_StudyResult.Good);
            CheckSpawnPeBox(studier, Props.amountPeBoxStudySuccess);
        }

        /// <summary>
        /// 逆卡巴拉熔毁事件
        /// </summary>
        protected virtual void QliphothMeltdown()
        {
            Log.Message($"{SelfPawn.def.defName} 的收容单元发生了熔毁");

            CompHoldingPlatformTarget comp = SelfPawn.TryGetComp<CompHoldingPlatformTarget>();
            if (comp != null)
            {
                Log.Message($"{SelfPawn.def.defName} 因收容单元熔毁而出逃");
                comp.Escape(initiator: true);
            }
        }

        #endregion

        #region 生命周期

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref biosignature, "biosignature", 0);
            Scribe_Values.Look(ref qliphothCountCurrent, "qliphothCountCurrent", defaultValue: QliphothCountMax);
        }

        #endregion

        #region 工具功能

        /// <summary>
        /// 检查饰品是否冲突
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual bool CheckIfAccessoryConflict(Pawn studier, HediffDef hediffDef, string tag)
        {
            //没有相关hediff就不冲突，可添加
            var hediffs = studier.health.hediffSet.hediffs;
            List<Hediff> hediffs1 = new List<Hediff>();
            foreach (var hediff in hediffs)
            {
                if ((hediff.def.tags != null) && hediff.def.tags.Contains(tag))
                    hediffs1.Add(hediff);
            }
            if (hediffs1.NullOrEmpty())
            {
                Log.Message("没有检测相同部位的hediff");
                return true;
            }

            //如果有相同的hediff则不进行添加操作，否则清理重复部位的hediff
            foreach (var hediff in hediffs1)
            {
                if (hediff.def == hediffDef)
                {
                    Log.Message("检测到相同Hediff");
                    return false;
                }
                else
                    studier.health.RemoveHediff(hediff);
            }
            return true;
        }

        /// <summary>
        /// 检查研究是否成功
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <returns></returns>
        protected virtual bool CheckIfStudySuccess(Pawn studier)
        {
            if (CheckStudierSkillRequire(studier))
            {
                if (CheckIfFinalStudySuccess(studier))
                {
                    StudyEvent_Success(studier);
                    return true;
                }
                else
                {
                    StudyEvent_Failure(studier);
                    return false;
                }
            }
            else
            {
                StudyEvent_Failure(studier);
                return false;
            }
        }

        /// <summary>
        /// 检查研究最终判定是否成功
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <returns></returns>
        protected abstract bool CheckIfFinalStudySuccess(Pawn studier);

        /// <summary>
        /// 检查研究者技能是否符合最低要求
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <returns></returns>
        protected abstract bool CheckStudierSkillRequire(Pawn studier);

        /// <summary>
        /// 检查是否生成Pebox
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <param name="amount">生成数量</param>
        protected virtual void CheckSpawnPeBox(Pawn studier, int amount)
        {
            if (amount <= 0)
                return;

            if (studier != null)
            {
                if (Props.peBoxDef != null)
                {
                    Thing thing = ThingMaker.MakeThing(Props.peBoxDef);
                    thing.stackCount = amount;
                    GenSpawn.Spawn(thing, studier.Position, studier.Map);
                    Log.Message($"{SelfPawn.def.defName}生成了{amount}单位的{Props.peBoxDef.defName}");
                }
            }
        }

        /// <summary>
        /// 检查是否给予饰品
        /// </summary>
        protected virtual void CheckGiveAccessory(Pawn studier, HediffDef hediffDef, string tag)
        {
            //概率排前面是为了减少计算量，避免下面的foreach每次都要触发
            if (!Rand.Chance(Props.accessoryChance))
            {
                Log.Message($"{studier.Name} 获取饰品失败，概率判定失败");
                return;
            }

            if (CheckIfAccessoryConflict(studier, hediffDef, tag))
            {
                var bodypart = studier.RaceProps.body.corePart;
                if (bodypart != null)
                {
                    studier.health.AddHediff(hediffDef, bodypart);
                    Log.Message($"{studier.Name} 获取饰品成功");
                }
                else
                {
                    Log.Message($"{studier.Name} 获取饰品失败，身体核心部位为空");
                }
            }
            else
            {
                Log.Message($"{studier.Name} 获取饰品失败，已经拥有相同饰品");
            }
        }

        /// <summary>
        /// Debug：调试用逆卡巴拉强制熔毁
        /// </summary>
        protected void ForceQliphothMeltdown()
        {
            QliphothCountCurrent = 0;
        }

        #endregion
    }
}
