﻿

public class PInjureTag : PTag {
    public static string TagName = "伤害";
    public static string FromPlayerFieldName = "造成伤害的玩家";
    public static string ToPlayerFieldName = "受到伤害的玩家";
    public static string InjureFieldName = "伤害的数额";
    public static string InjureSourceFieldName = "伤害方式来源";
    public PInjureTag(PPlayer FromPlayer, PPlayer ToPlayer, int Injure, PObject InjureSource): base(TagName) {
        AppendField(FromPlayerFieldName, FromPlayer);
        AppendField(ToPlayerFieldName, ToPlayer);
        AppendField(InjureFieldName, Injure);
        AppendField(InjureSourceFieldName, InjureSource);
    }
    public PPlayer FromPlayer {
        get {
            return GetField<PPlayer>(FromPlayerFieldName, null);
        }
        set {
            SetField(FromPlayerFieldName, value);
        }
    }
    public PPlayer ToPlayer {
        get {
            return GetField<PPlayer>(ToPlayerFieldName, null);
        }
        set {
            SetField(ToPlayerFieldName, value);
        }
    }
    public int Injure {
        get {
            return GetField(InjureFieldName, 0);
        }
        set {
            SetField(InjureFieldName, value);
        }
    }
    public PObject InjureSource {
        get {
            return GetField<PObject>(InjureSourceFieldName, null);
        }
    }

}