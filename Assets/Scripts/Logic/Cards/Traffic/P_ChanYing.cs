﻿using System.Linq;
using System.Collections.Generic;
/// <summary>
/// 战鹰
/// </summary>
public class P_ChanYing : PEquipmentCardModel {

    public override int AIInEquipExpectation(PGame Game, PPlayer Player) {
        return Player.IsAI ? 100 : 2000 * Game.Teammates(Player).FindAll((PPlayer _Player) => _Player.IsUser).Count;
    }

    public readonly static string CardName = "战鹰";

    public P_ChanYing():base(CardName, PCardType.TrafficCard) {
        Point = 2;
        Index = 56;
        AnnouceOnce(CardName);
        foreach (PTime Time in new PTime[] {
            PPeriod.FirstFreeTime.During,
            PPeriod.SecondFreeTime.During
        }) {
            MoveInEquipTriggerList.Add((PPlayer Player, PCard Card) => {
                return new PTrigger(CardName) {
                    IsLocked = true,
                    Player = Player,
                    Time = Time,
                    AIPriority = 0,
                    Condition = (PGame Game) => {
                        PUsedTag UsedTag = Player.Tags.FindPeekTag<PUsedTag>(PUsedTag.TagNamePrefix + CardName);
                        return Player.Equals(Game.NowPlayer) && Player.IsUser && Game.Logic.WaitingForEndFreeTime() && UsedTag != null && UsedTag.Count < UsedTag.Limit;
                    },
                    Effect = (PGame Game ) => {
                        AnnouceUseEquipmentSkill(Player);
                        if (Player.IsUser) {
                            PPlayer Target = PNetworkManager.NetworkServer.ChooseManager.AskForTargetPlayer(Player, PTrigger.Except(Player), CardName);
                        if (Target != null) {
                            PNetworkManager.NetworkServer.ChooseManager.Ask(Player, Target.Name + "的手牌", Target.Area.HandCardArea.CardList.ConvertAll((PCard _Card) => _Card.Name).Concat(new List<string> { "确认"}).ToArray());
                            }
                        }
                        Player.Tags.FindPeekTag<PUsedTag>(PUsedTag.TagNamePrefix + CardName).Count++;
                    }
                };
            });
        }
    }
}