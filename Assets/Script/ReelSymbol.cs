using UnityEngine;

public enum SymbolType
{
    Attack,  // 검: 공격
    Shield,  // 방패: 방어력 획득
    Heal,    // 하트: 체력 회복
    Gold,    // 골드: 재화 획득
    Skull    // 해골: 패널티/적 공격
}

[System.Serializable]
public class ReelSymbolData
{
    [Tooltip("같은 마법을 구분하는 이름입니다. 비어 있으면 심볼 타입 이름을 사용합니다.")]
    public string symbolName;
    public SymbolType symbolType;
    public Sprite symbolSprite;

    [Min(0)]
    [Tooltip("심볼 하나가 발동할 때의 기본 수치입니다. 0이면 타입별 기본값을 사용합니다.")]
    public int baseValue;

    [Min(0)]
    [Tooltip("동일 심볼 3개 이상일 때 한 번 더해지는 보너스입니다. 0이면 타입별 기본값을 사용합니다.")]
    public int comboBonus;

    [Min(0)]
    [Tooltip("동일 심볼 5개 이상일 때 콤보 보너스 대신 한 번 더해지는 보너스입니다. 0이면 타입별 기본값을 사용합니다.")]
    public int megaBonus;

    [Range(1, 100)]
    public int weight = 10; // 확률 가중치

    public string DisplayName => string.IsNullOrWhiteSpace(symbolName)
        ? symbolType.ToString()
        : symbolName;

    public int BaseValue => baseValue > 0 ? baseValue : GetDefaultBaseValue();
    public int ComboBonus => comboBonus > 0 ? comboBonus : GetDefaultComboBonus();
    public int MegaBonus => megaBonus > 0 ? megaBonus : GetDefaultMegaBonus();

    private int GetDefaultBaseValue()
    {
        switch (symbolType)
        {
            case SymbolType.Attack: return 3;
            case SymbolType.Shield: return 5;
            case SymbolType.Heal: return 5;
            case SymbolType.Gold: return 15;
            default: return 0;
        }
    }

    private int GetDefaultComboBonus()
    {
        switch (symbolType)
        {
            case SymbolType.Attack: return 9;
            case SymbolType.Shield: return 5;
            case SymbolType.Heal: return 10;
            case SymbolType.Gold: return 15;
            default: return 0;
        }
    }

    private int GetDefaultMegaBonus()
    {
        switch (symbolType)
        {
            case SymbolType.Attack: return 30;
            case SymbolType.Shield: return 20;
            case SymbolType.Heal: return 25;
            case SymbolType.Gold: return 45;
            default: return 0;
        }
    }
}

