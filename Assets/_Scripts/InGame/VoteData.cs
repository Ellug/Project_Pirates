using System;

[Serializable]
public class VotePlayerInfo
{
    public int ActorNumber;      // Photon ActorNumber (플레이어 고유 ID)
    public string NickName;      // 플레이어 닉네임
    public bool IsDead;          // 사망 여부
    public int VoteCount;        // 받은 투표 수
    public int VotedFor;         // 이 플레이어가 투표한 대상 (-1: 미투표, -2: 스킵)

    public VotePlayerInfo(int actorNumber, string nickName)
    {
        ActorNumber = actorNumber;
        NickName = nickName;
        IsDead = false;
        VoteCount = 0;
        VotedFor = -1;
    }
}

// 투표 상태
public enum VotePhase
{
    None,
    Discussion,     // 토론 시간
    Voting,         // 투표 진행 중
    Result          // 결과 표시
}
