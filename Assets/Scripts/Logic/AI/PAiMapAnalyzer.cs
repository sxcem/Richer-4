﻿using System;
using System.Collections.Generic;

public class PAiMapAnalyzer {

    public static List<PBlock> NextBlocks (PGame Game, PPlayer Player, PBlock StartBlock = null) {
        if (StartBlock == null) {
            StartBlock = Player.Position;
        }
        List<PBlock> Answer = new List<PBlock>();
        PBlock Block = StartBlock;
        if (!Player.NoLadder) {
            Block = Block.NextBlock;
        }
        if (Player.Traffic != null && Player.Traffic.Model is P_ChiihTuu) {
            Block = Block.NextBlock;
        }
        if (Player.NoLadder) {
            Answer.Add(Block);
        } else {
            for (int i = 0;i < 6; ++i, Block = Block.NextBlock) {
                Answer.Add(Block);
            }
        }
        return Answer;
    }

    /// <summary>
    /// 将一个玩家移出游戏对其的收益，无卡牌收益嵌套
    /// </summary>
    /// <param name="Game"></param>
    /// <param name="Player"></param>
    /// <param name="Including">是否包含其行走阶段</param>
    /// <returns></returns>
    public static int OutOfGameExpect(PGame Game, PPlayer Player, bool Including = false, bool IncludingOnly = false) {
        if (Player.OutOfGame) {
            return 0;
        }
        int Sum = 0;
        // 一轮其他角色可能造成的过路费收益总和的相反数
        if (!IncludingOnly) {
            PPlayer _Player = Game.NowPlayer;
            if (Player.Equals(Game.NowPlayer)) {
                _Player = Game.GetNextPlayer(_Player);
            }
            for (; !_Player.Equals(Player); _Player = Game.GetNextPlayer(_Player)) {
                if (_Player.Equals(Game.NowPlayer) && Game.NowPeriod.IsAfter(PPeriod.WalkingStage)) {
                    continue;
                }
                int Temp = 0;
                List<PBlock> Blocks = NextBlocks(Game, _Player);
                Blocks.ForEach((PBlock Block) => {
                    int BlockTemp = 0;
                    if (Block.Lord != null && Player.Equals(Block.Lord)) {
                        BlockTemp = Math.Max(0, PAiTargetChooser.InjureExpect(Game, Player, Player, _Player, Block.Toll, Block));
                    }
                    if (_Player.General is P_Newton) {
                        PBlock PossibleBlock = Game.Map.NextStepBlock(Block, P_Newton.Grx_Next(Game, _Player.Position).Value);
                        if (PossibleBlock.Lord != null && Player.Equals(PossibleBlock.Lord)) {
                            BlockTemp = Math.Min(BlockTemp, Math.Max(0, PAiTargetChooser.InjureExpect(Game, Player, Player, _Player, Block.Toll, Block)));
                        } else {
                            BlockTemp = 0;
                        }
                    }
                    Temp -= BlockTemp;
                });
                Sum += Temp / Math.Max(1, Blocks.Count);
            }
        }
        int SumSelf = 0;
        if (Including) {
            List<PBlock> Blocks = NextBlocks(Game, Player);
            Blocks.ForEach((PBlock Block) => {
                int BlockTemp = 0;
                if (Block.Lord != null && Player.TeamIndex != Block.Lord.TeamIndex) {
                    BlockTemp = Math.Max(0, -PAiTargetChooser.InjureExpect(Game, Player, Block.Lord, Player, Block.Toll, Block));
                }
                if (Player.General is P_Newton) {
                    PBlock PossibleBlock = Game.Map.NextStepBlock(Block, P_Newton.Grx_Next(Game, Player.Position).Value);
                    if (PossibleBlock.Lord != null && Player.TeamIndex != PossibleBlock.Lord.TeamIndex) {
                        BlockTemp = Math.Max(BlockTemp, Math.Max(0, -PAiTargetChooser.InjureExpect(Game, Player, Block.Lord, Player, Block.Toll, Block)));
                    } else {
                        BlockTemp = 0;
                    }
                }
                SumSelf += BlockTemp;
            });
            SumSelf /= Math.Max(1, Blocks.Count);
        }
        return Sum + SumSelf;
    }

