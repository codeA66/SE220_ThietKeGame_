# Chỉ dẫn bắt buộc cho AI làm việc với project

Đọc file này trước khi làm việc. Quy tắc quan trọng nhất: **CẤM ĐOÁN, CẤM BỊA. Mọi kết luận về project phải có bằng chứng.**

## 0. Ngữ cảnh và điểm bắt đầu

- Chạy lệnh shell qua `rtk`; với lệnh chưa có wrapper, dùng `rtk proxy <lệnh>`. Đọc/ghi văn bản tiếng Việt bằng UTF-8.
- Đây là project Unity 2D. Phiên bản Editor lấy từ `ProjectSettings/ProjectVersion.txt` (đã đối chiếu ngày 16/11/2026: `6000.6.9f1`). Package thực tế lấy từ `Packages/manifest.json` và `Packages/packages-lock.json`; `docs/ProjectConfig.yaml` chỉ là bản tóm tắt.
- Code của game nằm trong `Assets/_Project/Core/Scripts/` và `Assets/_Project/Features/`. Scene sandbox: `Assets/_Project/Scenes/Sandbox_Player.unity`. Công cụ Editor: `Assets/_Project/Tools/Editor/`.
- Điểm vào để lần theo hành vi: `PlayerController`, `PlayerContext`, `PlayerStateMachine`; `EnemyController`, `EnemyContext`, `EnemyStateMachine`; combat dùng `Health`, `HitQuery`, `DamageInfo`.
- Ranh giới assembly được khai báo trong các `.asmdef`, không suy ra từ tên thư mục:

| Assembly | Reference nội bộ |
| --- | --- |
| Core | Không có; có reference Unity.InputSystem |
| VFX | Không có |
| CameraSystem | Không có |
| Combat | Core, VFX |
| Enemies | Core, Combat, VFX |
| Player | Core, VFX, Combat, CameraSystem; thêm Unity.InputSystem |

- Khi thay đổi phụ thuộc giữa module, đối chiếu `.asmdef` liên quan. Không tự thêm framework hoặc thay kiến trúc ngoài yêu cầu.
- `Library/`, `Logs/`, `UserSettings/` là dữ liệu cục bộ; không dùng làm nguồn code chính. `Assets/_ThirdParty/` và `SourceAssets/` không nằm trong phạm vi graph hiện tại; đọc trực tiếp khi nhiệm vụ cần.

## 1. Kiểm tra bằng chứng trước khi kết luận

- Đọc code, prefab, scene và cấu hình liên quan trước khi đánh giá tính năng hoặc đề xuất task.
- Không dựa vào tên file, cấu trúc thư mục, comment hoặc tài liệu kế hoạch để khẳng định một tính năng thiếu, chưa làm hoặc đã chạy đúng.
- Phân biệt rõ: người dùng xác nhận; có implementation trong code; có cấu hình trong prefab; có đối tượng/reference trong scene; đã quan sát chạy thực tế.
- Chỉ nói đã đọc những file thực sự đã đọc. Không nói đã kiểm tra toàn project khi mới đọc một phần.
- Mọi vấn đề phải nêu file và đoạn code/cấu hình làm bằng chứng. Nếu chưa đủ bằng chứng, ghi rõ chưa xác định và tiếp tục đọc phần liên quan; không bịa kết quả chạy thực tế.
- Không coi không có một state/class mang tên mong đợi là bằng chứng duy nhất rằng cơ chế không tồn tại. Tìm implementation trong toàn bộ phạm vi liên quan.
- Khi người dùng yêu cầu đối chiếu project khác hoặc cách làm thực tế, xem nguồn gốc có thể kiểm chứng, dẫn nguồn và phân biệt sự thật từ nguồn với đề xuất cho project này.

## 2. Chỉ thêm hoặc sửa khi người dùng cho phép

- Mặc định chỉ đọc. Chỉ thêm/sửa/xóa code khi người dùng cho phép rõ ràng trong phạm vi cụ thể.
- Yêu cầu đọc code, kiểm tra, giải thích, so sánh hoặc lập kế hoạch không phải là quyền sửa code.
- Quyền tạo/sửa tài liệu chỉ áp dụng cho tài liệu được yêu cầu; không mở rộng thành quyền sửa gameplay, prefab, scene, asset, package hay ProjectSettings.
- Việc sửa prefab, scene, ScriptableObject asset, Input Actions, package và cấu hình project cũng cần quyền phù hợp từ người dùng.
- Không chạy thao tác Unity có thể làm thay đổi project, build, Play Mode hoặc test nếu chưa được cho phép trong nhiệm vụ hiện tại.
- Nếu đã được cho phép, thực hiện đúng phạm vi đó; không xin lại cùng một quyền và không tự mở rộng phạm vi.
- Không ghi đè thay đổi có sẵn của người dùng. Kiểm tra trạng thái thay đổi trước khi ghi file được phép sửa.
- Không tự tạo/sửa/xóa file .meta.

