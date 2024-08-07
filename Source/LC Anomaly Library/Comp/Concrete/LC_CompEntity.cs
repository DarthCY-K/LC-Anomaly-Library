﻿using LCAnomalyLibrary.Interface;
using LCAnomalyLibrary.Setting;
using LCAnomalyLibrary.Util;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LCAnomalyLibrary.Comp
{
    /// <summary>
    /// LC基础实体Comp
    /// </summary>
    public class LC_CompEntity : ThingComp
    {
        #region 变量

        /// <summary>
        /// CompProperties
        /// </summary>
        public LC_CompProperties_Entity Props => (LC_CompProperties_Entity)props;

        /// <summary>
        /// 研究阶段解锁
        /// </summary>
        public CompStudyUnlocks StudyUnlocksComp => parent.GetComp<CompStudyUnlocks>();

        /// <summary>
        /// 首饰组件
        /// </summary>
        public CompAccessoryable AccessoryableComp => parent.GetComp<CompAccessoryable>();

        /// <summary>
        /// PeBox组件
        /// </summary>
        public LC_CompPeBoxProduce PeBoxComp => parent.GetComp<LC_CompPeBoxProduce>();

        /// <summary>
        /// 生物特征
        /// </summary>
        public int biosignature;

        /// <summary>
        /// XML引用：逆卡巴拉计数器最大值
        /// </summary>
        public int QliphothCountMax => Props.qliphothCountMax;
        
        /// <summary>
        /// 逆卡巴拉机制是否启用
        /// </summary>
        public bool QliphothEnabled => QliphothCountMax > 0;

        /// <summary>
        /// XML引用：警报点数
        /// </summary>
        public int WarningPoints
        {
            get
            {
                //强制不提供警报点数就返回0
                if (!Props.ifProvideWarningPoints)
                {
                    //Log.Message($"警报点数：{parent.def.label.Translate()}不提供警报点数");
                    return 0;
                }

                //如果警报点数大于0就返回警报点数，否则返回等级对应的点数
                if (Props.customWarningPoints > 0)
                {
                    //Log.Message($"警报点数：{parent.def.label.Translate()}提供自定义警报点数{Props.customWarningPoints}点");
                    return Props.customWarningPoints;
                }
                else
                {
                    var points = MusicUtils.LevelTag2Points(parent.def.entityCodexEntry.category.defName);
                    //Log.Message($"警报点数：{parent.def.label.Translate()}提供根据等级的警报点数{points}点");

                    return points;
                }
            }
        }

        /// <summary>
        /// 逆卡巴拉计数器当前值
        /// </summary>
        public int QliphothCountCurrent
        {
            get
            {
                //无逆卡巴拉值的情况下只会返回-1
                if (!QliphothEnabled)
                    return -1;

                return qliphothCountCurrent;
            }
            set
            {
                //无逆卡巴拉值的情况下修改无效
                if (!QliphothEnabled)
                    return;

                //值相同则修改无效
                if (qliphothCountCurrent == value)
                    return;

                //小于0强制归零，大于最大值时若当前值已经异常就强制归最大，其他情况正常变化
                if (value <= 0)
                {
                    qliphothCountCurrent = 0;
                    //Log.Message($"逆卡巴拉计数：{parent.def.defName} 的逆卡巴拉计数器变化，变为：0");
                    QliphothMeltdown();
                }
                else if (value > Props.qliphothCountMax)
                {
                    qliphothCountCurrent = Props.qliphothCountMax;
                }
                else
                {
                    qliphothCountCurrent = value;
                    //Log.Message($"逆卡巴拉计数：{parent.def.defName} 的逆卡巴拉计数器变化，变为：{QliphothCountCurrent}");
                }
            }
        }

        private int qliphothCountCurrent;

        /// <summary>
        /// 生物特征名
        /// </summary>
        public string BiosignatureName => biosignatureName ?? (biosignatureName = AnomalyUtility.GetBiosignatureName(biosignature));

        private string biosignatureName;

        #endregion 变量

        #region 触发事件

        /// <summary>
        /// PostPostMake初始化
        /// </summary>
        public override void PostPostMake()
        {
            base.PostPostMake();

            //初始化生物特征和逆卡巴拉计数
            biosignature = Rand.Int;
            QliphothCountCurrent = Props.qliphothCountMax;
        }

        /// <summary>
        /// 逃脱收容后执行的操作
        /// </summary>
        public virtual void Notify_Escaped()
        {
            //如果出逃提醒就弹出逃信封和计算警报点数
            if (Props.shouldNotifyWhenEscape)
            {
                //TODO 这个写的不对，需要研究如何格式化
                Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("LetterLabelEscapingFromHoldingPlatform".Translate(),
                    "LetterEscapingFromHoldingPlatform", LetterDefOf.ThreatBig));

                //如果未启用警报系统，就不更新警报点数
                if (Setting_LCAnomalyLibrary_Main.Settings.If_EnableLCWarning)
                {
                    if (Components.LC != null)
                    {
                        Components.LC.CurWarningPoints += WarningPoints;
                    }
                    else
                    {
                        Log.Warning("GameComponent_LC is null");
                    }
                }
            }

            CompHoldingPlatformTarget comp = parent.TryGetComp<CompHoldingPlatformTarget>();
            comp.isEscaping = true;
        }

        /// <summary>
        /// 被研究后执行的操作
        /// </summary>
        public virtual void Notify_Studied(Pawn studier)
        {
            if (studier == null)
                return;
            var platform = parent.GetComp<CompHoldingPlatformTarget>().HeldPlatform as IHoldingPlatformWorkTypeSelectable;
            if (platform != null)
            {
                //Log.Warning($"研究者：{studier.Name} 将通过工作类型 {platform.CurWorkType.ToString()} 来通过判定");
                CheckIfStudySuccess(studier, platform.CurWorkType);
            }
        }

        /// <summary>
        /// 正在被研究时执行的操作
        /// </summary>
        public virtual void Notify_Studying(Pawn studier)
        {
        }

        /// <summary>
        /// 绑到收容平台上的操作
        /// </summary>
        public virtual void Notify_Holded()
        {
        }

        /// <summary>
        /// 研究质量：非差
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <param name="result">研究质量</param>
        protected virtual void StudyEvent_NotBad(Pawn studier, LC_StudyResult result)
        {
            switch (result)
            {
                case LC_StudyResult.Good:
                    StudyEvent_Good(studier);
                    break;

                case LC_StudyResult.Normal:
                    StudyEvent_Normal(studier);
                    break;
            }

            PeBoxComp?.CheckSpawnPeBox(studier, result);
            StudyUtil.DoStudyResultEffect(studier, (Pawn)parent, result);
        }

        /// <summary>
        /// 研究质量：优秀
        /// </summary>
        /// <param name="studier">研究者</param>
        protected virtual void StudyEvent_Good(Pawn studier)
        {
            QliphothCountCurrent++;
            AccessoryableComp?.CheckGiveAccessory(studier);
        }

        /// <summary>
        /// 研究质量：良好
        /// </summary>
        /// <param name="studier">研究者</param>
        protected virtual void StudyEvent_Normal(Pawn studier)
        {
        }

        /// <summary>
        /// 研究质量：差
        /// </summary>
        /// <param name="studier">研究者</param>
        protected virtual void StudyEvent_Bad(Pawn studier)
        {
            QliphothCountCurrent--;
        }

        /// <summary>
        /// 逆卡巴拉熔毁事件
        /// </summary>
        protected virtual void QliphothMeltdown()
        {
            //Log.Message($"收容：{parent.def.label.Translate()} 的收容单元发生了熔毁");

            CompHoldingPlatformTarget comp = parent.TryGetComp<CompHoldingPlatformTarget>();
            if (comp != null)
            {
                //Log.Message($"收容：{parent.def.label.Translate()} 因收容单元熔毁而出逃");
                comp.Escape(initiator: true);
            }
        }

        #endregion 触发事件

        #region 生命周期

        /// <summary>
        /// 我不知道这是什么，好像和数据保存有关
        /// </summary>
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref biosignature, "biosignature", 0);
            Scribe_Values.Look(ref qliphothCountCurrent, "qliphothCountCurrent", defaultValue: QliphothCountMax);
        }

        #endregion 生命周期

        #region 工具功能

        /// <summary>
        /// 检查研究是否成功
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <param name="workType">工作类型</param>
        /// <returns></returns>
        protected virtual bool CheckIfStudySuccess(Pawn studier, EAnomalyWorkType workType)
        {
            if (CheckStudierSkillRequire(studier))
            {
                StudyEvent_NotBad(studier, CheckFinalStudyQuality(studier, workType));
                return true;
            }
            else
            {
                StudyEvent_Bad(studier);
                return false;
            }
        }

        /// <summary>
        /// 计算研究质量
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <param name="workType">工作类型</param>
        /// <returns>研究质量</returns>
        protected virtual LC_StudyResult CheckFinalStudyQuality(Pawn studier, EAnomalyWorkType workType)
        {
            return LC_StudyResult.Bad;
        }

        /// <summary>
        /// 检查研究者技能是否符合最低要求
        /// </summary>
        /// <param name="studier">研究者</param>
        /// <returns></returns>
        public virtual bool CheckStudierSkillRequire(Pawn studier)
        {
            return false;
        }

        /// <summary>
        /// Debug：调试用逆卡巴拉强制熔毁
        /// </summary>
        protected void ForceQliphothMeltdown()
        {
            QliphothCountCurrent = 0;
        }

        #endregion 工具功能

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
                    defaultLabel = "kill",
                    action = delegate
                    {
                        parent.Kill(null);
                    }
                };

                //逆卡巴拉值有效情况
                if(QliphothEnabled)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Force Meltdown",
                        action = delegate
                        {
                            Log.Warning($"Dev：{parent.def.label.Translate()} 的收容单元发生了强制熔毁");
                            ForceQliphothMeltdown();
                        }
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "QliphothCount +1",
                        action = delegate
                        {
                            Log.Warning($"Dev：{parent.def.label.Translate()} 的逆卡巴拉计数器上升了1点");
                            QliphothCountCurrent++;
                        }
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "QliphothCount -1",
                        action = delegate
                        {
                            Log.Warning($"Dev：{parent.def.label.Translate()} 的逆卡巴拉计数器下降了1点");
                            QliphothCountCurrent--;
                        }
                    };
                }

                //yield return new Command_Action
                //{
                //    defaultLabel = "show text",
                //    action = delegate
                //    {
                //        Log.Warning($"Dev：Show screen text");
                //        LCCanvasSingleton.Instance.ShowText("s2as2s3sss32321ssssssssss1231ssssssssss1231sssssssssss");
                //    }
                //};
            }
        }

        #endregion UI
    }
}