    public static int ChangeFaceExpect(PGame Game, PPlayer Player, PBlock Start = null) {
        if (Start == null) {
            Start = Player.Position;
        }
        int Value = 0;
        List<PBlock> Blocks = NextBlocks(Game, Player);
        foreach (PBlock Block in Blocks) {
            int DeltaMoney = 0;
            int Disaster = Block.GetMoneyStopSolid;
            if (Disaster < 0 && -Disaster <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
                Disaster = 0;
            } else if (Disaster < 0 && Player.Defensor != null && Player.Defensor.Model is P_YinYangChing) {
                Disaster = 0;
            }
            DeltaMoney += Disaster;
            Disaster = PMath.Percent(Player.Money, Block.GetMoneyStopPercent);
            if (Disaster < 0 && -Disaster <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
                Disaster = 0;
            } else if (Disaster < 0 && Player.Defensor != null && Player.Defensor.Model is P_YinYangChing) {
                Disaster = 0;
            }
            DeltaMoney += Disaster;
            if (Block.Lord != null && Block.Lord.TeamIndex != Player.TeamIndex) {
                int Toll = Block.Toll;
                if (Block.BusinessType.Equals(PBusinessType.ShoppingCenter)) {
                    Toll *= 2;
                }
                if (Block.BusinessType.Equals(PBusinessType.Club)) {
                    Toll += PMath.Percent(Toll, 100);
                }
                if (Block.Lord.Weapon != null && Block.Lord.Weapon.Model is P_KuTingTao && Player.Area.HandCardArea.CardNumber == 0) {
                    Toll *= 2;
                }
                if (Toll <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
                    Toll = 0;
                }
                DeltaMoney -= Toll;
            }
            if (-DeltaMoney >= Player.Money) {
                Value = 1000 * Blocks.Count;
                break;
            } else if (Block.Lord == null && Block.Price < Player.Money) {
                int LandValue = PMath.Percent(Block.Price, 10) * Game.Enemies(Player).Count;
                if (Block.IsBusinessLand) {
                    LandValue += PMath.Max(PAiBusinessChooser.DirectionExpectations(Game, Player, Block));
                }
                Value -= LandValue;
            }
        }
        Value /= Blocks.Count;
        Value *= (Player.BackFace ? -1 : 1);
        // 陈胜特殊处理
        if (Player.General is P_ChenSheng) {
            int CurrentValue = StartFromExpect(Game, Player, Player.Position, 0, false);
            int MaxValue = PMath.Max(new List<int> { 1, 2, 3, 4, 5, 6 }, (int ChenShengStep) => StartFromExpect(Game, Player, Game.Map.NextStepBlock(Player.Position, ChenShengStep), 0, false)).Value - CurrentValue;
            Value += 2000 + MaxValue;
        }
        return Value;
    }

    public static int HouseValue(PGame Game, PPlayer Player, PBlock Block) {
        if (Block.Lord == null) {
            return 0;
        }
        int EnemyCount = Game.Enemies(Block.Lord).Count;
        int Value = PMath.Percent(Block.Price, 50 + 20 * EnemyCount * (Block.BusinessType.Equals(PBusinessType.ShoppingCenter) ? 2 : 1) * (Block.BusinessType.Equals(PBusinessType.Club) ? 2 : 1));
        if (Player.TeamIndex == Block.Lord.TeamIndex) {
            return Value;
        } else {
            return -Value;
        }
    }

    public static KeyValuePair< PBlock, int> MaxValueHouse(PGame Game, PPlayer Player, bool StartFromZero = false) {
        return PMath.Max(Game.Map.BlockList.FindAll((PBlock Block) => Player.Equals(Block.Lord) && ( StartFromZero || Block.HouseNumber > 0)), (PBlock Block) => {
            return HouseValue(Game, Player, Block);
        });
    }

    /// <summary>
    /// 返回最低价值的房屋
    /// </summary>
    /// <param name="Game"></param>
    /// <param name="Player">房屋所有者</param>
    /// <param name="StartFromZero">是否允许土地上实际没有房屋</param>
    /// <param name="Concentrate">是否优先选取房屋数量少的土地的房屋</param>
    /// <returns></returns>
    public static KeyValuePair<PBlock, int> MinValueHouse(PGame Game, PPlayer Player, bool StartFromZero = false, bool Concentrate = false) {
        KeyValuePair<PBlock, int> Test = PMath.Min(Game.Map.BlockList.FindAll((PBlock Block) => Player.Equals(Block.Lord) && (StartFromZero || Block.HouseNumber > 0)), (PBlock Block) => {
            return HouseValue(Game, Player, Block) * 1000 + 
            (Concentrate ? Block.HouseNumber : 0);
        });
        return new KeyValuePair<PBlock, int>(Test.Key, Test.Value / 1000);
    }