## 3. Lập task dựa trên hiện trạng đã kiểm tra

- Kiểm tra implementation và wiring thực tế trước khi thêm task tạo, tích hợp hoặc hoàn thiện tính năng.
- Người dùng đã xác nhận player movement hiện có được coi là hoàn thiện; không tự tạo task làm lại/kiểm tra lại phần này.
- Người dùng đã xác nhận quái đã được đưa vào prefab. Không mặc định cần tạo hoặc đưa quái vào prefab lần nữa.
- Các bổ sung movement được người dùng yêu cầu phải được phân biệt với movement đã có. Kiểm tra implementation trước khi xác định phần việc cụ thể.
- docs/SprintPlan.md là đề xuất kế hoạch, không phải bằng chứng tính năng chưa tồn tại. Đối chiếu lại task với code/prefab/scene trước khi dùng kế hoạch.
- Task phải có đầu ra cụ thể và Acceptance Criteria dựa trên yêu cầu đã xác nhận. Không bịa lỗi, số liệu, thời gian hoàn thành hoặc trạng thái công việc.
- Estimate chưa có căn cứ thì để chưa chốt; không trình bày ước lượng như số liệu đã xác nhận.
- Không tự phân nhóm thành các vai trò cố định. Để người phụ trách trống cho thành viên tự pick task, trừ khi người dùng yêu cầu khác.
- Mã tham chiếu trong tài liệu không phải issue key Jira. Issue key thực tế chỉ được ghi khi đã lấy từ Jira.

## 4. Bắt đầu và kết thúc công việc

- Đọc docs/ProjectConfig.yaml để có ngữ cảnh; xác minh thông tin cần thiết bằng file project thực tế.
- Tìm các AGENTS.md ở thư mục con trước khi làm việc trong phạm vi đó, nếu có.
- Với yêu cầu đọc toàn bộ Assets, kiểm kê tất cả file và phân loại code, văn bản cấu hình, scene/prefab, hình ảnh/âm thanh và dữ liệu nhị phân. Nêu rõ phần đã đọc nội dung và phần chỉ kiểm kê.
- Báo cáo phạm vi đã đọc, bằng chứng chính, phần chưa xác định và mọi thay đổi đã thực hiện.
- Khi phát hiện nhận định trước đó sai, sửa nhận định theo bằng chứng. Không tự sửa implementation nếu người dùng chỉ yêu cầu kiểm tra.

## 5. Hiện trạng đã đối chiếu ngày 13/10/2026

Đây là mốc đọc file, không phải kết quả chạy Unity. Khi bắt đầu nhiệm vụ mới, xác minh lại các file liên quan nếu chúng đã thay đổi.

- Đã kiểm kê 4.717 file trong Assets, đọc 60 file C# và 2 shader; đọc/đối chiếu cấu hình serialized, prefab, scene, animation, Input Actions và metadata.
- Có 5 enemy prefab: Enemy_Zombie, Enemy_Rat, Enemy_Shield, Enemy_Archer, Enemy_Grenadier. Mỗi prefab đã có Health, Hurtbox, EnemyAnimator, EnemyController, Rigidbody2D, collider, animation controller và EnemyData tương ứng.
- Assets/_Project/Scenes/Sandbox_Player.unity đã có instance của cả 5 enemy prefab. Scene override đã gán _player, _projectiles và _vfx cho chúng. Không tạo task tích hợp lại các reference này như thể chưa có.
- Scene đã có Player.prefab, TrainingDummy, CameraFollow2D, VfxSpawner, HitStop, ProjectileSpawner, ground/platform và Chain_Test.
- Player trong Sandbox_Player đã được scene override gán VFX spawner, camera và hit-stop. Combat và feedback không chỉ mới tồn tại dưới dạng file code.
- Enemy_Archer.asset đã tham chiếu Projectile_Arrow.prefab. Enemy_Grenadier.asset đã tham chiếu Projectile_Grenade.prefab và Vfx_Explosion.asset.
- Các reference fileID bằng 0 trong prefab có thể được gán bằng scene override hoặc Initialize tại runtime. Phải kiểm tra cả hai trước khi kết luận thiếu wiring.
- Các hình PNG và file âm thanh đã được kiểm kê và đọc metadata import; chưa xem từng hình hoặc nghe từng âm thanh. Chưa chạy Play Mode, build hoặc test trong lần đọc này.

