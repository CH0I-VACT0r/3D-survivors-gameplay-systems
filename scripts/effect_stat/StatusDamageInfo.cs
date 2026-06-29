public struct StatusDamageInfo
{
    public StatusType type;
    public float damage;        // 해당 타격 시 즉시 입히는 추가 상태 이상 피해
    public float gaugeValue;    // 면역치 게이지를 얼마나 채울 것인가? (기본 1)
    public float synergy;
}