    /// <summary>
    /// AI计算从一个点开始走的期望
    /// </summary>
    /// <param name="Game">游戏句柄</param>
    /// <param name="Player">玩家句柄</param>
    /// <param name="Block">起点的格子</param>
    /// <param name="Banned">被禁止投出的骰子点数（用于唐寅的技能【浪子】）</param>
    /// <param name="IncludePassMoney">计算是否包括经过奖励（用于计算前进收益）</param>
    /// <returns></returns>
    public static int StartFromExpect(PGame Game, PPlayer Player, PBlock Block, int Banned = 0, bool IncludePassMoney = true) {
        PBlock CurrentBlock = Block;
        if (!Player.NoLadder) {
            CurrentBlock = CurrentBlock.NextBlock;
        }
        if (Player.Traffic != null && Player.Traffic.Model is P_ChiihTuu) {
            CurrentBlock = CurrentBlock.NextBlock;
        }
        if (Player.NoLadder) {
            return Expect(Game, Player, CurrentBlock);
        }
        int Expectation = 0;
        int NewtonTargetStep = (Player.General is P_Newton ? P_Newton.Grx_Next(Game, Block).Value : 0);
        for (int i = 6; i >= 1; -- i) {
            if (Banned == i) {
                continue;
            }
            int SingleExpect = Expect(Game, Player, CurrentBlock);
            if (NewtonTargetStep > 0) {
                SingleExpect = Math.Max(SingleExpect, Expect(Game, Player, Game.Map.NextStepBlock(CurrentBlock, NewtonTargetStep)));
            }
            Expectation += SingleExpect;
            if (IncludePassMoney) {
                if (CurrentBlock.GetMoneyPassSolid != 0) {
                    int Disaster = Block.GetMoneyPassSolid;
                    if (Disaster < 0 && -Disaster <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
                        Disaster = 0;
                    } else if (Disaster < 0 && Player.Defensor != null && Player.Defensor.Model is P_YinYangChing) {
                        Disaster = 0;
                    }
                    Expectation += i * Disaster;
                }
                if (CurrentBlock.GetMoneyPassPercent != 0) {
                    int Disaster = PMath.Percent(Player.Money, CurrentBlock.GetMoneyPassPercent);
                    if (Disaster < 0 && -Disaster <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
                        Disaster = 0;
                    } else if (Disaster < 0 && Player.Defensor != null && Player.Defensor.Model is P_YinYangChing) {
                        Disaster = 0;
                    }
                    Expectation += i * Disaster;
                }
                if (CurrentBlock.GetCardPass != 0) {
                    Expectation += i * 2000 * CurrentBlock.GetCardPass;
                }
                if (CurrentBlock.BusinessType.Equals(PBusinessType.Altar) && CurrentBlock.Lord != null && CurrentBlock.Lord.TeamIndex != Player.TeamIndex) {
                    Expectation -= 1000;
                }
            }
            CurrentBlock = Block.NextBlock;
        }
        Player.Area.AmbushCardArea.CardList.ForEach((PCard Card) => {
            if (Card.Model is P_TsaaoMuChiehPing Cao) {
                Expectation += Cao.AIExpect(Game, Player, Block);
            }
        });
        return Expectation / (Banned > 0 ? 5 : 6);
    }

