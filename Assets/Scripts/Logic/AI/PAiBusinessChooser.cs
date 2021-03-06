﻿
using System;
using System.Collections.Generic;

public class PAiBusinessChooser {

    public static List<int> DirectionExpectations(PGame Game, PPlayer Player, PBlock Block) {
        /*
         * AI决策商业用地类型的机制：
         * 购物中心收益：2*（40%*max（1，20*建房次数上限/环长）+20%）*地价*敌方人数
         * 研究所收益  ：牌堆期望收益*2*己方人数
         * 公园收益    ：（max（1，20*建房次数上限/环长）*60%+50%）*地价
         * 城堡收益    ：（50%+敌方人数*20%）*赠送房屋数量*地价
         * 当铺收益    ：2000*己方人数
         * 
         * 特殊计算：
         * 杨玉环：研究所+4000
         */
        int RingLength = PAiMapAnalyzer.GetRingLength(Game, Block);
        int MaxOperationCount = Player.PurchaseLimit;
        int MikuBias = (Player.General is P_IzayoiMiku && Player.RemainLimit("轮舞曲")) ? 1 : 0;

        int ShoppingCenterExpectation = 2 * PMath.Percent(Block.Price, 40 * Math.Max(1, 20 * MaxOperationCount / RingLength) + 20) * Game.Enemies(Player).Count;
        int InsituteExpectation = 2000 * 2 * Game.Teammates(Player).Count;
        int ParkExpectation = PMath.Percent(Block.Price, 60 * Math.Max(1, 20 * MaxOperationCount / RingLength) + 50);
        int CastleExpectation = Game.Enemies(Player).Exists((PPlayer _Player) => _Player.Money > 3000 && _Player.Weapon != null && _Player.Weapon.Model is P_ToouShihChi) ? 0: PMath.Percent(Block.Price, 50 + 20 * Game.Enemies(Player).Count) * Game.GetBonusHouseNumberOfCastle(Player, Block);
        if (MikuBias == 1 && Game.GetBonusHouseNumberOfCastle(Player, Block) >= PMath.Max(Game.Map.BlockList.FindAll((PBlock _Block) => _Block.IsBusinessLand && _Block.Lord != null && _Block.Lord.TeamIndex == Player.TeamIndex).ConvertAll((PBlock _Block) => _Block.HouseNumber))) {
            CastleExpectation = PMath.Percent(Block.Price, 50 + 40 * Game.Enemies(Player).Count) * Game.GetBonusHouseNumberOfCastle(Player, Block);
        }

        int PawnshopExpectation = 2000 * Game.Teammates(Player).Count;
        int AltarExpectation = 1000 * 20 * 6 / RingLength * Game.Enemies(Player).Count;

        if (Player.General is P_YangYuHuan) {
            InsituteExpectation += 4000;
        }
        if (Player.General is P_Xdyu || Player.General is P_Gryu) {
            ShoppingCenterExpectation += 4000;
        }

        List<int> ExpectationList = new List<int>() {
            ShoppingCenterExpectation,
            InsituteExpectation,
            ParkExpectation,
            CastleExpectation,
            AltarExpectation
        };

        return ExpectationList;
    }

    public static PBusinessType ChooseDirection(PGame Game, PPlayer Player, PBlock Block) {
        List<int> ExpectationList = DirectionExpectations(Game, Player, Block);
        List<double> Weights = ExpectationList.ConvertAll((int Raw) => Math.Pow(Math.E, (double)Raw / 1000));
        return new PBusinessType[] {
            PBusinessType.ShoppingCenter,
            PBusinessType.Institute,
            PBusinessType.Park,
            PBusinessType.Castle,
            PBusinessType.Altar
        }[PMath.RandomIndex(Weights)];
    }
}