﻿
using System.Collections.Generic;
/// <summary>
/// 八卦阵
/// </summary>
public class P_PaKuaChevn : PEquipmentCardModel {

    public override int AIInEquipExpectation(PGame Game, PPlayer Player) {
        if (Player.General is P_LiuJi) {
            return 7000;
        }
        return 3000;
    }

    public readonly static string CardName = "八卦阵";

    public P_PaKuaChevn():base(CardName, PCardType.DefensorCard) {
        Point = 1;
        Index = 49;
        foreach (PTime Time in new PTime[] {
            PTime.Injure.AcceptInjure
        }) {
            MoveInEquipTriggerList.Add((PPlayer Player, PCard Card) => {
                return new PTrigger(CardName) {
                    IsLocked = false,
                    Player = Player,
                    Time = Time,
                    AIPriority = 150,
                    Condition = (PGame Game) => {
                        PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                        return Player.Equals(InjureTag.ToPlayer) && InjureTag.Injure > 0;
                    },
                    AICondition = (PGame Game) => {
                        PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                        if (InjureTag.FromPlayer != null && InjureTag.FromPlayer.General is P_IzayoiMiku && Player.General is P_Gabriel) {
                            return false;
                        }
                        return InjureTag.Injure > 500 || Player.General is P_LiuJi;
                    },
                    Effect = (PGame Game ) => {
                        AnnouceUseEquipmentSkill(Player);
                        PInjureTag InjureTag = Game.TagManager.FindPeekTag<PInjureTag>(PInjureTag.TagName);
                        int Result = Game.Judge(Player, InjureTag.Injure > 500 ? 5 : 6);
                        if (Result % 2 == 1) {
                            PNetworkManager.NetworkServer.TellClients(new PShowInformationOrder(CardName + "：成功"));
                            InjureTag.Injure = PMath.Percent(InjureTag.Injure, 50);
                        } else {
                            PNetworkManager.NetworkServer.TellClients(new PShowInformationOrder(CardName + "：失败"));
                        }
                    }
                };
            });
        }
    }
}