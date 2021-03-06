﻿using System;
using System.Collections.Generic;
/// <summary>
/// 连环计
/// </summary>
/// 

public class PLockTriggerInstaller : PSystemTriggerInstaller {

    public PLockTriggerInstaller() : base("连环传递伤害") {
        TriggerList.Add(new PTrigger("连环传递伤害") {
            IsLocked = true,
            Time = PTime.Injure.EndSettle,
            Condition = (PGame Game) => {
                PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                return InjureTag.ToPlayer != null && InjureTag.Injure > 0 && InjureTag.ToPlayer.Tags.ExistTag(PTag.LockedTag.Name);
            },
            Effect = (PGame Game) => {
                PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                InjureTag.ToPlayer.Tags.PopTag<PTag>(PTag.LockedTag.Name);
                bool Invoke = false;
                Game.Traverse((PPlayer _Player) => {
                    if (!Invoke && _Player.Tags.ExistTag(PTag.LockedTag.Name)) {
                        Invoke = true;
                        PNetworkManager.NetworkServer.TellClients(new PPushTextOrder(_Player.Index.ToString(), "触发连锁伤害", PPushType.Injure.Name));
                        Game.Injure(InjureTag.FromPlayer, _Player, InjureTag.Injure, InjureTag.InjureSource);
                    }
                }, Game.GetNextPlayer(InjureTag.ToPlayer));
            }
        });
    }
}

public class P_LienHuanChi : PSchemeCardModel {

    public List<PPlayer> AIEmitTargets(PGame Game, PPlayer Player) {
        List<PPlayer> Targets = new List<PPlayer>();
        int Cal(PPlayer _Player) {
            int Base = 1000;
            Base += Math.Max(0, (20000 - _Player.Money) / 10);
            bool Positive = !_Player.Tags.ExistTag(PTag.LockedTag.Name);
            Positive ^= Player.TeamIndex == _Player.TeamIndex;
            return Base * (Positive ? 1 : -1);
        }
        Targets.Add(PMath.Max(Game.PlayerList.FindAll((PPlayer _Player) => _Player.IsAlive && !(_Player.Defensor != null && _Player.Defensor.Model is P_YooHsi)), Cal, true).Key);
        Targets.Add(PMath.Max(Game.PlayerList.FindAll((PPlayer _Player) => _Player.IsAlive && !_Player.Equals(Targets[0]) && !(_Player.Defensor != null && _Player.Defensor.Model is P_YooHsi)), Cal, true).Key);
        return Targets;
    }

    public override int AIInHandExpectation(PGame Game, PPlayer Player) {
        int Basic = 3000;
        if (Game.Enemies(Player).Count < 2) {
            if (Game.AlivePlayerNumber <= 2) {
                return 0;
            } else {
                return 1500;
            }
        }
        return Math.Max(Basic, base.AIInHandExpectation(Game, Player));
    }

    public readonly static string CardName = "连环计";

    public P_LienHuanChi():base(CardName) {
        Point = 6;
        Index = 35;
        foreach (PTime Time in new PTime[] {
            PPeriod.FirstFreeTime.During,
            PPeriod.SecondFreeTime.During
        }) {
            MoveInHandTriggerList.Add((PPlayer Player, PCard Card) => {
                return new PTrigger(CardName) {
                    IsLocked = false,
                    Player = Player,
                    Time = Time,
                    AIPriority = 50,
                    Condition = (PGame Game) => {
                        return Player.Equals(Game.NowPlayer) && (Player.IsAI || Game.Logic.WaitingForEndFreeTime());
                    },
                    AICondition = (PGame Game) => {
                        return !AIEmitTargets(Game, Player).Exists((PPlayer _Player) => _Player == null);
                    },
                    Effect = MakeMultiTargetNormalEffect(Player, Card, AIEmitTargets, PTrigger.NoCondition,
                        (PGame Game, PPlayer User, PPlayer Target) => {
                            if (!Target.Tags.ExistTag(PTag.LockedTag.Name)) {
                                Target.Tags.CreateTag(PTag.LockedTag);
                            } else {
                                Target.Tags.PopTag<PTag>(PTag.LockedTag.Name);
                            }
                        }, 2)
                };
            });
        }
    }
}