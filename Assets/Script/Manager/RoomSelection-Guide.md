# Selete 방 선택

Selete 씬 Room 오브젝트에 SeleteManager를 연결합니다. 기존 1/2/3 버튼, Start, 좌우 화살표, Catelog를 사용합니다.

- 방 버튼 클릭 또는 화살표: 해당 방이 중앙으로 0.35초 동안 이동하고 설명이 바뀝니다. 빠르게 다른 방을 누르면 현재 위치에서 새 목표로 이동합니다.
- 잠긴 방도 선택/설명을 볼 수 있지만 Start는 비활성화됩니다. 이동 중에도 Start는 비활성화됩니다.
- 열린 방 초록색, 잠긴 방 빨간색. 선택된 방은 12% 확대됩니다.
- Start: 중앙에 도착한 방 번호를 저장하고 원형 전환으로 Ingame에 진입합니다. Ingame의 ROOM 번호와 적 체력에 선택 방이 반영됩니다.
- 처음에는 ROOM 1만 열립니다. 클리어 시 다음 번호가 PlayerPrefs에 저장되어 게임 재실행 후에도 열립니다.

SeleteManager의 Rooms 배열을 직접 설정하면 각 Button, Room Number, Title, Description, Scene Name, Unlocked By Default를 수정할 수 있습니다. 배열이 비어 있으면 기존 숫자 이름 버튼 1/2/3을 자동 연결하고 기본 설명을 제공합니다. 이동 시간과 열린/잠긴 색도 Inspector에서 변경할 수 있습니다.

검증: Unity 참조 C# 컴파일 및 씬의 버튼·매니저 참조 확인. 실제 Unity 클릭/전환 플레이 검증은 미수행.
