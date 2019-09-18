public class PSkillInfo : PObject {
    public string ToolTip;
    public PSkillType Type;

    private PSkillInfo(string _Name, string _ToolTip, PSkillType _Type) {
        Name = _Name;
        ToolTip = _ToolTip;
        Type = _Type;
    }
    public static PSkillInfo Empty = new PSkillInfo("空技能", "", PSkillType.Lock);
    public static PSkillInfo 负荆 = new PSkillInfo("负荆", "【主】回合内限一次，你可以令一名其他角色对你造成3000点伤害，然后该角色摸一张牌。", PSkillType.Initiative);
    public static PSkillInfo 英姿 = new PSkillInfo("英姿", "【被】回合开始时，你可以摸200。", PSkillType.SoftLock);
    public static PSkillInfo 闲居 = new PSkillInfo("闲居", "【被】回合结束时，若在本回合内你没有造成或受到伤害，且你在其他角色的土地上，你可以就地建造一座房屋。", PSkillType.Passive);

    public PSkillInfo Copy() {
        return new PSkillInfo(Name, ToolTip, Type);
    }

    public PSkillInfo SetType(PSkillType _Type) {
        Type = _Type;
        return this;
    }

}