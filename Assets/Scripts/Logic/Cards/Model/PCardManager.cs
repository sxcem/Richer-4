﻿using System.Collections.Generic;

/// <summary>
/// PCardManager类：牌的管理类
/// </summary>
public class PCardManager {
    private readonly PGame Game;

    public List<PPlayerCardArea> PlayerAreaList;
    public PCardArea CardHeap;
    public PCardArea ThrownCardHeap;
    public PCardArea SettlingArea;

    public PCardManager(PGame _Game) {
        Game = _Game;
        PlayerAreaList = new List<PPlayerCardArea>();
        CardHeap = new PCardArea("牌堆");
        ThrownCardHeap = new PCardArea("弃牌堆");
        SettlingArea = new PCardArea("结算区");
    }

    public void Clear() {
        PlayerAreaList.ForEach((PPlayerCardArea Area) => Area.Clear());
        CardHeap.CardList.Clear();
        ThrownCardHeap.CardList.Clear();
        SettlingArea.CardList.Clear();
    }

    public void MoveCard(PCard Card, PCardArea Source, PCardArea Destination) {
        if (Source.CardList.Contains(Card)) {
            PMoveCardTag MoveCardTag = Game.Monitor.CallTime(PTime.Card.LeaveAreaTime, new PMoveCardTag(Card, Source, Destination));
            MoveCardTag.Source.CardList.Remove(Card);
            if (MoveCardTag.Source.Owner != null) {
                if (MoveCardTag.Source.IsHandCardArea()) {
                    PNetworkManager.NetworkServer.TellClient(MoveCardTag.Source.Owner, new PRefreshHandCardsOrder(MoveCardTag.Source.ToStringArray()));
                    PNetworkManager.NetworkServer.TellClients(new PRefreshHandCardNumberOrder(MoveCardTag.Source.Owner.Index.ToString(), MoveCardTag.Source.CardNumber.ToString()));
                }
            }
            Game.Monitor.CallTime(PTime.Card.EnterAreaTime, MoveCardTag);
            MoveCardTag.Destination.CardList.Add(Card);
            if (MoveCardTag.Destination.Owner != null) {
                if (MoveCardTag.Destination.IsHandCardArea()) {
                    PNetworkManager.NetworkServer.TellClient(MoveCardTag.Destination.Owner, new PRefreshHandCardsOrder(MoveCardTag.Destination.ToStringArray()));
                    PNetworkManager.NetworkServer.TellClients(new PRefreshHandCardNumberOrder(MoveCardTag.Destination.Owner.Index.ToString(), MoveCardTag.Destination.CardNumber.ToString()));
                }
            }
        }
    }

    /// <summary>
    /// 初始化牌堆，将一副新的牌加入到牌堆里并洗牌
    /// </summary>
    public void InitializeCardHeap() {
        Clear();
        PlayerAreaList = new List<PPlayerCardArea>();
        Game.PlayerList.ForEach((PPlayer Player) => {
            PlayerAreaList.Add(new PPlayerCardArea(Player));
        });
        (new List<PCard>() {
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate(),
            new P_ManTiienKuoHai().Instantiate()
        }).ForEach((PCard Card) => {
            CardHeap.CardList.Add(Card);
        });
        CardHeap.Wash();
    }

}