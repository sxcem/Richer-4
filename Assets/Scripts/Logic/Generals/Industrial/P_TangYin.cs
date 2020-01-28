﻿using System;
using System.Linq;
using System.Collections.Generic;

public class P_TangYin: PGeneral {

    public static KeyValuePair<PCard, int> LangZiValue(PGame Game, PPlayer Player) {
        Dictionary<PCard, int> Dict = new Dictionary<PCard, int>();
        PCard Answer = null;
        int AnswerValue = -1;
        foreach (PCardType CardType in new PCardType[] {
                PCardType.WeaponCard, PCardType.DefensorCard, PCardType.TrafficCard
            }) {
            PCard CurrentCard = Player.GetEquipment(CardType);
            if (CurrentCard != null) {
                List<PCard> AvailableCardList = Player.Area.HandCardArea.CardList.FindAll((PCard _Card) => _Card.Type.Equals(CardType));
                if (AvailableCardList.Count > 0) {
                    KeyValuePair<PCard, int> MinCard = PMath.Min(AvailableCardList, (PCard _Card) => _Card.Model.AIInEquipExpectation(Game, Player));
                    KeyValuePair<PCard, int> MaxCard = PMath.Max(AvailableCardList, (PCard _Card) => _Card.Model.AIInEquipExpectation(Game, Player));
                    int CurrentValue = CurrentCard.Model.AIInEquipExpectation(Game, Player);
                    if (CurrentValue > MinCard.Value) {
                        Dict.Add(MinCard.Key, 0);
                    }
                    if (CurrentValue < MaxCard.Value) {
                        Dict.Add(CurrentCard, MaxCard.Value - CurrentValue);
                    }
                }
            }
        }
        foreach (KeyValuePair<PCard, int> Record in Dict) {
            if (Record.Value > AnswerValue) {
                AnswerValue = Record.Value;
                Answer = Record.Key;
            }
        }
        return new KeyValuePair<PCard, int>(Answer, AnswerValue);
    }

    public static KeyValuePair<int, int> LangZiBannedNumber(PGame Game, PPlayer Player) {
        int Original = PAiMapAnalyzer.StartFromExpect(Game, Player, Player.Position);
        int Answer = 0;
        int AnswerValue = 0;
        for (int i = 1; i < 6; ++i) {
            int New = PAiMapAnalyzer.StartFromExpect(Game, Player, Player.Position, i);
            if (New - Original > AnswerValue) {
                Answer = i;
                AnswerValue = New - Original;
            }
        }
        return new KeyValuePair<int, int>(Answer, AnswerValue);
    }