<!-- CODEGRAPH_START -->
## 6. CodeGraph — ưu tiên khi tìm và đọc code

- Khi repo có `.codegraph/`, dùng CodeGraph **trước `rg` hoặc đọc file code** để tìm symbol, đọc source, lần theo caller/callee và phạm vi ảnh hưởng.
- MCP: `codegraph_explore`, truyền `projectPath` là đường dẫn tuyệt đối của repo (máy hiện tại: `D:/thietkegame`). Đặt tên symbol/file cụ thể trong `query`. Source được trả về có số dòng; không đọc lại đúng đoạn đó bằng công cụ khác.
- CLI tương đương trên Windows; `cmd /c` cần thiết vì CodeGraph được cài dưới dạng `.cmd`:

```powershell
rtk proxy cmd /c codegraph status
rtk proxy cmd /c codegraph explore "PlayerCombat PerformStrike HitQuery"
rtk proxy cmd /c codegraph sync
```

- Chạy `sync` sau khi sửa code trong phạm vi được phép hoặc khi index cũ. Khi chưa có index ở bản clone mới, chỉ chạy `rtk proxy cmd /c codegraph init --yes .` nếu người dùng đã cho phép tạo index.
- `codegraph.json` chứa các đường dẫn loại trừ. Không index agent tooling, template, source art hoặc output Graphify.
- Nếu tool không hoạt động hoặc symbol không có trong index, dùng `rg` và đọc source trực tiếp, nêu rõ giới hạn. Không coi kết quả không tìm thấy là bằng chứng tính năng chưa tồn tại.
- Index tĩnh không thay thế việc kiểm tra serialized reference, prefab override, scene, animation, Input Actions hay hành vi runtime.
<!-- CODEGRAPH_END -->

<!-- GRAPHIFY_START -->
## 7. Graphify — bản đồ quan hệ để khảo sát

- Dùng `graphify-out/graph.json` để khảo sát quan hệ tổng thể; dùng CodeGraph để kiểm tra source/caller cụ thể trước khi kết luận hoặc sửa code.
- Graph hiện tại được tạo bằng AST cục bộ với `--code-only`; không trích xuất ngữ nghĩa tài liệu/hình ảnh. Một số cấu hình JSON/YAML cũng được công cụ phân loại là code. Không xem graph là bản kiểm kê toàn bộ Assets.
- Đọc `source_file`, `source_location` và mức tin cậy của cạnh; cạnh `INFERRED` cần đối chiếu source. Không suy ra wiring Unity từ graph.

```powershell
rtk proxy graphify query "PlayerCombat PerformStrike HitQuery" --budget 2000
rtk proxy graphify extract . --code-only --max-workers 2
rtk proxy graphify cluster-only . --no-label --no-viz
rtk proxy graphify export html
```

- Chạy từ repo root. Ba lệnh cuối lần lượt cập nhật graph, báo cáo và HTML; dùng sau thay đổi code được phép. `--code-only` và `--no-label` giữ quá trình cục bộ, không gọi LLM/API.
- Nếu console Windows lỗi Unicode, chạy cùng lệnh qua interpreter lưu trong `graphify-out/.graphify_python`: `rtk proxy <python.exe> -X utf8 -m graphify ...`. Khi chuyển máy, xác định lại interpreter đang cài `graphifyy`; không dùng đường dẫn máy cũ.
- `.graphifyignore` bổ sung phạm vi loại trừ ngoài `.gitignore`. `.codegraph/` và `graphify-out/` được git-ignore; máy clone mới cần tạo lại index khi được phép. Không thêm output vào `Assets/`.
- Không tự cài Git hook, watcher, server, API provider hoặc sửa cấu hình agent toàn máy. MCP CodeGraph và skill Graphify hiện có trên máy đã đủ cho cách dùng này.
<!-- GRAPHIFY_END -->

## 8. Vai trò thiết kế map

- Người dùng phụ trách **Level Designer / Map Designer**: chọn ảnh tham chiếu, quyết định bố cục, xác nhận các sai khác và duyệt map.
- AI hỗ trợ phân tích ảnh, đối chiếu tileset, dựng map theo phạm vi được giao và kiểm tra các reference/collision liên quan. Không tự thay đổi gameplay để ép khớp ảnh.
- Brief hiện tại: người dùng sẽ gửi một ảnh; tái dựng map bám sát ảnh bằng bộ tileset đã đưa. Chưa nhận ảnh thì chưa chốt layout, kích thước hoặc nội dung map.
- Quy trình, asset và checklist chuẩn bị: `docs/MapDesign.md`. Không coi tài liệu chuẩn bị này là bằng chứng đã dựng hoặc đã playtest map.

