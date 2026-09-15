# 스핀 결과 드래그 사용

- 스핀 도중 슬롯은 계속 보입니다. 모든 릴이 멈추면 슬롯 창을 닫고 화면 하단에 결과 카드 5개를 릴 순서대로 표시합니다.
- 공격 카드는 Enemy 스프라이트 위에, 보호막/회복 카드는 Player 스프라이트 위에 놓습니다. 잘못된 위치에 놓으면 카드가 돌아옵니다.
- 사용한 카드는 소모됩니다. 모든 카드를 쓰거나 END TURN을 누르면 기존 턴 진행으로 이어집니다. END TURN은 남은 카드를 버립니다.
- 같은 심볼의 콤보 보너스는 해당 카드들에 나눠 적용하여 기존 합계가 유지됩니다.
- 현재 프로젝트에는 체력 시스템이 없어 SpellResultHand 안에 플레이어/적 HP 100, 보호막, 골드 상태를 구현했습니다. 적 HP가 0이면 VICTORY로 멈춥니다. 기존 적 턴은 안내 대기이며 적 AI/공격은 추가하지 않았습니다.
- 씬의 Player, Enemy SpriteRenderer와 기존 Canvas를 사용합니다. 카드는 CanvasGroup 없이 EventSystem 드래그 인터페이스로 동작합니다.
- 씬에 하단 카드 영역을 런타임 생성합니다. 위치는 SpellResultHand.Initialize의 hand.anchorMin/anchorMax로 조절할 수 있습니다.

## 검증

Unity 6000.5.8f1 참조로 C# 빌드 확인. 실제 에디터 플레이 테스트는 수행하지 못했습니다.
플레이 확인: 첫 스핀 → 카드 5개 표시 → 잘못된 위치 드롭 시 복귀 → 적에게 공격 시 HP 감소/카드 소모 → 플레이어에게 방패 시 SHIELD 증가 → 남은 카드 END TURN → 다음 스핀. 재시작하면 HP와 카드 초기화.

## 생성 이미지

내장 image_gen 도구 사용. 리소스: Assets/Resources/SpellIcons/spell-atlas.png.
런타임에 왼쪽부터 공격/보호막/회복 3개 Sprite로 분리하여 카드에 사용합니다. 기존 릴 이미지 파일은 보존합니다.

최종 프롬프트:

Create a single game-ready sprite atlas, wide 3:1 aspect ratio, three equal square tiles in one horizontal row with exact equal widths. Pixel art dark fantasy dungeon spell icons, crisp chunky pixel edges, no text no letters no borders. Left tile: luminous violet magic missile with comet trail. Middle tile: cyan arcane shield. Right tile: emerald healing light with golden sparkles. Each symbol centered in its own tile with generous 15 percent padding, each tile flat very dark navy background. Cohesive readable inventory UI art for a retro dungeon slot game. This is one sprite sheet asset.
