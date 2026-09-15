# 타이틀 원형 전환

Title 씬의 기존 Start → TitleManager.GameStart 연결을 사용합니다.

1. 원이 줄어들며 타이틀을 검게 덮습니다(기본 0.4초).
2. 검은 화면에서 Selete 씬을 비동기 로드합니다.
3. 중앙의 원이 커지며 Selete 씬이 나타납니다(기본 0.7초).

TitleManager Inspector의 Next Scene Name, Close Duration, Reveal Duration에서 이동 씬과 시간을 변경할 수 있습니다.
씬이 빌드 목록에 없으면 오류를 출력하고 타이틀에 남습니다. 로딩 중 중복 GameStart 호출은 무시됩니다.
전환 Canvas만 DontDestroyOnLoad로 유지하고 연출 종료 후 제거합니다. 시간은 unscaledDeltaTime을 사용합니다.
원형 그래픽은 코드로 생성하므로 이미지나 셰이더 설정이 필요하지 않습니다.

검증: Unity 6000.5.8f1 어셈블리 참조로 C# 컴파일. 실제 Unity 플레이 전환은 미검증.