## 9. Bắt buộc sử dụng Unity Plugin

- Mọi agent làm việc trong project này **phải ưu tiên sử dụng Unity plugin** (các skill của plugin `unity`, `unity-cli` và `unity-pipeline` / `com.unity.pipeline`) cho mọi tác vụ phát triển game Unity thay vì tự đoán API, cấu hình serialized hoặc thao tác thủ công thiếu kiểm chứng.
- Trước khi thực hiện tác vụ thuộc lĩnh vực nào, đọc `SKILL.md` của skill tương ứng trong Unity plugin:
  - **Điều khiển Editor, CLI, compile, test, Play Mode, eval, screenshot**: `unity-cli` (`.agents/skills/unity-cli/SKILL.md` hoặc plugin `unity`), `unity-pipeline`, `generate-editor-search-query`.
  - **Quản lý package (UPM)**: `unity-package-management`.
  - **2D, Tilemap, Physics 2D, Sprite**: `create-2d-physics`, `create-tile-palette-or-rule-tiles`, `2d-pixel-perfect`, `sprite-editor`, `sprite-segment-3x3grid`, `manage-sprite-atlas`.
  - **UI**: luôn đọc `ui` trước để chọn đúng framework (`ui-uitk`, `ui-ugui`, `ui-imgui`).
  - **URP & Shader**: `urp-postprocessing`, `shader-graph-create-custom-node`, `validate-urp-render-graph-renderer-feature`.
  - **Audio, Font, Localization, AI Nav, Polish**: `optimize-audio`, `audio-setup-mixers`, `optimize-text-mesh-pro`, `localize-unity-project`, `initialize-ai-navigation`, `craft-game-floors`.
- Hạ tầng đã cấu hình trong project/máy:
  - Global plugin `unity` (`0.1.9-beta`) tại `~/.gemini/config/plugins/unity`.
  - Skill `unity-cli` trong repo tại `.agents/skills/unity-cli/`.
  - MCP server `unity` trong `.codex/config.toml` trỏ tới `D:\thietkegame` và package `com.unity.pipeline` (`1.0.0`) trong `Packages/manifest.json` (xem thêm `docs/UnityConnection.md`).
- **Ranh giới quyền hạn**: Có sẵn Unity plugin, `unity-cli` và `unity-pipeline` không tự cấp quyền sửa code, scene, prefab, asset, package hay chạy Play Mode, test, build. Mọi thao tác ghi hoặc làm thay đổi project vẫn phải tuân theo Mục 2.

## 10. Định hướng sáng tạo & tư duy thiết kế (Dead Cells)

- **Bản chất dự án**: Đây là dự án game **2D hack-and-slash** (action-platformer / roguelite) lấy cảm hứng rất lớn từ **Dead Cells**.
- **Không giới hạn sáng tạo**: Vì là dự án cho môn học, **không tự đặt giới hạn** hay cắt giảm chất lượng/độ táo bạo trong thiết kế game, gameplay, asset hay VFX. Mạnh dạn đề xuất và thiết kế ở chuẩn mực cao nhất có thể trong phạm vi nhiệm vụ được giao.
- **Tư duy như đội ngũ Dead Cells**:
  - **Gameplay & Combat**: Ưu tiên cảm giác điều khiển nhạy bén, nhịp độ nhanh, mượt mà (input buffering, coyote time, animation cancel hợp lý, hitstop, knockback, i-frames, telegraph đòn đánh rõ ràng và công bằng).
  - **Assets & Art Direction**: Đảm bảo tách lớp thị giác rõ ràng (background giảm bão hòa/tối để nhân vật, kẻ địch, đạn và bẫy nổi bật tức thì); có thể tham chiếu cấu trúc sprite/animation/audio trong `Assets/_ThirdParty/DeadCells/` để học hỏi tỷ lệ, timing và nhịp chuyển động.
  - **VFX, Audio & Game Feel**: Mỗi nhát chém, cú né, va chạm hay hạ gục phải có lực — kết hợp hit-flash, slash trail, particle theo hướng lực, camera shake vừa đủ và âm thanh sắc gọn, không làm rối tầm nhìn gameplay.
  - **Level & Flow**: Thiết kế không gian khuyến khích di chuyển liên tục, leo trèo, lăn né và giao tranh dồn dập.
- Vẫn tuân thủ Mục 2 về quyền hạn thực thi (chỉ sửa code/asset/scene khi được người dùng cho phép trong phạm vi cụ thể) và Mục 8 về vai trò Level/Map Designer.