    public static int Expect(PGame Game, PPlayer Player, PBlock Block, bool InPortal = false) {
        int DeltaMoney = 0;
        int Disaster = Block.GetMoneyStopSolid;
        if (Disaster < 0 && -Disaster <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
            Disaster = 0;
        } else if (Disaster < 0 && Player.Defensor != null && Player.Defensor.Model is P_YinYangChing) {
            Disaster = 0;
        }
        DeltaMoney += Disaster;
        Disaster = PMath.Percent(Player.Money, Block.GetMoneyStopPercent);
        if (Disaster < 0 && -Disaster <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
            Disaster = 0;
        } else if (Disaster < 0 && Player.Defensor != null && Player.Defensor.Model is P_YinYangChing) {
            Disaster = 0;
        }
        DeltaMoney += Disaster;
        if (Block.Lord != null && Block.Lord.TeamIndex != Player.TeamIndex) {
            int Toll = Block.Toll;
            if (Block.BusinessType.Equals(PBusinessType.ShoppingCenter)) {
                Toll *= 2;
            }
            if (Block.BusinessType.Equals(PBusinessType.Club)) {
                Toll += PMath.Percent(Toll, 100);
            }
            if (Block.Lord.Weapon != null && Block.Lord.Weapon.Model is P_KuTingTao && Player.Area.HandCardArea.CardNumber == 0) {
                Toll *= 2;
            }
            if (Toll <= 1000 && Player.Traffic != null && Player.Traffic.Model is P_NanManHsiang) {
                Toll = 0;
            }
            DeltaMoney -= Toll;
        }
        if (Player.Money + DeltaMoney <= 0) {
            return -30000 + DeltaMoney * 2;
        } else if (Player.Money <= 3000) {
            DeltaMoney *= 2;
        }
        DeltaMoney += 2000 * Block.GetCardStop;
        int LandValue = 0;
        if (Block.Lord == null && Block.Price < Player.Money) {
            LandValue = PMath.Percent(Block.Price, 10) * Game.Enemies(Player).Count;
            if (Block.IsBusinessLand) {
                LandValue += PMath.Max(PAiBusinessChooser.DirectionExpectations(Game, Player, Block));
            }
        } else if (Player.Equals(Block.Lord)) {
            int PurchaseLimit = Player.PurchaseLimit;
            LandValue += PMath.Percent(Block.Price, 20) * Game.Enemies(Player).Count * PurchaseLimit;
            if (Block.BusinessType.Equals(PBusinessType.Park)) {
                LandValue += PMath.Percent(Block.Price, 60) * PurchaseLimit;
            } else if (Block.BusinessType.Equals(PBusinessType.ShoppingCenter) || Block.BusinessType.Equals(PBusinessType.Club)) {
                LandValue += PMath.Percent(Block.Price, 20) * Game.Enemies(Player).Count * PurchaseLimit;
            }
            if (Player.General is P_YangYuHuan) {
                LandValue += P_ShunShouChiienYang.AIExpect(Game, Player, 0).Value;
            }
        }
        if (Block.Lord != null && Block.Lord.TeamIndex == Player.TeamIndex) {
            if (Block.BusinessType.Equals(PBusinessType.Institute)) {
                LandValue += 2 * 2000;
            } else if (Block.BusinessType.Equals(PBusinessType.Pawnshop)) {
                LandValue += 2000;
            }
            if (Player.General is P_PanYue && (Block.PortalBlockList.Count == 0 || InPortal)) {
                if (!Player.Equals(Game.NowPlayer) || Player.RemainLimit("闲居")) {
                    LandValue += PMath.Percent(Block.Price, 20 * Game.Enemies(Player).Count + 50);
                }
            }
        }

        int PortalValue = 0;
        if (!InPortal && Block.PortalBlockList.Count > 0) {
            PortalValue = PMath.Max(Block.PortalBlockList, (PBlock _Block) => Expect(Game, Player, _Block, true)).Value;
        }
        return DeltaMoney + LandValue * 20 / GetRingLength(Game, Block) + PortalValue;
    }

    public static int GetRingLength(PGame Game, PBlock Block) {
        Queue<PBlock> q = new Queue<PBlock>();
        q.Enqueue(Block);
        int[] Visited = new int[Game.Map.BlockList.Count];
        for (int i = 0; i < Visited.Length; ++i) {
            Visited[i] = int.MaxValue;
        }
        while (q.Count > 0) {
            PBlock Front = q.Dequeue();
            if (Front != null) {
                int v = Visited[Front.Index];
                if (Front.Equals(Block)) {
                    if (v != int.MaxValue) {
                        break;
                    } else {
                        v = 0;
                    }
                }
                Front.NextBlockList.ForEach((PBlock NextBlock) => {
                    if (Visited[NextBlock.Index] == int.MaxValue) {
                        Visited[NextBlock.Index] = v + 1;
                        q.Enqueue(NextBlock);
                    }
                });
            }
        }
        return Visited[Block.Index];
    }

}