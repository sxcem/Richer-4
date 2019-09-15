﻿using System;
using System.Collections.Generic;
/// <summary>
/// 瘟疫
/// </summary>
public class P_WevnI : PAmbushCardModel {

    private int RoundExpect(PGame Game, PPlayer Player) {
        int Sum = 0;
        double Rate = 5.0 / 6;
        Game.Traverse((PPlayer _Player) => {
            int Base = _Player.Money <= 1000 ? 30000 : 1000;
            Base *= _Player.TeamIndex == Player.TeamIndex ? -1 : 1;
            Sum += (int)(Base * Rate);
            Rate *= 5.0 / 6;
        }, Player);
        return Sum ;
    }

    public List<PPlayer> AIEmitTargets(PGame Game, PPlayer Player) {
        
        return new List<PPlayer>() { RoundExpect(Game, Player) >= 500 && (Player.Defensor == null || !(Player.Defensor.Model is P_ChiiHsingPaao)) ? Player : null };
    }

    public override int AIInHandExpectation(PGame Game, PPlayer Player) {
        return RoundExpect(Game, Player);
    }

    public override int AIInAmbushExpectation(PGame Game, PPlayer Player) {
        return RoundExpect(Game, Player);
    }

    public override void AnnouceInvokeJudge(PGame Game, PPlayer Player, PCard Card) {
        base.AnnouceInvokeJudge(Game, Player, Card);
        int Result = Game.Judge(Player);
        if (Result != 2) {
            Game.LoseMoney(Player, 1000);
            Game.CardManager.MoveCard(Card, Player.Area.AmbushCardArea, Game.GetNextPlayer(Player).Area.AmbushCardArea);
        } else {
            Game.CardManager.MoveCard(Card, Player.Area.AmbushCardArea, Game.CardManager.ThrownCardHeap);
        }
    }

    public readonly static string CardName = "瘟疫";

    public P_WevnI():base(CardName) {
        Point = 5;
        Index = 41;
        BuildAmbush(AIEmitTargets, false, 10);
    }
}