    public P_TangYin() : base("唐寅") {
        Sex = PSex.Male;
        Age = PAge.Industrial;
        Index = 20;
        Cost = 25;
        Tips = "定位：防御\n" +
            "难度：中等\n" +
            "史实：明代画家、书法家、诗人，“明四家”和“吴中四才子”之一。以风流之名传于世，后代有“唐伯虎点秋香”等传说。\n" +
            "攻略：\n暂无";

        PSkill LangZi = new PSkill("浪子");
        SkillList.Add(LangZi
            .AddTimeTrigger(
            new PTime[] {
                PPeriod.DiceStage.Start
            },
            (PTime Time, PPlayer Player, PSkill Skill) => {
                return new PTrigger(LangZi.Name) {
                    IsLocked = false,
                    Player = Player,
                    Time = Time,
                    AIPriority = 10,
                    Condition = (PGame Game) => {
                        return Player.Equals(Game.NowPlayer) &&
                        Player.HasEquipInArea();
                    },
                    AICondition = (PGame Game) => {
                        KeyValuePair<PCard, int> CardValue = LangZiValue(Game, Player);
                        KeyValuePair<int, int> SkillValue = LangZiBannedNumber(Game, Player);
                        return CardValue.Key != null && SkillValue.Key > 0 && SkillValue.Value > 300 && SkillValue.Value + CardValue.Value >= 1000;
                    },
                    Effect = (PGame Game) => {
                        LangZi.AnnouceUseSkill(Player);
                        PCard TargetCard = null;
                        if (Player.IsAI) {
                            TargetCard = LangZiValue(Game, Player).Key;
                        } else {
                            do {
                                TargetCard = PNetworkManager.NetworkServer.ChooseManager.AskToChooseOwnCard(Player, LangZi.Name + "[选择一张装备牌]", true, true);
                            } while (!TargetCard.Type.IsEquipment());
                        }
                        if (TargetCard != null) {
                            int BannedNumber = 0;
                            if (Player.IsAI) {
                                BannedNumber = LangZiBannedNumber(Game, Player).Key;
                            } else {
                                BannedNumber = PNetworkManager.NetworkServer.ChooseManager.Ask1To6(Player, LangZi.Name + "[选择不会被掷出的数字]");
                            }
                            if (BannedNumber > 0) {
                                Game.CardManager.MoveCard(TargetCard, Player.Area.HandCardArea.CardList.Contains(TargetCard) ? Player.Area.HandCardArea : Player.Area.EquipmentCardArea, Game.CardManager.ThrownCardHeap);
                                Player.Tags.CreateTag(new PNumberedTag(LangZi.Name, BannedNumber));
                            }
                        }
                    }
                };
            })
            .AddTrigger((PPlayer Player, PSkill Skill) => {
                return new PTrigger(LangZi.Name + "[掷骰无效触发]") {
                    IsLocked = true,
                    Player = Player,
                    Time = PPeriod.DiceStage.During,
                    Condition = (PGame Game) => {
                        return Player.Equals(Game.NowPlayer) && Player.Tags.ExistTag(LangZi.Name);
                    },
                    Effect = (PGame Game) => {
                        int BannedNumber = Player.Tags.PopTag<PNumberedTag>(LangZi.Name).Value;
                        PDiceResultTag DiceResult = Game.TagManager.FindPeekTag<PDiceResultTag>(PDiceResultTag.TagName);
                        if (BannedNumber == DiceResult.DiceResult) {
                            LangZi.AnnouceUseSkill(Player);
                            int NewNumber = BannedNumber;
                            while (NewNumber == BannedNumber) {
                                NewNumber = PMath.RandInt(1, 6);
                            }
                            PNetworkManager.NetworkServer.TellClients(new PShowInformationOrder("掷骰结果更改为" + NewNumber.ToString()));
                            DiceResult.DiceResult = NewNumber;
                        }
                    }
                };
            })
        );
        PSkill FengLiu = new PSkill("风流");
        const int FengLiuInjure = 800;
        SkillList.Add(FengLiu
            .AddTimeTrigger(
            new PTime[] {
                PTime.Injure.EmitInjure
            },
            (PTime Time, PPlayer Player, PSkill Skill) => {
                return new PTrigger(FengLiu.Name) {
                    IsLocked = false,
                    Player = Player,
                    Time = Time,
                    AIPriority = 150,
                    Condition = (PGame Game) => {
                        PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                        return InjureTag.Injure > 0 && Player.Equals(InjureTag.FromPlayer) && InjureTag.ToPlayer != null && !Player.Equals(InjureTag.ToPlayer);
                    },
                    AICondition = (PGame Game) => {
                        PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                        PPlayer ToPlayer = InjureTag.ToPlayer;
                        if (ToPlayer.TeamIndex == Player.TeamIndex) {
                            if (ToPlayer.Money > FengLiuInjure + InjureTag.Injure && ToPlayer.Money > Player.Money) {
                                return true;
                            } else if (ToPlayer.Money <= InjureTag.Injure) {
                                return true;
                            } else if (ToPlayer.Area.EquipmentCardArea.CardNumber == 0) {
                                return false;
                            }
                        }
                        foreach (PCardType CardType in new PCardType[] {
                            PCardType.WeaponCard, PCardType.DefensorCard, PCardType.TrafficCard
                        }) {
                            PCard CurrentCard = Player.GetEquipment(CardType);
                            PCard TestCard = ToPlayer.GetEquipment(CardType);
                            if (ToPlayer.TeamIndex == Player.TeamIndex) {
                                if (CurrentCard == null && TestCard != null && TestCard.AIInEquipExpectation(Game, Player) > TestCard.AIInEquipExpectation(Game, ToPlayer)) {
                                    return true;
                                }
                            } else if (CurrentCard != null && TestCard != null && CurrentCard.AIInEquipExpectation(Game, Player) >= TestCard.AIInEquipExpectation(Game, Player) + TestCard.AIInEquipExpectation(Game, ToPlayer)) {
                                return false;
                            }
                        }
                        return ToPlayer.TeamIndex != Player.TeamIndex;
                    },
                    Effect = (PGame Game) => {
                        FengLiu.AnnouceUseSkill(Player);
                        PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                        PPlayer ToPlayer = InjureTag.ToPlayer;
                        int Answer = 0;
                        PCard TargetCard = null;
                        if (ToPlayer.Area.EquipmentCardArea.CardNumber == 0) {
                            Answer = 1;
                        } else {
                            if (ToPlayer.IsAI) {
                                if (ToPlayer.TeamIndex == Player.TeamIndex) {
                                    if (ToPlayer.Money <= InjureTag.Injure) {
                                        Answer = 1;
                                    } else {
                                        foreach (PCard TestCard in ToPlayer.Area.EquipmentCardArea.CardList) {
                                            if (Player.GetEquipment(TestCard.Type) == null && TestCard.AIInEquipExpectation(Game, Player) > TestCard.AIInEquipExpectation(Game, ToPlayer)) {
                                                TargetCard = TestCard;
                                                break;
                                            }
                                        }
                                        if (TargetCard == null && ToPlayer.Money > InjureTag.Injure + 1000) {
                                            Answer = 1;
                                        }
                                    }
                                } else {
                                    int Value = FengLiuInjure * 2;
                                    if (ToPlayer.Money <= InjureTag.Injure) {
                                        Value -= FengLiuInjure;
                                    } else if (ToPlayer.Money <= InjureTag.Injure + FengLiuInjure) {
                                        Value += 30000;
                                    }
                                    foreach (PCard TestCard in ToPlayer.Area.EquipmentCardArea.CardList) {
                                        int NowValue = TestCard.AIInEquipExpectation(Game, ToPlayer);
                                        int GiveValue = TestCard.AIInEquipExpectation(Game, Player);
                                        int OverrideValue = 0;
                                        if (Player.GetEquipment(TestCard.Type) != null) {
                                            OverrideValue = Player.GetEquipment(TestCard.Type).AIInEquipExpectation(Game, Player);
                                        }
                                        if (Value > NowValue + GiveValue - OverrideValue) {
                                            Value = NowValue + GiveValue - OverrideValue;
                                            TargetCard = TestCard;
                                        }
                                    }
                                    if (TargetCard == null) {
                                        Answer = 1;
                                    }
                                }
                            } else {
                                Answer = PNetworkManager.NetworkServer.ChooseManager.Ask(ToPlayer, FengLiu.Name, new string[] { "交给" + Player.Name + "一件装备", "受到的伤害+" + FengLiuInjure.ToString() });
                            }
                        }
                        if (Answer == 0) {
                            Game.GiveCardTo(ToPlayer, Player, false, true, false, true);
                        } else {
                            InjureTag.Injure += FengLiuInjure;
                        }
                    }
                };
            })
        );
    }
}