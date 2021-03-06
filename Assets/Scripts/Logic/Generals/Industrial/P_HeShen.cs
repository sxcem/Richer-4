﻿using System;
using System.Collections.Generic;

public class P_HeShen: PGeneral {

    public class PShouHuiTag : PTag {
        public static string TagName = "贿";
        public PShouHuiTag(): base(TagName) { }
    }

    public P_HeShen() : base("和珅") {
        Sex = PSex.Male;
        Age = PAge.Industrial;
        Index = 24;
        Cost = 30;
        Tips = "定位：防御\n" +
            "难度：简单\n" +
            "史实：清朝中期权臣，中国历史上有名的巨贪，聚敛了约十亿两白银的巨大财富。\n" +
            "攻略：\n和珅是一名拥有不错防御力的武将。【贪污】使和珅的起始资金变相增加，而【受贿】可用于关键击杀，但一般都用于辅助队友。和珅的技能使用基本没有任何难度，因此十分适合新手使用。";

        PSkill TanWu = new PSkill("贪污") {
            Lock = true
        };
        SkillList.Add(TanWu
             .AddTrigger(
            (PPlayer Player, PSkill Skill) => {
                return new PTrigger(TanWu.Name) {
                    IsLocked = true,
                    Player = Player,
                    Time = PTime.PurchaseLandTime,
                    Condition = (PGame Game) => {
                        PPurchaseLandTag PurchaseLandTag = Game.TagManager.FindPeekTag<PPurchaseLandTag>(PPurchaseLandTag.TagName);
                        return Player.Equals(PurchaseLandTag.Player);
                    },
                    Effect = (PGame Game) => {
                        TanWu.AnnouceUseSkill(Player);
                        PPurchaseLandTag PurchaseLandTag = Game.TagManager.FindPeekTag<PPurchaseLandTag>(PPurchaseLandTag.TagName);
                        PurchaseLandTag.LandPrice = PMath.Percent(PurchaseLandTag.LandPrice, 50);
                    }
                };
            })
            .AddTrigger(
            (PPlayer Player, PSkill Skill) => {
                return new PTrigger(TanWu.Name) {
                    IsLocked = true,
                    Player = Player,
                    Time = PTime.PurchaseHouseTime,
                    Condition = (PGame Game) => {
                        PPurchaseHouseTag PurchaseHouseTag = Game.TagManager.FindPeekTag<PPurchaseHouseTag>(PPurchaseHouseTag.TagName);
                        return Player.Equals(PurchaseHouseTag.Player);
                    },
                    Effect = (PGame Game) => {
                        TanWu.AnnouceUseSkill(Player);
                        PPurchaseHouseTag PurchaseHouseTag = Game.TagManager.FindPeekTag<PPurchaseHouseTag>(PPurchaseHouseTag.TagName);
                        PurchaseHouseTag.HousePrice = PMath.Percent(PurchaseHouseTag.HousePrice, 50);
                    }
                };
            }));
        PSkill ShouHui = new PSkill("受贿") {
            Initiative = true
        };
        int ShouHuiExpect(PGame Game, PPlayer Player) {
            int Sum = 0;
            int Cnt = 0;
            foreach (PBlock Block in PAiMapAnalyzer.NextBlocks(Game, Player)) {
                if (Block.CanPurchase && Block.Lord == null) {
                    Sum += Block.Price;
                    Cnt++;
                } else if (Player.Equals(Block.Lord)) {
                    Sum += Block.HousePrice;
                    Cnt++;
                }
            }
            if (Cnt == 0) {
                return -1;
            } else {
                return Sum / Cnt;
            }
        }
        PPlayer ShouHuiTarget(PGame Game, PPlayer Player) {
            foreach (PPlayer Target in Game.Teammates(Player)) {
                if (ShouHuiExpect(Game, Target) >= 1500 && Target.Money >= 2000 && Player.RemainLimit(ShouHui.Name, Target)) {
                    return Target;
                }
            }
            foreach (PPlayer Target in Game.Enemies(Player)) {
                if (Player.RemainLimit(ShouHui.Name, Target)) {
                    int Expect = ShouHuiExpect(Game, Target);
                    if (Expect < 0) {
                        Expect = 3000; // 无法判断下次购买土地或房屋的收益
                    }
                    Expect -= PAiTargetChooser.InjureExpect(Game, Player, Player, Target, 1000, ShouHui);
                    if (Expect <= -1000) {
                        return Target;
                    }
                }
            }
            return null;
        }
        SkillList.Add(ShouHui
            .AnnounceEachPlayerOnce()
            .AddTimeTrigger(
            new PTime[] {
                PPeriod.FirstFreeTime.During,
                PPeriod.SecondFreeTime.During
            },
            (PTime Time, PPlayer Player, PSkill Skill) => {
                return new PTrigger(ShouHui.Name) {
                    IsLocked = false,
                    Player = Player,
                    Time = Time,
                    AIPriority = 10,
                    CanRepeat = true,
                    Condition = (PGame Game) => {
                        return Player.Equals(Game.NowPlayer) && (Player.IsAI || Game.Logic.WaitingForEndFreeTime()) && Game.AlivePlayers().Exists((PPlayer _Player) => Player.RemainLimit(ShouHui.Name, _Player));
                    },
                    AICondition = (PGame Game) => {
                        return ShouHuiTarget(Game, Player) != null;
                    },
                    Effect = (PGame Game) => {
                        ShouHui.AnnouceUseSkill(Player);
                        PPlayer Target = null;
                        if (Player.IsAI) {
                            Target = ShouHuiTarget(Game, Player);
                        } else {
                            Target = PNetworkManager.NetworkServer.ChooseManager.AskForTargetPlayer(Player, (PGame _Game, PPlayer _Player) => Player.RemainLimit(ShouHui.Name, _Player), ShouHui.Name, true);
                        }
                        if (Target != null) {
                            Game.Injure(Player, Target, 1000, ShouHui);
                            Target.Tags.CreateTag(new PShouHuiTag());
                            ShouHui.DeclareUseFor(Player, Target);
                        }
                    }
                };
            })
            .AddTrigger(
            (PPlayer Player, PSkill Skill) => {
                return new PTrigger(ShouHui.Name) {
                    IsLocked = true,
                    Player = null,
                    Time = PTime.PurchaseLandTime,
                    Condition = (PGame Game) => {
                        PPurchaseLandTag PurchaseLandTag = Game.TagManager.FindPeekTag<PPurchaseLandTag>(PPurchaseLandTag.TagName);
                        return PurchaseLandTag.Player.Tags.ExistTag(PShouHuiTag.TagName);
                    },
                    Effect = (PGame Game) => {
                        PPurchaseLandTag PurchaseLandTag = Game.TagManager.FindPeekTag<PPurchaseLandTag>(PPurchaseLandTag.TagName);
                        PurchaseLandTag.LandPrice = 0;
                        ShouHui.AnnouceUseSkill(PurchaseLandTag.Player);
                        PurchaseLandTag.Player.Tags.PopTag<PShouHuiTag>(PShouHuiTag.TagName);
                    }
                };
            })
            .AddTrigger(
            (PPlayer Player, PSkill Skill) => {
                return new PTrigger(ShouHui.Name) {
                    IsLocked = true,
                    Player = null,
                    Time = PTime.PurchaseHouseTime,
                    Condition = (PGame Game) => {
                        PPurchaseHouseTag PurchaseHouseTag = Game.TagManager.FindPeekTag<PPurchaseHouseTag>(PPurchaseHouseTag.TagName);
                        return PurchaseHouseTag.Player.Tags.ExistTag(PShouHuiTag.TagName);
                    },
                    Effect = (PGame Game) => {
                        PPurchaseHouseTag PurchaseHouseTag = Game.TagManager.FindPeekTag<PPurchaseHouseTag>(PPurchaseHouseTag.TagName);
                        PurchaseHouseTag.HousePrice = 0;
                        ShouHui.AnnouceUseSkill(PurchaseHouseTag.Player);
                        PurchaseHouseTag.Player.Tags.PopTag<PShouHuiTag>(PShouHuiTag.TagName);
                    }
                };
            }));
    }

}